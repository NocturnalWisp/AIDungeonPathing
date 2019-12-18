using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HostilePathFinder : PathFinder
{
	public float yellRadius;

	public float killDistance;

	public float foundTimer;
	float ticker;

	bool foundFriendly = false;

	new void Start()
	{
		base.Start();
	}

	new void Update()
    {
		base.Update();

		ticker -= Time.deltaTime;
	}

	protected override void EnemyFound(Transform enemy)
	{
		base.EnemyFound(enemy);

		//Check for line of sight
		if (!Physics.Linecast(transform.position, enemy.position, Physics.AllLayers & ~LayerMask.GetMask("Player", "Enemy")))
		{
			if (enemy.GetComponent<FriendlyPathFinder>() != null)
			{
				target = enemy;
				destination = null;
				state = AIState.Follow;
				foundFriendly = true;

				if (Vector3.Distance(transform.position, enemy.position) <= killDistance)
				{
					UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
				}
				else
				{
					//Do a yell
					if (ticker <= 0)
					{
						Collider[] colliders = Physics.OverlapSphere(transform.position, yellRadius);

						foreach (Collider secondCollider in colliders)
						{
							HostilePathFinder finder = secondCollider.GetComponent<HostilePathFinder>();
							if (finder != null && finder.state == AIState.Wander)
							{
								finder.destination = prevNext;
								finder.GetOptimalPath(finder.bestPath, finder.next, finder.destination);
								finder.state = AIState.Wander;
								finder.nextIndex = -1;
							}
						}

						ticker = foundTimer;
					}
				}
			}
		}
	}

	protected override void EnemyLost(Transform enemy)
	{
		base.EnemyLost(enemy);

		if (foundFriendly)
		{
			destination = target.GetComponent<PathFinder>().prevNext;
			GetOptimalPath(bestPath, next, destination);
			state = AIState.Wander;
			nextIndex = -1;
			foundFriendly = false;
		}
		else
		{
			destination = null;
			target = null;
			state = AIState.Wander;
		}
	}
}
