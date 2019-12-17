using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class RoomAttempt
{
	public int roomRetries;
	public Vector2Int roomMin;
	public Vector2Int roomMax;

	public RoomSettings settings;
}

public class DungeonMaster : MonoBehaviour
{
	private int ogPathCount;
	public int pathCount = 800;
	public int mapWidth = 100, mapHeight = 100;
	internal Tile[,] tiles;

	private List<Segment> segments = new List<Segment>();

	[Range(0, 100)]
	public float straightPercent = 90f;
	[Range(0, 100)]
	public float turnPercent = 98f;

	[Space]
	public float secondsBetweenBuilding;

	[Space]

	[Tooltip("The number of times it will try to create a room attempt.")]
	public int roomTries = 5;

	public RoomAttempt[] roomAttempts;

	[Space]

	public List<Room> rooms;

	[Tooltip("This will allow the rooms to expand if every wall tile is touching a hall tile")]
	public bool expandRooms = true;

	[Tooltip("This will join rooms that are touching.")]
	public bool joinRooms = true;

	[Space]
	public GameObject roomFloorPrefab;
	public GameObject hallFloorPrefab;
	public GameObject wallPrefab;
	public GameObject doorPrefab;

	private UnityEvent completedDungeon;
	private UnityEvent finishedPath;

	[Space]
	public DungeonState state = DungeonState.Path;

	[Space]
	public Material defaultMaterial;

    void Awake()
    {
		ogPathCount = pathCount;
	}

	public void CreateDungeon(UnityEvent completedDungeon)
	{
		this.completedDungeon = completedDungeon;

		for (int i = 0; i < transform.childCount; i++)
		{
			Destroy(transform.GetChild(i).gameObject);
		}

		tiles = new Tile[mapWidth, mapHeight];

		pathCount = ogPathCount;

		segments.Clear();
		Segment.currentID = 0;

		Segment start = //new Segment(new Vector2Int(Random.Range(mapWidth / 4, (mapWidth / 4) * 3), Random.Range(mapHeight / 4, (mapHeight / 4) * 3)));
			new Segment(new Vector2Int(mapWidth / 2, mapHeight / 2));

		segments.Add(start);

		int lastPositionDeterminant = Random.Range(0, 4);

		switch (lastPositionDeterminant)
		{
			case 0:
				start.LastPostion = new Vector2Int(start.CurrentPosition.x, start.CurrentPosition.y - 1);
				break;
			case 1:
				start.LastPostion = new Vector2Int(start.CurrentPosition.x, start.CurrentPosition.y + 1);
				break;
			case 2:
				start.LastPostion = new Vector2Int(start.CurrentPosition.x - 1, start.CurrentPosition.y);
				break;
			case 3:
				start.LastPostion = new Vector2Int(start.CurrentPosition.x + 1, start.CurrentPosition.y);
				break;
		}

		this.completedDungeon.AddListener(CompletedDungeon);

		state = DungeonState.Path;

		finishedPath = new UnityEvent();
		finishedPath.AddListener(CompletedPath);
		StartCoroutine(BuildCriticalPath(finishedPath));
	}

	void CompletedPath()
	{
		state = DungeonState.Rooms;

		UnityEvent finishedRoomCreation = new UnityEvent();
		finishedRoomCreation.AddListener(CompletedRoomCreation);
		StartCoroutine(BuildRooms(finishedRoomCreation));
	}

	void CompletedRoomCreation()
	{
		//TODO: remove halls that lead nowhere ????

		if (expandRooms)
			ExpandRooms();

		BuildDoors();

		if (joinRooms)
			JoinRooms();

		state = DungeonState.Building;

		StartCoroutine(BuildDungeon(completedDungeon));
	}

	void CompletedDungeon()
	{
		state = DungeonState.Complete;

		GameManager.Instance.UpdateDungeonLoading(1f, "Dungeon Complete...");
	}

