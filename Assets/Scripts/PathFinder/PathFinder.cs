using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using System;

public class PathFinder : MonoBehaviour
{
	public AIState state = AIState.Wander;

	public PathNode destination;
	public Transform target;
	public PathNode next, prevNext;
	bool foundDestination;
	public float stoppingDistance;
	[Tooltip("Little helpful forward force to move the player faster.")]
	public float forwardForce;
	public float speed;
	public float jumpForce;
	public float dropForce;

	public bool large;

	Rigidbody rb;

	public List<PathNode> bestPath;
	protected int nextIndex;

	protected Color ogColor;
	new internal MeshRenderer renderer;

	public Vector3 direction = new Vector3(1, 0, 1);

	[Space]
	public Canvas characterCanvas;
	public UnityEngine.UI.RawImage healthBarBack;
	public UnityEngine.UI.RawImage healthBarFront;
	public int maxHealth;
	private float currentHealth;

	[Space]
	internal List<Transform> enemies = new List<Transform>();
	internal List<Transform> items = new List<Transform>();

	internal CastEvent enemyFound;
	internal CastEvent enemyLost;

	public Transform flashLight;

	internal Camera cam;

	internal virtual void Start()
	{
		if (destination == null)
		{
			List<PathNode> viables = FindObjectsOfType<PathNode>()
				.Where(o => o.connections.Count > 0 && (!large || o.connections.Exists(f => f.type == PathConnectionType.Normal))).ToList();

			int r = Random.Range(0, viables.Count);

			destination = viables[r];

			List<PathNode> nodes = FindObjectsOfType<PathNode>()
								.Where(o => o.connections.Count > 0)
								.OrderBy(o => Vector3.Distance(o.transform.position, transform.position))
								.ToList();

			next = nodes[0];

			GetOptimalPath(bestPath, nodes[0], destination);
			nextIndex = -1;
		}

		rb = GetComponent<Rigidbody>();

		renderer = GetComponent<MeshRenderer>();
		ogColor = renderer.material.color;

		//Events
		enemyFound = new CastEvent();
		enemyLost = new CastEvent();

		enemyFound.AddListener(EnemyFound);
		enemyLost.AddListener(EnemyLost);
	}

	private void FixedUpdate()
	{
		//If not attacking then move
		if (state != AIState.Attacking && Vector3.Distance(direction, Vector3.zero) != 0)
		{
			rb.AddTorque(direction * speed);
			rb.AddForce(Vector3.Cross(direction, Vector3.up) * forwardForce);
		}

		flashLight.position = transform.position;
		if (direction.normalized != Vector3.zero)
			flashLight.rotation = Quaternion.LookRotation(Vector3.Cross(direction, Vector3.up));

		characterCanvas.GetComponent<RectTransform>().position = transform.position;
		characterCanvas.GetComponent<RectTransform>().rotation = Quaternion.identity;

		healthBarBack.GetComponent<RectTransform>().rotation = Quaternion.LookRotation(cam.transform.forward);
	}

