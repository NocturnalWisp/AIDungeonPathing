using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Flags]
public enum RoomSettings
{
	PlayerSpawnable = 1 << 0,
	EnemySpawnable = 1 << 1
}

[System.Serializable]
public class Room
{
	public Vector2Int[] tiles;

	public RoomSettings settings;

	public Vector2Int Center
	{
		get => new Vector2Int((int)tiles.Average(o => o.x), (int)tiles.Average(o => o.y));
	}

	public Vector2Int Min
	{
		get => new Vector2Int(tiles.Select(o => o.x).Min(), tiles.Select(o => o.y).Min());
	}

	public Vector2Int Max
	{
		get => new Vector2Int(tiles.Select(o => o.x).Max(), tiles.Select(o => o.y).Max());
	}

	public static Room operator +(Room a, Room b)
	{
		Room newRoom = new Room();

		List<Vector2Int> tiles = a.tiles.ToList();
		tiles.AddRange(b.tiles.ToList());

		newRoom.tiles = tiles.ToArray();

		return newRoom;
	}
}
