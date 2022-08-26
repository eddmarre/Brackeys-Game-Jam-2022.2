using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WireAttacher : MonoBehaviour
{
	[SerializeField] private Vector2 relativeCarryPosition;
	
	private bool carrying = false;
	private FakeWire wire = null;
	private Device device = null;
	private Transform wireTransform;

	//private Transform transformToCarry;

	private void Start()
	{
		//Physics2D.IgnoreLayerCollision(3, 7);
		lastPos = transform.position;
	}

	bool pressed = false;
	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.gameObject.layer == 7 && wire == null && !carrying)
		{
			wire = collision.transform.parent.GetComponent<FakeWire>();
			wire.carrier = transform;
			wire.localCarrierOffset = relativeCarryPosition;
			wireTransform = collision.transform;
		}
		if(collision.gameObject.layer == 8)
		{
			device = collision.GetComponent<Device>();
		}
	}
	
	private void OnTriggerExit2D(Collider2D collision)
	{
		if (collision.gameObject.layer == 7 && wire != null && !carrying)
		{
			wire.carrier = null;
			wireTransform = null;
			wire = null;
		}
		if (collision.gameObject.layer == 8)
		{
			device = null;
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawSphere((Vector2)transform.position + relativeCarryPosition, 0.2f);
	}

	Vector3 lastPos;
	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Q) && wire != null)
		{
			if (carrying) wire.SetNewState(PlugState.Free, wireTransform);
			else wire.SetNewState(PlugState.Carry, wireTransform);
			
			carrying = !carrying;
		}

		if (Input.GetKeyDown(KeyCode.E) && wire != null)
		{
			if (carrying)
			{
				carrying = false;
				//transformToCarry = null;
				//wire.startState = PlugState.Free;
				//wire.hookIsPicked = false;

				if (device != null)
				{
					wireTransform.position = device.transform.position + device.wireAnchorPoint;
					wire.SetNewState(PlugState.Attach, wireTransform, device);
				}
				else wire.SetNewState(PlugState.Attach, wireTransform);
			}
			else
			{
				carrying = true;
				wire.SetNewState(PlugState.Carry, wireTransform);
			}
		}

		lastPos = transform.position;
	}
}
