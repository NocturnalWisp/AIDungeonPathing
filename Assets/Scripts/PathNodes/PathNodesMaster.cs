using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;

public class PathNodesMaster : MonoBehaviour
{
	public static PathNodesMaster Instance;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}

	public GameObject node;
	public float spacing = 1;
	public float distance = 1;

	[Space]
	public int pathConnectionsWaitNumber;

	[Space]
	public float viewGizmoDistance;

	public static IEnumerator GenerateDungeonNodes(Tile[,] tiles, UnityEvent finished)
	{
		if (Instance.node == null)
		{
			Debug.LogWarning("Select a node prefab to create.");
		}
		else
		{
			for (int x = 0; x < tiles.GetLength(0); x++)
			{
				for (int y = 0; y < tiles.GetLength(1); y++)
				{
					if (tiles[x, y] != Tile.Air)
					{
						GameObject newNode = Instantiate(Instance.node, new Vector3(x, 0, y), Quaternion.identity);
						newNode.transform.parent = Instance.transform;

						newNode.name = "Node (" + x + ", " + y + ")";
					}
				}

				yield return new WaitForEndOfFrame();
			}
		}

		finished.Invoke();
	}

	public static IEnumerator ClearAllNodes(UnityEvent completed)
	{
		GameObject[] nodes = GameObject.FindGameObjectsWithTag("Node");
		for (int i = nodes.Length - 1, f = 0; i >= 0; i--, f++)
		{
			Destroy(nodes[i]);

			if (f % 1000 == 0)
				yield return new WaitForEndOfFrame();
		}

		completed.Invoke();
	}

	public static IEnumerator BuildPaths(UnityEvent completed)
	{
		PathNode[] nodes = FindObjectsOfType<PathNode>().Select(o => o.GetComponent<PathNode>()).ToArray();
		for (int i = 0; i < nodes.Length; i++)
		{
			for (int j = 0; j < nodes.Length; j++)
			{
				//Check node isn't already connected
				if (PathNode.CheckConnection(nodes[i], nodes[j]))
				{
					float spacing = Instance.spacing / 1.25f;
					Vector3 subtractedPos = (nodes[j].transform.position - nodes[i].transform.position);

					//Check distance between nodes
					if (Vector3.Distance(nodes[i].transform.position, nodes[j].transform.position) <= Instance.distance * 1.5f &&
						(!(nodes[i].type == PathNodeType.Jump && nodes[j].type == PathNodeType.Jump)) &&
						(!(nodes[i].type == PathNodeType.Drop && nodes[j].type == PathNodeType.DropOff)) &&
						(!(nodes[i].type == PathNodeType.DropOff && nodes[j].type == PathNodeType.Drop)))
					{
						if (!Physics.CheckBox(nodes[i].transform.position + subtractedPos / 2,
							new Vector3(spacing, spacing, spacing),
							Quaternion.identity, Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Ground", "Node"))
							)
						{
							nodes[i].connections.Add(new Connection(nodes[j], PathConnectionType.Normal));
						}
						else
						{
							nodes[i].connections.Add(new Connection(nodes[j], PathConnectionType.Narrow));
						}

						//Handle door nodes
						RaycastHit hit;

						//Check line of sight
						if (Physics.SphereCast(
							new Ray(nodes[i].transform.position, (nodes[j].transform.position - nodes[i].transform.position).normalized), 
							nodes[i].radius, 
							out hit, 
							Vector3.Distance(nodes[i].transform.position, nodes[j].transform.position), 
							Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Ground", "Node"), 
							QueryTriggerInteraction.Collide))
						{
							if (hit.collider.tag == "Door")
							{
								//Make sure not already added door connection
								if (hit.collider.GetComponentInParent<DoorBehavior>() != null)
								{
									hit.collider.GetComponentInParent<DoorBehavior>().connections.Add(new DoorBehavior.Connection(nodes[i], nodes[j]));
								}
							}
						}
					}
					else if (nodes[i].type == PathNodeType.Jump && nodes[j].type == PathNodeType.Jump &&
					   Vector3.Distance(nodes[i].transform.position, nodes[j].transform.position) <= Instance.distance * 2.25f
					   )
					{
						nodes[i].connections.Add(new Connection(nodes[j], PathConnectionType.Jump));
					}
					else if (nodes[i].type == PathNodeType.Drop && nodes[j].type == PathNodeType.DropOff &&
					   Vector3.Distance(nodes[i].transform.position, nodes[j].transform.position) <= Instance.distance * 2.25f
					   )
					{
						nodes[i].connections.Add(new Connection(nodes[j], PathConnectionType.Drop));
					}
				}
			}

			if (i % Instance.pathConnectionsWaitNumber == 0)
				yield return new WaitForEndOfFrame();
		}

		completed.Invoke();
	}

	IEnumerator ClearPaths(bool rebuild = false)
	{
		PathNode[] nodes = FindObjectsOfType<PathNode>().Select(o => o.GetComponent<PathNode>()).ToArray();
		for (int i = 0; i < nodes.Length; i++)
		{
			nodes[i].connections.Clear();

			if (i % 1000 == 0)
				yield return new WaitForEndOfFrame();
		}

		if (rebuild)
		{
			StartCoroutine(BuildPaths(new UnityEvent()));
		}
	}
}
