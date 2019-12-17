using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class CastEvent : UnityEvent<Transform> { }

public class CastSphere : MonoBehaviour
{
	PathFinder pathFinder;

	public string enemyTag;

	private void Start()
	{
		pathFinder = GetComponentInParent<PathFinder>();
	}

	private void OnTriggerStay(Collider other)
	{
		if (other.tag == enemyTag)
		{
			pathFinder.enemyFound.Invoke(other.transform);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.tag == enemyTag)
		{
			pathFinder.enemyLost.Invoke(other.transform);
		}
	}
}
