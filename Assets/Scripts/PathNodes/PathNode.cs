using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PathNode : MonoBehaviour
{
	public List<Connection> connections = new List<Connection>();

	public PathNodeType type;

	public float radius;

	bool showingSphere;

	//For determining path
	internal PathNode parent;

#if UNITY_EDITOR
	private void OnDrawGizmos()
	{
		if (Vector3.Distance(transform.position, Camera.current.transform.position) < PathNodesMaster.Instance.viewGizmoDistance && 
			!showingSphere)
		{
			switch (type)
			{
				case PathNodeType.Normal:
					Gizmos.color = Color.green;
					break;
				case PathNodeType.Drop:
					Gizmos.color = Color.yellow;
					Handles.Label(transform.position + Vector3.up * radius * 1.25f, "Start",
						new GUIStyle() { normal = new GUIStyleState() { textColor = Color.black } });
					break;
				case PathNodeType.DropOff:
					Gizmos.color = Color.yellow;
					Handles.Label(transform.position + Vector3.up * radius * 1.25f, "End",
						new GUIStyle() { normal = new GUIStyleState() { textColor = Color.black } });
					break;
				case PathNodeType.Jump:
					Gizmos.color = Color.cyan;
					break;
			}
			Gizmos.DrawSphere(transform.position, radius);
		}
		else if (Vector3.Distance(transform.position, Camera.current.transform.position) < PathNodesMaster.Instance.viewGizmoDistance)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawSphere(transform.position, radius * 1.5f);

			showingSphere = false;
		}

		for (int i = 0; i < connections.Count; i++)
		{
			if (connections[i].pathNode == null)
			{
				continue;
			}

			if (Vector3.Distance(transform.position, Camera.current.transform.position) < PathNodesMaster.Instance.viewGizmoDistance)
			{
				switch (connections[i].type)
				{
					case PathConnectionType.Normal:
						Gizmos.color = Color.black;
						break;
					case PathConnectionType.Narrow:
						Gizmos.color = Color.magenta;
						break;
					case PathConnectionType.Jump:
						Gizmos.color = Color.cyan;
						break;
					case PathConnectionType.Drop:
						Gizmos.color = Color.yellow;

						Gizmos.DrawLine(transform.position, new Vector3(
							connections[i].pathNode.transform.position.x,
							transform.position.y,
							connections[i].pathNode.transform.position.z
						));
						Gizmos.DrawLine(new Vector3(
							connections[i].pathNode.transform.position.x,
							transform.position.y,
							connections[i].pathNode.transform.position.z
						), connections[i].pathNode.transform.position);
						break;
				}

				if (connections[i].closed)
				{
					Handles.Label(Vector3.Lerp(connections[i].pathNode.transform.position, transform.position, 0.5f),
						"Closed",
						new GUIStyle() { normal = new GUIStyleState() { textColor = Color.black } });
				}

				if (connections[i].type != PathConnectionType.Drop)
				{
					Gizmos.DrawLine(transform.position - Vector3.up * radius, connections[i].pathNode.transform.position + Vector3.up * connections[i].pathNode.radius);
				}
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		//Show connections
		foreach (Connection connection in connections)
		{
			connection.pathNode.showingSphere = true;
		}
	}
#endif

	public static bool CheckConnection(PathNode a, PathNode b)
	{
		//Check node isn't already connected
		if (!a.connections.Exists(o => o.pathNode == b))
		{
			//Check if node is not self
			if (a != b)
			{
				//Check sphere around node
				if (!Physics.CheckSphere(a.transform.position, a.GetComponent<PathNode>().radius, Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Ground", "Node")))
				{
					//Check sphere around other node
					if (!Physics.CheckSphere(b.transform.position, b.GetComponent<PathNode>().radius, Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Ground", "Node")))
					{
						RaycastHit hit;

						//Check line of sight
						if (!Physics.SphereCast(new Ray(a.transform.position, (b.transform.position - a.transform.position).normalized), a.radius, out hit, Vector3.Distance(a.transform.position, b.transform.position), Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Ground", "Node")))
						{
							return true;
						}
						else
						{
							if (hit.collider.tag == "Door")
							{
								return true;
							}
						}
					}
				}
			}
		}

		return false;
	}

	private void OnDestroy()
	{
		//Remove all connections
		for (int i = 0; i < connections.Count; i++)
		{
			PathNode otherNode = connections[i].pathNode;

			for (int f = otherNode.connections.Count - 1; f >= 0; f--)
			{
				if (otherNode.connections[f].pathNode == this)
				{
					otherNode.connections.RemoveAt(f);
				}
			}
		}

		connections.Clear();
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(PathNode))]
[CanEditMultipleObjects]
public class PathNodeEditor : Editor
{
	SerializedProperty radius, pathNodeType, connections;

	bool foldout = true;
	GameObject toConnect;
	PathConnectionType toConnectType;

	PathNode me;

	private void OnEnable()
	{
		radius = serializedObject.FindProperty("radius");
		pathNodeType = serializedObject.FindProperty("type");
		connections = serializedObject.FindProperty("connections");

		me = (PathNode)target;
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		EditorGUILayout.PropertyField(radius, new GUIContent("Radius"));
		EditorGUILayout.PropertyField(pathNodeType, new GUIContent("Node Type"));

		//Hide connections and connection controls if there are no connections
		GUI.enabled = connections.arraySize > 0;

		foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, "Connections");
		using (new EditorGUI.IndentLevelScope())
		{

			if (foldout)
			{
				for (int i = 0; i < connections.arraySize; i++)
				{
					if (connections.GetArrayElementAtIndex(i).FindPropertyRelative("foldout") == null)
					{
						break;
					}

					SerializedProperty foldout = connections.GetArrayElementAtIndex(i).FindPropertyRelative("foldout");
					foldout.boolValue = EditorGUILayout.Foldout(foldout.boolValue, "Connection To " +
						((PathNode)connections.GetArrayElementAtIndex(i).FindPropertyRelative("pathNode").objectReferenceValue).name);

					if (foldout.boolValue)
					{
						EditorGUILayout.BeginVertical(EditorStyles.helpBox);

						EditorGUILayout.PropertyField(connections.GetArrayElementAtIndex(i).FindPropertyRelative("pathNode"));
						EditorGUILayout.PropertyField(connections.GetArrayElementAtIndex(i).FindPropertyRelative("type"));
						EditorGUILayout.PropertyField(connections.GetArrayElementAtIndex(i).FindPropertyRelative("closed"));

						if (GUILayout.Button("Remove One Way Connection"))
						{
							connections.DeleteArrayElementAtIndex(i);
						}
						else if (GUILayout.Button("Remove Two Way Connection"))
						{
							List<Connection> otherConnections = ((PathNode)connections.GetArrayElementAtIndex(i).FindPropertyRelative("pathNode").objectReferenceValue).connections;

							int? otherConnectionIndex = null;
							for (int f = 0; f < otherConnections.Count; f++)
							{
								if (otherConnections[f].pathNode ==
									(PathNode)connections.GetArrayElementAtIndex(i).FindPropertyRelative("pathNode").serializedObject.targetObject)
								{
									otherConnectionIndex = f;
								}
							}

							if (otherConnectionIndex != null)
							{
								otherConnections.RemoveAt((int)otherConnectionIndex);
							}

							connections.DeleteArrayElementAtIndex(i);
						}

						EditorGUILayout.EndVertical();
					}
				}
			}

			EditorGUILayout.BeginVertical(EditorStyles.helpBox);

			if (GUILayout.Button("Remove All One Way Connections"))
			{
				for (int i = connections.arraySize - 1; i >= 0; i--)
				{
					connections.DeleteArrayElementAtIndex(i);
				}
			}
			if (GUILayout.Button("Remove All Connections"))
			{
				for (int i = 0; i < connections.arraySize; i++)
				{
					PathNode otherNode = ((PathNode)connections.GetArrayElementAtIndex(i).FindPropertyRelative("pathNode").objectReferenceValue);

					for (int f = otherNode.connections.Count - 1; f >= 0; f--)
					{
						if (otherNode.connections[f].pathNode ==
							(PathNode)connections.GetArrayElementAtIndex(i).FindPropertyRelative("pathNode").serializedObject.targetObject)
						{
							otherNode.connections.RemoveAt(f);
						}
					}
				}

				for (int i = connections.arraySize - 1; i >= 0; i--)
				{
					connections.DeleteArrayElementAtIndex(i);
				}
			}

			EditorGUILayout.EndVertical();
		}

		GUI.enabled = true;

		//To force node connections
		toConnect = (GameObject)EditorGUILayout.ObjectField("To Connect To", toConnect, typeof(GameObject), true);
		toConnectType = (PathConnectionType)EditorGUILayout.EnumPopup("Connection Type", toConnectType);

		if (toConnect != null)
		{
			if (!toConnect.GetComponent<PathNode>())
			{
				toConnect = null;
			}
			else
			{
				if (GUILayout.Button("Force Connect"))
				{
					me.connections.Add(new Connection(toConnect.GetComponent<PathNode>(), toConnectType));
				}
				if (GUILayout.Button("Force Two Way Connection"))
				{
					//Make sure connection doesn't already exist
					if (!toConnect.GetComponent<PathNode>().connections.Exists(o => o.pathNode == me))
					{
						toConnect.GetComponent<PathNode>().connections.Add(new Connection(me, toConnectType));
					}

					me.connections.Add(new Connection(toConnect.GetComponent<PathNode>(), toConnectType));
				}
			}
		}

		serializedObject.ApplyModifiedProperties();
	}

	void HorizontalLine(int height = 1)
	{
		Rect rect = EditorGUILayout.GetControlRect(false, height);

		rect.height = height;

		EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
	}
}
#endif