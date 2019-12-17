using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Segment
{
	internal static int currentID = 0;
	public int id;

	private Vector2Int currentPosition;
    public Vector2Int CurrentPosition
	{
		get
		{
			return currentPosition;
		}
		set
		{
			LastPostion = CurrentPosition;
			currentPosition = value;
		}
	}

	public Vector2Int LastPostion { get; set; }

	public Segment(Vector2Int position)
	{
		CurrentPosition = position;

		id = currentID;
		currentID++;
	}
}