	#region CriticalPath
	IEnumerator BuildCriticalPath(UnityEvent completed)
	{
		while (pathCount > 0 && segments.Count > 0)
		{
			foreach (Segment segment in segments.ToArray()/*int i = 0; i < segments.Count && pathCount > 0; i++*/)
			{
				//Segment segment = segments[i];

				if (pathCount <= 0)
					break;

				yield return new WaitForSecondsRealtime(secondsBetweenBuilding);
				float pathPercentComplete = (float)pathCount / ogPathCount;

				if (pathPercentComplete != 0)
					GameManager.Instance.UpdateDungeonLoading((1 - pathPercentComplete) * 0.3333f, "Generating Critical Path...");
				else
					GameManager.Instance.UpdateDungeonLoading(0.3333f, "Generating Critical Path...");

				//Random variable for determining what the segment does
				int segmentChoice = Random.Range(1, 101);

				//Straight
				if (segmentChoice <= straightPercent)
				{
					GoStraight(segment);
				}
				//Turn
				else if (segmentChoice <= turnPercent)
				{
					Turn(segment);
				}
				//Split
				else if (pathCount >= 2)
				{
					Split(segment);
				}
				else
				{
					GoStraight(segment);
				}
			}
		}

		completed.Invoke();
	}

	private void GoStraight(Segment segment)
	{
		if (segment.CurrentPosition.x < segment.LastPostion.x)
		{
			if (segment.CurrentPosition.x - 1 >= 0)
			{
				segment.CurrentPosition = new Vector2Int(segment.CurrentPosition.x - 1, segment.CurrentPosition.y);
			}
			else
			{
				Turn(segment);
			}
		}
		else if (segment.CurrentPosition.x > segment.LastPostion.x)
		{
			if (segment.CurrentPosition.x + 1 <= mapWidth - 1)
			{
				segment.CurrentPosition = new Vector2Int(segment.CurrentPosition.x + 1, segment.CurrentPosition.y);
			}
			else
			{
				Turn(segment);
			}
		}
		else if (segment.CurrentPosition.y > segment.LastPostion.y)
		{
			if (segment.CurrentPosition.y + 1 <= mapHeight - 1)
			{
				segment.CurrentPosition = new Vector2Int(segment.CurrentPosition.x, segment.CurrentPosition.y + 1);
			}
			else
			{
				Turn(segment);
			}
		}
		else if (segment.CurrentPosition.y < segment.LastPostion.y)
		{
			if (segment.CurrentPosition.y - 1 >= 0)
			{
				segment.CurrentPosition = new Vector2Int(segment.CurrentPosition.x, segment.CurrentPosition.y - 1);
			}
			else
			{
				Turn(segment);
			}
		}

		tiles[segment.CurrentPosition.x, segment.CurrentPosition.y] = Tile.Hall;

		pathCount--;
	}

	private void Turn(Segment segment)
	{
		int choice = Random.Range(0, 2);

		//Up or Down
		if (segment.CurrentPosition.x < segment.LastPostion.x || segment.CurrentPosition.x > segment.LastPostion.x)
		{
			switch (choice)
			{
				case 0:
					//Not hitting top
					if (segment.CurrentPosition.y - 1 >= 0)
					{
						segment.CurrentPosition = new Vector2Int(segment.CurrentPosition.x, segment.CurrentPosition.y - 1);
					}
					else
					{
						segment.CurrentPosition = new Vector2Int(segment.CurrentPosition.x, segment.CurrentPosition.y + 1);
					}
					break;
				case 1:
					//Not hitting bottom
					if (segment.CurrentPosition.y + 1 <= mapHeight-1)
					{
						segment.CurrentPosition = new Vector2Int(segment.CurrentPosition.x, segment.CurrentPosition.y + 1);
					}
					else
					{
						segment.CurrentPosition = new Vector2Int(segment.CurrentPosition.x, segment.CurrentPosition.y - 1);
					}
					break;
			}
		}
		//Right or Left
		else if (segment.CurrentPosition.y < segment.LastPostion.y || segment.CurrentPosition.y > segment.LastPostion.y)
		{
			switch (choice)
			{
				case 0:
					//Not hitting left
					if (segment.CurrentPosition.x - 1 >= 0)
					{
						segment.CurrentPosition = new Vector2Int(segment.CurrentPosition.x - 1, segment.CurrentPosition.y);
					}
					else
					{
						segment.CurrentPosition = new Vector2Int(segment.CurrentPosition.x + 1, segment.CurrentPosition.y);
					}
					break;
				case 1:
					//Not hitting right
					if (segment.CurrentPosition.x + 1 <= mapWidth-1)
					{
						segment.CurrentPosition = new Vector2Int(segment.CurrentPosition.x + 1, segment.CurrentPosition.y);
					}
					else
					{
						segment.CurrentPosition = new Vector2Int(segment.CurrentPosition.x - 1, segment.CurrentPosition.y);
					}
					break;
			}
		}

		tiles[segment.CurrentPosition.x, segment.CurrentPosition.y] = Tile.Hall;

		pathCount--;
	}

