using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraLookAt : MonoBehaviour
{
	public Transform target;

	private float originalY;

	public float rotateSpeed;
	public float lookAtSpeed;

	public Vector3 offset;

	bool once = true;

	void Update()
	{
		if (target != null)
		{
			if (once)
			{
				originalY = target.position.y;
				once = false;
			}

#if GVR
			transform.parent.position = new Vector3(target.position.x - offset.x, originalY - offset.y, target.position.z - offset.z);
#else
			transform.parent.position = new Vector3(target.position.x - offset.x, originalY - offset.y, target.position.z - offset.z);

			transform.RotateAround(target.position, Vector3.up, -Input.GetAxis("Horizontal") * rotateSpeed * Time.deltaTime);

			transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, target.position - transform.position, lookAtSpeed * Time.deltaTime, 0.0f));
#endif
		}
	}
}
