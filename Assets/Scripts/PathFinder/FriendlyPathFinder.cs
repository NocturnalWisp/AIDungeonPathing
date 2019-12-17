using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FriendlyPathFinder : PathFinder
{
	protected override void EnemyFound(Transform enemy)
	{
		base.EnemyFound(enemy);

		//Check for line of sight
		if (!Physics.Linecast(transform.position, enemy.position, Physics.AllLayers & ~LayerMask.GetMask("Player", "Enemy")))
		{
			if (enemy.GetComponent<HostilePathFinder>() != null)
			{
				target = enemy;
				destination = null;
				state = AIState.Flee;
			}
		}
	}

	protected override void EnemyLost(Transform enemy)
	{
		base.EnemyLost(enemy);

		target = null;
		state = AIState.Wander;
	}
}