	public void Split(Segment segment)
	{
		int ogListSize = segments.Count;
		int splitType = Random.Range(0, 4);

	#region Going Up
		if (segment.CurrentPosition.y < segment.LastPostion.y)
		{
			//Split left & right | left & up | right & up | right & left & up
			switch (splitType)
			{
				case 0:
					{
						CreateNewSegment(Direction.Left, segment);
						CreateNewSegment(Direction.Right, segment);
						break;
					}
				case 1:
					{
						CreateNewSegment(Direction.Left, segment);
						CreateNewSegment(Direction.Up, segment);
						break;
					}
				case 2:
					{
						CreateNewSegment(Direction.Right, segment);
						CreateNewSegment(Direction.Up, segment);
						break;
					}
				case 3:
					{
						CreateNewSegment(Direction.Left, segment);
						CreateNewSegment(Direction.Right, segment);
						CreateNewSegment(Direction.Up, segment);
						break;
					}
			}
		}
#endregion
	#region Going Down
		else if (segment.CurrentPosition.y > segment.LastPostion.y)
		{
			//Split left & right | left & down | right & down | right & left & down
			switch (splitType)
			{
				case 0:
					{
						CreateNewSegment(Direction.Left, segment);
						CreateNewSegment(Direction.Right, segment);
						break;
					}
				case 1:
					{
						CreateNewSegment(Direction.Left, segment);
						CreateNewSegment(Direction.Down, segment);
						break;
					}
				case 2:
					{
						CreateNewSegment(Direction.Right, segment);
						CreateNewSegment(Direction.Down, segment);
						break;
					}
				case 3:
					{
						CreateNewSegment(Direction.Left, segment);
						CreateNewSegment(Direction.Right, segment);
						CreateNewSegment(Direction.Down, segment);
						break;
					}
			}
		}
#endregion
	#region Going Left
		else if (segment.CurrentPosition.x < segment.LastPostion.x)
		{
			//Split up & down | left & down | left & up | left & up & down
			switch (splitType)
			{
				case 0:
					{
						CreateNewSegment(Direction.Up, segment);
						CreateNewSegment(Direction.Down, segment);
						break;
					}
				case 1:
					{
						CreateNewSegment(Direction.Left, segment);
						CreateNewSegment(Direction.Down, segment);
						break;
					}
				case 2:
					{
						CreateNewSegment(Direction.Left, segment);
						CreateNewSegment(Direction.Up, segment);
						break;
					}
				case 3:
					{
						CreateNewSegment(Direction.Left, segment);
						CreateNewSegment(Direction.Up, segment);
						CreateNewSegment(Direction.Down, segment);
						break;
					}
			}
		}
#endregion
	#region Going Right
		else if (segment.CurrentPosition.x > segment.LastPostion.x)
		{
			//Split up & down | right & down | right & up | right & up & down
			switch (splitType)
			{
				case 0:
					{
						CreateNewSegment(Direction.Up, segment);
						CreateNewSegment(Direction.Down, segment);
						break;
					}
				case 1:
					{
						CreateNewSegment(Direction.Right, segment);
						CreateNewSegment(Direction.Down, segment);
						break;
					}
				case 2:
					{
						CreateNewSegment(Direction.Right, segment);
						CreateNewSegment(Direction.Up, segment);
						break;
					}
				case 3:
					{
						CreateNewSegment(Direction.Right, segment);
						CreateNewSegment(Direction.Up, segment);
						CreateNewSegment(Direction.Down, segment);
						break;
					}
			}
		}
#endregion

		if (segments.Count > ogListSize)
		{
			pathCount -= segments.Count - ogListSize;
			
			//Remove old segment
			segments.Remove(segment);
		}
	}

