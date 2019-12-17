using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DoorBehavior : MonoBehaviour
{
	[System.Serializable]
	public class Connection
	{
		public PathNode a, b;

		public Connection(PathNode a, PathNode b)
		{
			this.a = a;
			this.b = b;
		}
	}

	public List<Connection> connections = new List<Connection>();

	Transform hinge;
	float hingeRotation;

	public bool autoClose = true;
	bool shouldAutoClose;
	public float autoCloseTimer;
	float autoCloseTicker;

	private void Start()
	{
		hinge = transform.Find("Hinge");
		hingeRotation = hinge.eulerAngles.y;
	}

	private void Update()
	{
		if (shouldAutoClose)
		{
			autoCloseTicker -= Time.deltaTime;

			if (autoCloseTicker <= 0)
			{
				Close();
				shouldAutoClose = false;
			}
		}
	}

	public void Close()
	{
		hinge.eulerAngles = new Vector3(hinge.eulerAngles.x, hingeRotation, hinge.eulerAngles.z);

		foreach (Connection connection in connections)
		{
			connection.a.connections.Find(o => o.pathNode = connection.b).closed = true;
			connection.b.connections.Find(o => o.pathNode = connection.a).closed = true;
		}
	}

	public void Open()
	{
		hinge.eulerAngles = new Vector3(hinge.eulerAngles.x, hingeRotation + 90f, hinge.eulerAngles.z);

		foreach (Connection connection in connections)
		{
			connection.a.connections.Find(o => o.pathNode = connection.b).closed = false;
			connection.b.connections.Find(o => o.pathNode = connection.a).closed = false;
		}

		if (autoClose)
		{
			shouldAutoClose = true;
			autoCloseTicker = autoCloseTimer;
		}
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(DoorBehavior))]
public class DoorBehaviorEditor : Editor
{
	DoorBehavior self;

	private void OnEnable()
	{
		self = (DoorBehavior)target;
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		if (GUILayout.Button("Open"))
		{
			self.Open();
		}
		if (GUILayout.Button("Close"))
		{
			self.Close();
		}
	}
}
#endif