	internal virtual void Update()
	{
		switch (state)
		{
			case AIState.Wander:
				{
					renderer.material.color = ogColor;

					if (foundDestination || destination == null)
					{
						List<PathNode> viables = FindObjectsOfType<PathNode>()
							.Where(o => o.connections.Count > 0 && 
								((!large && o.connections.Exists(f => f.type == PathConnectionType.Narrow)) || 
								o.connections.Exists(f => f.type == PathConnectionType.Normal))).ToList();

						int r = Random.Range(0, viables.Count);

						destination = viables[r];

						foundDestination = false;

						List<PathNode> nodes = FindObjectsOfType<PathNode>()
							.Where(o => o.connections.Count > 0)
							.OrderBy(o => Vector3.Distance(o.transform.position, transform.position))
							.ToList();

						next = nodes[0];
						GetOptimalPath(bestPath, nodes[0], destination);
						if (bestPath.Count <= 1)
						{
							destination = null;
						}
						nextIndex = -1;
					}
					else
					{
						if (next != null)
						{
							Vector3 nextPositionElevated = new Vector3(next.transform.position.x, next.transform.position.y + renderer.bounds.extents.y, next.transform.position.z);
							//transform.Translate(Vector3.Normalize(nextPositionElevated - transform.position) * speed * Time.deltaTime, Space.World);
							direction = Vector3.Cross(Vector3.Normalize(nextPositionElevated - transform.position), -Vector3.up);

							if (Vector3.Distance(transform.position, nextPositionElevated) <= stoppingDistance)
							{
								if (nextIndex + 1 < bestPath.Count)
								{
									prevNext = next;
									next = bestPath[++nextIndex];

									//Handle door nodes
									RaycastHit hit;

									//Check line of sight
									if (Physics.SphereCast(
										new Ray(transform.position, (next.transform.position - transform.position).normalized), 
										next.radius, 
										out hit, 
										Vector3.Distance(next.transform.position, transform.position), 
										Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Ground", "Node", "Player", "Enemy"), 
										QueryTriggerInteraction.Collide))
									{
										if (hit.collider.tag == "Door")
										{
											//Make sure not already added door connection
											if (hit.collider.GetComponentInParent<DoorBehavior>() != null)
											{
												hit.collider.GetComponentInParent<DoorBehavior>().Open();
											}
										}
									}

									if (prevNext != null)
									{
										if (prevNext.connections.Find(o => o.pathNode == next) != null)
										{
											if (prevNext.connections.Find(o => o.pathNode == next).type == PathConnectionType.Jump)
											{
												rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
											}
											else if (prevNext.connections.Find(o => o.pathNode == next).type == PathConnectionType.Drop)
											{
												rb.AddForce(Vector3.up * dropForce, ForceMode.Impulse);
											}
										}
									}
								}
							}
						}
						else
						{
							next = bestPath[0];
						}
					}
				}
				break;
			case AIState.Follow:
				{
					renderer.material.color = ogColor * 2;

					if (next != null)
					{
						Vector3 nextPositionElevated = new Vector3(next.transform.position.x, next.transform.position.y + renderer.bounds.extents.y, next.transform.position.z);
						//transform.Translate(Vector3.Normalize(nextPositionElevated - transform.position) * speed * Time.deltaTime, Space.World);
						direction = Vector3.Cross(Vector3.Normalize(nextPositionElevated - transform.position), -Vector3.up);

						if (Vector3.Distance(transform.position, nextPositionElevated) <= stoppingDistance)
						{
							List<Connection> tempConnections;

							if (!large)
								tempConnections = next.connections
									.OrderBy(o => Vector3.Distance(o.pathNode.transform.position, target.position))
									.ToList();
							else
								tempConnections = next.connections
									.Where(o => o.type == PathConnectionType.Normal)
									.OrderBy(o => Vector3.Distance(o.pathNode.transform.position, target.position))
									.ToList();

							prevNext = next;
							next = tempConnections[0].pathNode;

							//Handle door nodes
							RaycastHit hit;

							//Check line of sight
							if (Physics.SphereCast(
								new Ray(transform.position, (next.transform.position - transform.position).normalized),
								next.radius,
								out hit,
								Vector3.Distance(next.transform.position, transform.position),
								Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Ground", "Node", "Player", "Enemy"),
								QueryTriggerInteraction.Collide))
							{
								if (hit.collider.tag == "Door")
								{
									//Make sure not already added door connection
									if (hit.collider.GetComponentInParent<DoorBehavior>() != null)
									{
										hit.collider.GetComponentInParent<DoorBehavior>().Open();
									}
								}
							}

							if (tempConnections[0].type == PathConnectionType.Jump)
							{
								rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
							}
							else if (tempConnections[0].type == PathConnectionType.Drop)
							{
								rb.AddForce(Vector3.up * dropForce, ForceMode.Impulse);
							}
						}
					}
					else
					{
						List<PathNode> nodes = FindObjectsOfType<PathNode>()
							.Where(o => o.connections.Count > 0)
							.OrderBy(o => Vector3.Distance(o.transform.position, transform.position))
							.ToList();

						transform.position = nodes[0].transform.position;

						next = nodes[0];
					}
				}
				break;
			case AIState.Flee:
				{
					renderer.material.color = ogColor * 2;

					if (next != null)
					{
						Vector3 nextPositionElevated = new Vector3(next.transform.position.x, next.transform.position.y + renderer.bounds.extents.y, next.transform.position.z);
						//transform.Translate(Vector3.Normalize(nextPositionElevated - transform.position) * speed * Time.deltaTime, Space.World);
						direction = Vector3.Cross(Vector3.Normalize(nextPositionElevated - transform.position), -Vector3.up);

						if (Vector3.Distance(transform.position, nextPositionElevated) <= stoppingDistance)
						{
							List<Connection> tempConnections;

							if (!large)
								tempConnections = next.connections
									.OrderByDescending(o => Vector3.Distance(o.pathNode.transform.position, target.position))
									.ToList();
							else
								tempConnections = next.connections
									.Where(o => o.type == PathConnectionType.Normal)
									.OrderByDescending(o => Vector3.Distance(o.pathNode.transform.position, target.position))
									.ToList();

							prevNext = next;
							next = tempConnections[0].pathNode;

							//Handle door nodes
							RaycastHit hit;

							//Check line of sight
							if (Physics.SphereCast(
								new Ray(transform.position, (next.transform.position - transform.position).normalized),
								next.radius,
								out hit,
								Vector3.Distance(next.transform.position, transform.position),
								Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Ground", "Node", "Player", "Enemy"),
								QueryTriggerInteraction.Collide))
							{
								if (hit.collider.tag == "Door")
								{
									//Make sure not already added door connection
									if (hit.collider.GetComponentInParent<DoorBehavior>() != null)
									{
										hit.collider.GetComponentInParent<DoorBehavior>().Open();
									}
								}
							}

							if (tempConnections[0].type == PathConnectionType.Jump)
							{
								rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
							}
							else if (tempConnections[0].type == PathConnectionType.Drop)
							{
								rb.AddForce(Vector3.up * dropForce, ForceMode.Impulse);
							}
						}
					}
					else
					{
						List<PathNode> nodes = FindObjectsOfType<PathNode>()
							.Where(o => o.connections.Count > 0)
							.OrderBy(o => Vector3.Distance(o.transform.position, transform.position))
							.ToList();

						transform.position = nodes[0].transform.position;

						next = nodes[0];
					}
				}
				break;
		}
		
		if (destination != null)
		{
			Vector3 nextPositionElevated = new Vector3(destination.transform.position.x, destination.transform.position.y + GetComponent<MeshRenderer>().bounds.extents.y, destination.transform.position.z);
			if (Vector3.Distance(transform.position, nextPositionElevated) <= stoppingDistance)
			{
				foundDestination = true;
			}
		}
	}

