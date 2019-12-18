using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;

#if GVR
			nonVRSet.SetActive(false);
			vrSet.SetActive(true);
			myLookAt = vrCamLookAt;
#else
			nonVRSet.SetActive(true);
			vrSet.SetActive(false);
			myLookAt = camLookAt;
#endif
			GetComponentInChildren<Canvas>().worldCamera = myLookAt.GetComponent<Camera>();
		}
		else
		{
			Destroy(gameObject);
		}
	}

	public DungeonMaster dungeonMaster;
	public PathNodesMaster pathNodesMaster;

	public GameObject playerPrefab;
	public GameObject enemyPrefab;

	[Space]
	public CameraLookAt camLookAt;
	public CameraLookAt vrCamLookAt;
	private CameraLookAt myLookAt;
	public GameObject nonVRSet;
	public GameObject vrSet;

	[Space]
	public Canvas loadingCanvas;
	public RectTransform loadingBar;
	public Text loadingMessage;

    // Start is called before the first frame update
    void Start()
    {
		CreateDungeon();
	}

	void CreateDungeon()
	{
		UnityEvent completedDungeon = new UnityEvent();
		completedDungeon.AddListener(CompletedDungeon);

		dungeonMaster.CreateDungeon(completedDungeon);
	}

	void CompletedDungeon()
	{
		UnityEvent completedNodePlacement = new UnityEvent();
		completedNodePlacement.AddListener(CompletedNodePlacement);

		PathNodesMaster.Instance.StartCoroutine(PathNodesMaster.GenerateDungeonNodes(dungeonMaster.tiles, completedNodePlacement));
	}

	void CompletedNodePlacement()
	{
		UnityEvent completedPathing = new UnityEvent();
		completedPathing.AddListener(CompletedPathing);

		PathNodesMaster.Instance.StartCoroutine(PathNodesMaster.BuildPaths(completedPathing));
	}

	void CompletedPathing()
	{
		if (dungeonMaster.rooms.Count > 0)
		{
			List<Room> availableRooms = dungeonMaster.rooms.Where(o => (o.settings & RoomSettings.PlayerSpawnable) == RoomSettings.PlayerSpawnable).ToList();

			Room room = availableRooms[Random.Range(0, availableRooms.Count)];
			Vector2Int pos = room.tiles[Random.Range(0, room.tiles.Length)];

			GameObject newplayer = Instantiate(playerPrefab, new Vector3(pos.x, 1, pos.y), Quaternion.identity);
			newplayer.GetComponent<PathFinder>().cam = myLookAt.GetComponent<Camera>();
			myLookAt.target = newplayer.transform;

			//Get enemy available
			availableRooms = dungeonMaster.rooms.Where(
				o => ((o.settings & RoomSettings.EnemySpawnable) == RoomSettings.EnemySpawnable) && 
				o != room).ToList();

			for (int i = availableRooms.Count-1; i >= 0; i--)
			{
				room = availableRooms[i];
				pos = room.tiles[Random.Range(0, room.tiles.Length)];

				newplayer = Instantiate(enemyPrefab, new Vector3(pos.x, 1, pos.y), Quaternion.identity);
				newplayer.GetComponent<PathFinder>().cam = myLookAt.GetComponent<Camera>();

				availableRooms.RemoveAt(i);
			}

			loadingCanvas.gameObject.SetActive(false);
		}
		else
		{
			DestroyAll();
		}
	}

	private void DestroyAll()
	{
		UnityEvent completedPathRemoval = new UnityEvent();
		completedPathRemoval.AddListener(CompletedPathRemoval);

		PathNodesMaster.Instance.StartCoroutine(PathNodesMaster.ClearAllNodes(completedPathRemoval));
	}

	void CompletedPathRemoval()
	{
		for (int i = pathNodesMaster.transform.childCount-1; i >= 0; i--)
		{
			Destroy(pathNodesMaster.transform.GetChild(i));
		}
	}

    // Update is called once per frame
    void Update()
    {
		if (Input.GetKeyDown(KeyCode.R))
		{
			SceneManager.LoadScene(0);
		}else if (Input.GetKeyDown(KeyCode.Escape))
		{
			Application.Quit();
		}
    }

	public void UpdateDungeonLoading(float percentageComplete, string message)
	{
		loadingBar.localScale = new Vector3(percentageComplete, loadingBar.localScale.y, loadingBar.localScale.z);
		loadingMessage.text = message;
	}
}