	private void CreateNewSegment(Direction direction, Segment currentSegment)
	{
		switch (direction)
		{
			case Direction.Up:
				if (currentSegment.CurrentPosition.y - 1 >= 0)
				{
					Segment up = new Segment(new Vector2Int(currentSegment.CurrentPosition.x, currentSegment.CurrentPosition.y - 1));

					up.LastPostion = currentSegment.CurrentPosition;

					segments.Add(up);

					tiles[up.CurrentPosition.x, up.CurrentPosition.y] = Tile.Hall;
				}
				break;
			case Direction.Right:

				if (currentSegment.CurrentPosition.x + 1 <= mapWidth - 1)
				{
					Segment right = new Segment(new Vector2Int(currentSegment.CurrentPosition.x + 1, currentSegment.CurrentPosition.y));

					right.LastPostion = currentSegment.CurrentPosition;

					segments.Add(right);

					tiles[right.CurrentPosition.x, right.CurrentPosition.y] = Tile.Hall;
				}
				break;
			case Direction.Down:
				if (currentSegment.CurrentPosition.y + 1 <= mapHeight - 1)
				{
					Segment down = new Segment(new Vector2Int(currentSegment.CurrentPosition.x, currentSegment.CurrentPosition.y + 1));

					down.LastPostion = currentSegment.CurrentPosition;

					segments.Add(down);

					tiles[down.CurrentPosition.x, down.CurrentPosition.y] = Tile.Hall;
				}
				break;
			case Direction.Left:
				if (currentSegment.CurrentPosition.x - 1 >= 0)
				{
					Segment left = new Segment(new Vector2Int(currentSegment.CurrentPosition.x - 1, currentSegment.CurrentPosition.y));

					left.LastPostion = currentSegment.CurrentPosition;

					segments.Add(left);

					tiles[left.CurrentPosition.x, left.CurrentPosition.y] = Tile.Hall;
				}
				break;
		}
	}
	#endregion

	#region Rooms
	IEnumerator BuildRooms(UnityEvent finished)
	{
		rooms.Clear();

		foreach (RoomAttempt roomAttempt in roomAttempts)
		{
			int roomTriesCount = 0;
			bool builtRoom = false;

			while (roomTriesCount <= roomTries && !builtRoom)
			{
				int x = 0;
				int y = 0;

				int hallFindTries = 0;

				do
				{
					x = Random.Range(0, tiles.GetLength(0));
					y = Random.Range(0, tiles.GetLength(1));
					hallFindTries++;
				}
				while (tiles[x, y] != Tile.Hall && hallFindTries <= roomTries);

				if (tiles[x, y] == Tile.Hall)
				{
					//Build room
					int retries = 0;

					int width = Random.Range(roomAttempt.roomMin.x, roomAttempt.roomMax.x + 1);
					int height = Random.Range(roomAttempt.roomMin.y, roomAttempt.roomMax.y + 1);

					if (CheckInBounds(x + width, y + height))
					{
						while (!builtRoom && retries <= roomAttempt.roomRetries)
						{
							Room room = new Room();
							room.tiles = new Vector2Int[width * height];
							
							int startX = Random.Range(0, width + 1);
							int startY = Random.Range(0, height + 1);

							if (CheckInBounds(x - startX, y - startY))
							{
								if (!HasRoom(new Vector2Int(x - startX, y - startY),
									new Vector2Int(x + (width - startX), y + (height - startY))))
								{
									int roomTilesIndex = 0;

									for (int roomX = x - startX; roomX < x + (width - startX); roomX++)
									{
										for (int roomY = y - startY; roomY < y + (height - startY); roomY++)
										{
											room.tiles[roomTilesIndex] = new Vector2Int(roomX, roomY);
											roomTilesIndex++;

											tiles[roomX, roomY] = Tile.Room;
										}
									}

									room.settings = roomAttempt.settings;

									rooms.Add(room);

									builtRoom = true;
								}
							}

							retries++;
						}
					}
				}

				roomTriesCount++;

				yield return new WaitForSecondsRealtime(secondsBetweenBuilding);
				GameManager.Instance.UpdateDungeonLoading(
					(((roomAttempts.ToList().IndexOf(roomAttempt) + 1f) / roomAttempts.Length) * 0.3333f) + 0.3333f, 
					"Generating Rooms...");
			}
		}

		finished.Invoke();
	}

	private bool HasRoom(Vector2Int start, Vector2Int end)
	{
		for (int x = start.x; x < end.x; x++)
		{
			for (int y = start.y; y < end.y; y++)
			{
				if (tiles[x, y] == Tile.Room)
				{
					return true;
				}
			}
		}

		return false;
	}