	protected void GetOptimalPath(List<PathNode> path, PathNode startNode, PathNode destination)
	{
		List<PathNode> nodes = new List<PathNode>();
		List<PathNode> closed = new List<PathNode>();
		nodes.Add(startNode);

		PathNode currentNode = startNode;

		while (nodes.Count > 0)
		{
			nodes.OrderByDescending(o => Vector3.Distance(startNode.transform.position, currentNode.transform.position) + Vector3.Distance(currentNode.transform.position, destination.transform.position));
			currentNode = nodes[0];

			nodes.Remove(currentNode);
			closed.Add(currentNode);

			if (currentNode == destination)
				break;

			foreach (PathNode node in currentNode.connections.Select(o => o.pathNode))
			{
				if (!closed.Contains(node))
				{
					if (!nodes.Contains(node))
					{
						nodes.Add(node);

						node.parent = currentNode;
					}
					else
					{
						if (Vector3.Distance(node.transform.position, destination.transform.position) < Vector3.Distance(currentNode.transform.position, destination.transform.position))
						{
							node.parent = currentNode;
							nodes.OrderBy(o => Vector3.Distance(startNode.transform.position, currentNode.transform.position) + Vector3.Distance(currentNode.transform.position, destination.transform.position));
						}
					}
				}
			}
		}

		path.Clear();

		PathNode current = destination;
		while (true)
		{
			path.Add(current);

			if (current.parent == null)
			{
				break;
			}

			current = current.parent;
		}

		path.Reverse();

		foreach (PathNode pathNode in FindObjectsOfType<PathNode>())
		{
			pathNode.parent = null;
		}
	}

#if UNITY_EDITOR
	private void OnDrawGizmos()
	{
		if (UnityEditor.Selection.activeObject == gameObject && Application.isPlaying && destination != null)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawCube(destination.transform.position, Vector3.one * 1.5f);
		}
	}
#endif

	protected virtual void EnemyFound(Transform enemy)
	{

	}

	protected virtual void EnemyLost(Transform enemy)
	{
		
	}
}
