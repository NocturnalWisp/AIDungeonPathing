using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraLookAt : MonoBehaviour
{
	public Transform target;


	public float rotateSpeed;
	public float lookAtSpeed;

	public Vector3 offset;

	void Update()
	{
		if (target != null)
		{
			transform.parent.position = target.position - offset;

			transform.RotateAround(target.position, Vector3.up, -Input.GetAxis("Horizontal") * rotateSpeed * Time.deltaTime);

			transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, target.position - transform.position, lookAtSpeed * Time.deltaTime, 0.0f));
		}
	}
}