	void ExpandRooms()
	{
		foreach (Room room in rooms)
		{
			//Get the extremes
			Vector2Int min = room.Min, max = room.Max;

			List<Vector2Int> wall = null;

			//North Wall
			wall = room.tiles.Where(o => o.y == max.y).ToList();
			ExpandWall(room, wall, new Vector2Int(0, 1));
			//South Wall
			wall = room.tiles.Where(o => o.y == min.y).ToList();
			ExpandWall(room, wall, new Vector2Int(0, -1));
			//East Wall
			wall = room.tiles.Where(o => o.x == max.x).ToList();
			ExpandWall(room, wall, new Vector2Int(1, 0));
			//West Wall
			wall = room.tiles.Where(o => o.x == min.x).ToList();
			ExpandWall(room, wall, new Vector2Int(-1, 0));
		}
	}

	void ExpandWall(Room room, List<Vector2Int> wall, Vector2Int direction)
	{
		bool canExpand = true;
		foreach(Vector2Int tilePosition in wall)
		{
			//Check to make sure not working outside bounds
			if (CheckInBounds(tilePosition + direction))
			{
				//Can't expand in wall + direction runs into air tiles
				if (tiles[tilePosition.x + direction.x, tilePosition.y + direction.y] == Tile.Air)
				{
					canExpand = false;
					break;
				}
			}
			else
			{
				canExpand = false;
				break;
			}
		}

		if (canExpand)
		{
			List<Vector2Int> nextWall = wall.Select(o => o + direction).ToList();

			List<Vector2Int> roomTiles = room.tiles.ToList();
			roomTiles.AddRange(
				nextWall.
				Where(
					//Don't add room tiles */ 
					o => tiles[o.x, o.y] != Tile.Room
				)
			);
			room.tiles = roomTiles.ToArray();

			//Set all tiles to the room type
			foreach (Vector2Int roomTile in nextWall)
			{
				tiles[roomTile.x, roomTile.y] = Tile.Room;
			}

			ExpandWall(room, nextWall, direction);
		}
	}

	void JoinRooms()
	{
		foreach (Room room in rooms)
		{
			//Get the extremes
			Vector2Int min = room.Min, max = room.Max;

			List<Vector2Int> wall = null;
			Room otherRoom = null;

			//North Wall
			wall = room.tiles.Where(o => o.y == max.y).ToList();
			otherRoom = TouchingRoom(room, wall, new Vector2Int(0, 1));
			if (otherRoom != null)
			{
				Room newRoom = otherRoom + room;

				newRoom.settings = room.settings & otherRoom.settings;

				rooms.Remove(otherRoom);

				otherRoom = null;
				Room myRoom = rooms.Find(o => o == room);
				rooms.Remove(myRoom);
				myRoom = null;

				rooms.Add(newRoom);

				JoinRooms();
				break;
			}
			//South Wall
			wall = room.tiles.Where(o => o.y == min.y).ToList();
			otherRoom = TouchingRoom(room, wall, new Vector2Int(0, -1));
			if (otherRoom != null)
			{
				Room newRoom = otherRoom + room;

				newRoom.settings = room.settings & otherRoom.settings;

				rooms.Remove(otherRoom);

				otherRoom = null;
				Room myRoom = rooms.Find(o => o == room);
				rooms.Remove(myRoom);
				myRoom = null;

				rooms.Add(newRoom);

				JoinRooms();
				break;
			}
			//East Wall
			wall = room.tiles.Where(o => o.x == max.x).ToList();
			otherRoom = TouchingRoom(room, wall, new Vector2Int(1, 0));
			if (otherRoom != null)
			{
				Room newRoom = otherRoom + room;

				newRoom.settings = room.settings & otherRoom.settings;

				rooms.Remove(otherRoom);

				otherRoom = null;
				Room myRoom = rooms.Find(o => o == room);
				rooms.Remove(myRoom);
				myRoom = null;

				rooms.Add(newRoom);

				JoinRooms();
				break;
			}
			//West Wall
			wall = room.tiles.Where(o => o.x == min.x).ToList();
			otherRoom = TouchingRoom(room, wall, new Vector2Int(-1, 0));
			if (otherRoom != null)
			{
				Room newRoom = otherRoom + room;

				newRoom.settings = room.settings & otherRoom.settings;

				rooms.Remove(otherRoom);

				otherRoom = null;
				Room myRoom = rooms.Find(o => o == room);
				rooms.Remove(myRoom);
				myRoom = null;

				rooms.Add(newRoom);

				JoinRooms();
				break;
			}
		}
	}

