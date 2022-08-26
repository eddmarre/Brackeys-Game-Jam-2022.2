using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(SpriteRenderer))]
public class Device : MonoBehaviour
{
    public Vector3 wireAnchorPoint; //Relative position where the wire will be connected

	public FakeWire connectedWire = null;
	public Device connectedDevice = null;

	//The wire will be responsible for making all the connections
	//A DeviceManager behaviour or inheriting classes are ways to setup different types of devices
	public Action<FakeWire> wireConnectAction = wire => print($"Connected wire {wire.gameObject.name}");
	public Action<FakeWire> wireDisconnectAction = wire => print($"Disconnected wire {wire.gameObject.name}");
	public Action<Device> deviceConnectAction = device => print($"Connected device {device.gameObject.name}");
	public Action<Device> deviceDisconnectAction = device => print($"Disconnected device {device.gameObject.name}");

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.cyan;
		Gizmos.DrawSphere(transform.position + wireAnchorPoint, 0.2f);
	}
}
