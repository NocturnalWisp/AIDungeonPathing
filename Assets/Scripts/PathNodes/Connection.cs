using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Connection
{
	public Connection(PathNode pathNode, PathConnectionType type)
	{
		this.pathNode = pathNode;
		this.type = type;
	}

	public PathNode pathNode;
	public PathConnectionType type;

	public bool closed = false;

	[HideInInspector]
	public bool foldout = false;
}