	Room TouchingRoom(Room room, List<Vector2Int> wall, Vector2Int direction)
	{
		foreach(Vector2Int tilePosition in wall)
		{
			//Check working within bounds
			if (CheckInBounds(tilePosition + direction))
			{
				if (tiles[tilePosition.x + direction.x, tilePosition.y + direction.y] == Tile.Room)
				{
					//Find room tile in other rooms
					foreach (Room otherRoom in rooms.Where(o => o != room))
					{
						if (otherRoom.tiles.Contains(tilePosition + direction))
						{
							return otherRoom;
						}
					}
				}
			}
		}

		return null;
	}
	#endregion

	#region Doors
	private void BuildDoors()
	{
		//Build walls by averaging position between tiles
		foreach (Room room in rooms)
		{
			//Get the extremes
			Vector2Int min = room.Min, max = room.Max;

			List<Vector2Int> wall = null;

			//North Wall
			wall = room.tiles.Where(o => o.y == max.y).ToList();
			Vector2Int? pos = CheckWall(wall, new Vector2Int(0, 1));
			if (pos != null)
			{
				GameObject door = Instantiate(doorPrefab);
				door.transform.position = new Vector3(((Vector2Int)pos).x, 0, ((Vector2Int)pos).y + 0.5f);
				door.transform.rotation = Quaternion.Euler(0, -90, 0);
				door.transform.parent = transform;
			}
			//South Wall
			wall = room.tiles.Where(o => o.y == min.y).ToList();
			pos = CheckWall(wall, new Vector2Int(0, -1));
			if (pos != null)
			{
				GameObject door = Instantiate(doorPrefab);
				door.transform.position = new Vector3(((Vector2Int)pos).x, 0, ((Vector2Int)pos).y - 0.5f);
				door.transform.rotation = Quaternion.Euler(0, 90, 0);
				door.transform.parent = transform;
			}
			//East Wall
			wall = room.tiles.Where(o => o.x == max.x).ToList();
			pos = CheckWall(wall, new Vector2Int(1, 0));
			if (pos != null)
			{
				GameObject door = Instantiate(doorPrefab);
				door.transform.position = new Vector3(((Vector2Int)pos).x + 0.5f, 0, ((Vector2Int)pos).y);
				door.transform.parent = transform;
			}
			//West Wall
			wall = room.tiles.Where(o => o.x == min.x).ToList();
			pos = CheckWall(wall, new Vector2Int(-1, 0));
			if (pos != null)
			{
				GameObject door = Instantiate(doorPrefab);
				door.transform.position = new Vector3(((Vector2Int)pos).x - 0.5f, 0, ((Vector2Int)pos).y);
				door.transform.rotation = Quaternion.Euler(0, -180, 0);
				door.transform.parent = transform;
			}
		}
	}

	/// <summary>
	/// Returns a value if the wall is good to place one door in that position
	/// </summary>
	/// <returns></returns>
	Vector2Int? CheckWall(List<Vector2Int> wall, Vector2Int direction)
	{
		Vector2Int doorPos = Vector2Int.zero;
		int wallCount = 0;
		foreach (Vector2Int tilePosition in wall)
		{
			//Make sure not trying to access tiles that are beyond the bounds
			if (CheckInBounds(tilePosition + direction) &&
				CheckInBounds(tilePosition - direction) &&
				CheckInBounds(tilePosition + SwapValues(direction)) &&
				CheckInBounds(tilePosition - SwapValues(direction)))
			{
				if (tiles[tilePosition.x + direction.x, tilePosition.y + direction.y] == Tile.Hall)
				{
					doorPos = tilePosition;
					wallCount++;


					//Make sure perpendicular tiles are not halls
					if (tiles[tilePosition.x + direction.y, tilePosition.y + direction.x] == Tile.Hall ||
						tiles[tilePosition.x - direction.y, tilePosition.y - direction.x] == Tile.Hall)
					{
						//Basically break out because there is a hall tile open next to it
						wallCount = -1;
					}

					//Make sure not trying to access tiles that are beyond the bounds
					if (CheckInBounds(tilePosition + direction + SwapValues(direction)) &&
						CheckInBounds(tilePosition + direction - SwapValues(direction)))
					{
						//Check air both sides on hall tile
						if ((tiles[tilePosition.x + direction.x + direction.y, tilePosition.y + direction.y + direction.x] != Tile.Air ||
						tiles[tilePosition.x + direction.x - direction.y, tilePosition.y + direction.y - direction.x] != Tile.Air) &&
						//Perpendicular is hall and same perpendicular for this tile is air
						(
						 (tiles[tilePosition.x + direction.x + direction.y, tilePosition.y + direction.y + direction.x] != Tile.Hall &&
						  tiles[tilePosition.x + direction.y, tilePosition.y + direction.x] != Tile.Air) ||
						 (tiles[tilePosition.x + direction.x - direction.y, tilePosition.y + direction.y - direction.x] != Tile.Hall &&
						  tiles[tilePosition.x - direction.y, tilePosition.y - direction.x] != Tile.Air)
						))
						{
							wallCount = -1;
						}
					}

					if (wallCount != 1)
					{
						break;
					}
				}
			}
			else
			{
				wallCount = -1;
				break;
			}
		}

		if (wallCount == 1)
		{
			return doorPos;
		}
		else
		{
			return null;
		}
	}
	#endregion

	#region Build Dungeon
	IEnumerator BuildDungeon(UnityEvent finished)
	{
		List<Vector2Int> allCoords = new List<Vector2Int>();
		for (int x = 0; x < tiles.GetLength(0); x++)
		{
			for (int y = 0; y < tiles.GetLength(1); y++)
			{
				allCoords.Add(new Vector2Int(x, y));
			}
		}

		Shuffle(allCoords);

		foreach (Vector2Int coord in allCoords)
		{
			GameObject ground = null;

			if (tiles[coord.x, coord.y] == Tile.Room)
			{
				ground = Instantiate(roomFloorPrefab);
			}
			else if (tiles[coord.x, coord.y] == Tile.Hall)
			{
				ground = Instantiate(hallFloorPrefab);
			}

			if (tiles[coord.x, coord.y] != Tile.Air)
			{
				ground.transform.position = new Vector3(coord.x, 0, coord.y);
				ground.transform.parent = transform;
			}

			#region Walls
			if (tiles[coord.x, coord.y] != Tile.Air)
			{
				if ((coord.x < tiles.GetLength(0) - 1 && tiles[coord.x + 1, coord.y] == Tile.Air) ||
					//Also create wall aat end of bounds
					coord.x == tiles.GetLength(0) - 1)
				{
					GameObject wall = Instantiate(wallPrefab);
					wall.transform.position = new Vector3((coord.x + (coord.x + 1f)) / 2, 0, coord.y);
					wall.transform.parent = transform;
				}
				if ((coord.x > 0 && tiles[coord.x - 1, coord.y] == Tile.Air) ||
					//Also create wall aat end of bounds
					coord.x == 0)
				{
					GameObject wall = Instantiate(wallPrefab);
					wall.transform.position = new Vector3((coord.x + (coord.x - 1f)) / 2, 0, coord.y);
					wall.transform.parent = transform;
				}
				if ((coord.y < tiles.GetLength(1) - 1 && tiles[coord.x, coord.y + 1] == Tile.Air) ||
					//Also create wall aat end of bounds
					coord.y == tiles.GetLength(1) - 1)
				{
					GameObject wall = Instantiate(wallPrefab);
					wall.transform.position = new Vector3(coord.x, 0, (coord.y + (coord.y + 1f)) / 2);
					wall.transform.rotation = Quaternion.Euler(0, 90, 0);
					wall.transform.parent = transform;
				}
				if ((coord.y > 0 && tiles[coord.x, coord.y - 1] == Tile.Air) ||
					//Also create wall aat end of bounds
					coord.y == 0)
				{
					GameObject wall = Instantiate(wallPrefab);
					wall.transform.position = new Vector3(coord.x, 0, (coord.y + (coord.y - 1f)) / 2);
					wall.transform.rotation = Quaternion.Euler(0, 90, 0);
					wall.transform.parent = transform;
				}
			}
			#endregion

			if (allCoords.IndexOf(coord) % 500 == 0)
				yield return new WaitForSecondsRealtime(secondsBetweenBuilding);
			GameManager.Instance.UpdateDungeonLoading(
				(((allCoords.IndexOf(coord) + 1f) / allCoords.Count) * 0.3333f) + 0.6666f,
				"Building Dungeon...");
		}

		CombineHallTiles();

		finished.Invoke();
	}
	#endregion

	#region Utilities
	public void CreateCube(float x, float y)
	{
		GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
		cube.transform.position = new Vector3(x, 0, y);
		cube.transform.parent = transform;
	}

	internal bool CheckInBounds(Vector2Int pos)
	{
		return pos.x >= 0 && pos.y >= 0 &&
			pos.x < tiles.GetLength(0) && pos.y < tiles.GetLength(1);
	}

	internal bool CheckInBounds(int x, int y)
	{
		return x >= 0 && y >= 0 &&
			x < tiles.GetLength(0) && y < tiles.GetLength(1);
	}

	internal Vector2Int SwapValues(Vector2Int pos)
	{
		return new Vector2Int(pos.y, pos.x);
	}

	public void Shuffle<T>(IList<T> list)
	{
		int n = list.Count;
		while (n > 1)
		{
			n--;
			int k = Random.Range(0, n + 1);
			T value = list[k];
			list[k] = list[n];
			list[n] = value;
		}
	}

	void CombineHallTiles()
	{
		MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>()
			.Where(o => o.tag == "HallTile")
			.ToArray();

		GameObject ground = new GameObject();

		ground.name = "Halls";
		ground.layer = LayerMask.NameToLayer("Ground");

		MeshFilter meshFilter = ground.AddComponent<MeshFilter>();
		MeshRenderer meshRenderer = ground.AddComponent<MeshRenderer>();

		meshRenderer.material = defaultMaterial;

		CombineInstance[] combine = new CombineInstance[meshFilters.Length];

		int i = 0;
		while (i < meshFilters.Length)
		{
			combine[i].mesh = meshFilters[i].mesh;
			combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
			meshFilters[i].gameObject.SetActive(false);

			i++;
		}

		meshFilter.mesh = new Mesh();
		meshFilter.mesh.CombineMeshes(combine);

		ground.AddComponent<MeshCollider>();
	}
	#endregion
}

/*
#region Custom Editor
#if UNITY_EDITOR
[CustomEditor(typeof(DungeonMaster))]
[CanEditMultipleObjects]
public class DungeonMasterEditor : Editor
{
	SerializedProperty pathCount,
		mapWidth, mapHeight,
		straightPercent, turnPercent,
		runPause, secondsBetweenBuilding;

	SerializedProperty groundPrefab;

	private void OnEnable()
	{
		pathCount = serializedObject.FindProperty("pathCount");

		mapWidth = serializedObject.FindProperty("mapWidth");
		mapHeight = serializedObject.FindProperty("mapHeight");

		straightPercent = serializedObject.FindProperty("straightPercent");
		turnPercent = serializedObject.FindProperty("turnPercent");

		runPause = serializedObject.FindProperty("runPause");
		secondsBetweenBuilding = serializedObject.FindProperty("secondsBetweenBuilding");

		groundPrefab = serializedObject.FindProperty("groundPrefab");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		EditorGUILayout.PropertyField(pathCount, new GUIContent("Path Count"));

		Vector2Int map = EditorGUILayout.Vector2IntField("Map Size", new Vector2Int(mapWidth.intValue, mapHeight.intValue));

		mapWidth.intValue = map.x;
		mapHeight.intValue = map.y;

		straightPercent.floatValue = EditorGUILayout.Slider(new GUIContent("Straight Chance"), straightPercent.floatValue, 0f, 100f);

		turnPercent.floatValue = EditorGUILayout.Slider(new GUIContent("Turn Chance"), turnPercent.floatValue, 0f, 100f);

		turnPercent.floatValue = Mathf.Clamp(turnPercent.floatValue, straightPercent.floatValue, 100f);

		EditorGUILayout.PropertyField(runPause, new GUIContent("Create Over Time"));

		if (runPause.boolValue)
		{
			EditorGUI.indentLevel = 1;
			EditorGUILayout.PropertyField(secondsBetweenBuilding, new GUIContent("Wait Time"));
			EditorGUI.indentLevel = 0;
		}

		EditorGUILayout.Space();

		EditorGUILayout.ObjectField(groundPrefab, typeof(GameObject));

		serializedObject.ApplyModifiedProperties();
	}
}
#endif
#endregion
*/
