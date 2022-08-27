using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum PlugState { Carry, Attach, Free }

[RequireComponent(typeof(LineRenderer))]
public class FakeWire : MonoBehaviour
{
    [SerializeField] private int segmentCount;
	[SerializeField] private Vector2 segmentSize;
	[SerializeField] private float gravity = 10f;
	[SerializeField] private Sprite startSprite, endSprite, wireSprite;
	[SerializeField] private Color wireColor;
	[SerializeField] private Vector3 spriteScale = Vector3.one;

	[SerializeField] private Vector3 startPosition, endPosition;
	[SerializeField] private PlugState ctrlStartState = PlugState.Attach, ctrlEndState = PlugState.Attach;
	[SerializeField] private bool metrics;

    private List<WireNode> nodes;
	private Transform startTransform, endTransform;
	private Transform CarryTransform
	{
		get {
			if (ctrlStartState == PlugState.Carry) { return startTransform; }
			else if (ctrlEndState == PlugState.Carry) { return endTransform; }
			else { return null; }
		}
	}
	private Transform CarriedTransform
	{
		get
		{
			if (CarryTransform == null) return null;
			return (CarryTransform == startTransform) ? endTransform : startTransform;
		}
	}
	[HideInInspector] public Transform carrier;
	[HideInInspector] public Vector2 localCarrierOffset;
	[HideInInspector] public Device startDevice = null, endDevice = null;
	//[HideInInspector] public PlugState startState = PlugState.Attach, endState = PlugState.Attach;
	
	private LineRenderer lineRenderer;
	private CharacterController2D controller;

	private class WireNode
	{
		public float elapsedTime;
		public Vector2 originalPosition, currentPosition, finalPosition;
		public bool grounded, frozen;
		public int suspended;

		public WireNode(Vector2 _originalPosition, Vector2 segmentSize)
		{
			elapsedTime = 0f;
			originalPosition = _originalPosition;
			currentPosition = originalPosition;
			grounded = false;
			suspended = 0;

			RaycastHit2D[] hits = Physics2D.RaycastAll(originalPosition, Vector2.down);
			Array.Sort(hits, (a, b) => (a.distance < b.distance) ? -1 : 1);

			finalPosition = originalPosition;
			foreach(RaycastHit2D h in hits)
			{
				if(h.collider.gameObject.layer != 3 && h.collider.gameObject.layer != 7)
				{
					//Vector2 drop = Vector2.down * Mathf.Min(h.distance - segmentSize.x / 2f, segmentSize.y * 0.96f);
					Vector2 drop = Vector2.down * (h.distance - segmentSize.x / 2f);
					finalPosition = originalPosition + drop;
					break;
				}
			}
			//print(hit.distance);
			//finalPosition = originalPosition + Vector2.down * (hit.distance - wireThickness / 2f);
		}

		public void UpdatePosition(float gravity)
		{
			if (grounded) return;
			currentPosition = originalPosition + gravity * Vector2.down * elapsedTime * elapsedTime;
			if (currentPosition.y < finalPosition.y)
			{
				grounded = true;
				currentPosition = finalPosition;
			}
		}

		public void Tick(float deltaTime, float gravity)
		{
			if (frozen) return;
			elapsedTime += deltaTime;
			UpdatePosition(gravity);
		}
	}

	#region Plug Methods

	private bool Both(PlugState state) => ctrlStartState == state && ctrlEndState == state;
	private bool Both(PlugState start, PlugState end) => ctrlStartState == start && ctrlEndState == end;
	private bool Any(PlugState state) => ctrlStartState == state && ctrlEndState == state;
	private bool Any(PlugState start, PlugState end) => ctrlStartState == start && ctrlEndState == end;

	#endregion

	private void Start()
	{
		controller = FindObjectOfType<CharacterController2D>();
		
		lineRenderer = GetComponent<LineRenderer>();
		lineRenderer.startWidth = segmentSize.x;
		lineRenderer.endWidth = segmentSize.x;

		CreatePlug("Start", startPosition, startSprite, out startTransform);
		CreatePlug("End", endPosition, endSprite, out endTransform);
		
		lineRenderer.material.SetFloat("_HasWireTex", (wireSprite == null) ? 0f : 1f);
		if(wireSprite) lineRenderer.material.SetTexture("_WireTex", wireSprite.texture);
		else lineRenderer.material.SetColor("_WireColor", wireColor);

		nodes = new List<WireNode>();
		for (int i = 0; i < segmentCount; i++)
		{
			float step = i / (float)segmentCount;
			//Vector3 pointA = Vector3.Lerp(endPosition, bezierPosition, step);
			//Vector3 pointB = Vector3.Lerp(bezierPosition, startPosition, step);
			//nodes.Add(new WireNode(Vector3.Lerp(pointA, pointB, step), segmentSize));
			nodes.Add(new WireNode(Vector3.Lerp(endPosition, startPosition, step), segmentSize));
		}
		nodes.Add(new WireNode(startTransform.position, segmentSize));
		//DrawLine(nodes.ConvertAll<Vector3>());

		//attachBezierList = new List<Vector3>();
		stateBinds = new Dictionary<Transform, Func<PlugState>>();

		stateBinds.Add(startTransform, () => ctrlStartState);
		stateBinds.Add(endTransform, () => ctrlEndState);
	}

	//List<Vector3> attachBezierList;
	private void FixedUpdate()
	{
		//if (Both(PlugState.Attach))
		//{
		//	if (attachBezierList.Count == 0)
		//	{
		//		GenerateAttachBezier();
		//		nodes = new List<WireNode>(attachBezierList.ConvertAll<WireNode>(vect => new WireNode(vect, segmentSize)));
		//		DrawLine(attachBezierList);
		//	}
		//	return;
		//}
		//else attachBezierList = new List<Vector3>();
		
		nodes.ForEach(n => n.Tick(Time.deltaTime, gravity));

		for (int i = 1; i < nodes.Count; i++)
		{
			WireNode last = nodes[i - 1];
			WireNode current = nodes[i];

			Vector2 midpoint = Vector2.Lerp(current.finalPosition, last.finalPosition, 0.5f);
			foreach (Collider2D col in Physics2D.OverlapPointAll(midpoint))
			{
				if (col.gameObject.layer != 3)
				{
					WireNode lowest = (current.finalPosition.y < last.finalPosition.y) ? current : last;
					WireNode highest = (lowest == current) ? last : current;

					if(highest.suspended == 0)
					{
						lowest.finalPosition = new Vector2(lowest.finalPosition.x, highest.finalPosition.y);
						if (lowest.currentPosition.y < lowest.finalPosition.y) lowest.currentPosition = lowest.finalPosition;
						lowest.suspended++;
					}
					break;
				}
			}
		}

		if (CarryTransform)
		{
			CarryTransform.position = (Vector2)carrier.position + localCarrierOffset;

			//Check if we're carrying the wire from the other end
			//PlugState carriedState = (CarryTransform == startTransform) ? ctrlEndState : ctrlStartState;
			PlugState carriedState = stateBinds[CarriedTransform].Invoke();
			List<Vector3> positions = new List<Vector3>();

			float distance = Vector2.Distance(CarryTransform.position, nodes[^1].currentPosition);
			if (distance >= segmentSize.y)
			{
				if (carriedState == PlugState.Attach) nodes.Add(new WireNode(CarryTransform.position, segmentSize));
				if (nodes.Count >= segmentCount + 1)
					{
						if (carriedState == PlugState.Free) nodes.RemoveAt(0);
						else
						{
							WireNode farthest = nodes[^2];
							Vector2 attachPos = nodes[0].currentPosition;
							Vector2 plugPos = nodes[^1].currentPosition;
							nodes.ForEach(n =>
							{
								if (n != nodes[0] && n != nodes[^1])
								{
									Vector2 nodePos = n.currentPosition;
									if((nodePos - plugPos).sqrMagnitude > (plugPos - farthest.currentPosition).sqrMagnitude)
									{
										farthest = n;
									}
								}
							});
							nodes.Remove(farthest);
							//nodes.RemoveAt(nodes.Count - 2);
						}
					}
				if (carriedState == PlugState.Free) nodes.Add(new WireNode(CarryTransform.position, segmentSize));
			}

			if(carriedState == PlugState.Attach)
			{
				for(int i = 1; i < nodes.Count - 1; i++)
				{
					float horizontal = Mathf.Abs(nodes[i - 1].currentPosition.x - nodes[i].currentPosition.x);
					if(horizontal > segmentSize.y)
					{
						nodes[i].currentPosition = Vector2.Lerp(nodes[i - 1].currentPosition, nodes[i].currentPosition, segmentSize.y / horizontal);
					}
				}

				//THIS DOESN'T WORK but you can tweak it with your controller
				//meant to stop the player from moving when pulling on the wire
				float totalDist = nodes[0].currentPosition.x - nodes[^1].currentPosition.x;
				if (Mathf.Abs(totalDist) > (segmentCount * 1.05f) * segmentSize.y)
				{
					if (totalDist < 0f) controller.canMoveRight = false;
					else controller.canMoveLeft = false;
				}
				else
				{
					controller.canMoveRight = true;
					controller.canMoveLeft = true;
				}
			}
			
			positions = nodes.ConvertAll<Vector3>(wn => wn.currentPosition);
			if (nodes.Count == segmentCount + 1 && distance < segmentSize.y && carriedState == PlugState.Free)
			{
				positions[0] = Vector3.Lerp(positions[0], positions[1], distance / segmentSize.y);
			}
			
			positions.Add(CarryTransform.position);
			DrawLine(positions, (carriedState == PlugState.Free) ? 0.8f : 0.5f);
		}
		else
		{
			DrawLine(nodes.ConvertAll<Vector3>(wn => wn.currentPosition));
		}
		
		//lastStartPos = startTransform.position; lastEndPos = endTransform.position;
		if (metrics)
		{
			float plugDistance = Vector2.Distance(nodes[0].currentPosition, nodes[^1].currentPosition);
			float plugHorizontalDistance = Mathf.Abs(nodes[0].currentPosition.x - nodes[^1].currentPosition.x);
			float length = 0f;
			float horizontalLength = 0f;
			for (int i = 1; i < nodes.Count; i++)
			{
				Vector2 last = nodes[i - 1].currentPosition;
				Vector2 current = nodes[i].currentPosition;
				length += Vector2.Distance(last, current);
				horizontalLength += Mathf.Abs(last.x - current.x);
			}
			print($"Wire length: {(segmentCount * 1.05f) * segmentSize.y} - Actual length: {length} - Horizontal length: {horizontalLength} - Plug distance: {plugDistance} - Plug horizontal distance: {plugHorizontalDistance}");
		}
	}

	private void DrawLine(List<Vector3> positions, float bezierAmount = 0.5f)
	{
		//Move sprites
		if (CarryTransform)
		{
			if (positions.Count >= 2)
			{
				Vector2 dir = positions[^1] - positions[^2];
				CarryTransform.rotation = Quaternion.Euler(Vector3.forward * (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f));
				CarriedTransform.position = positions[0];
				dir = positions[0] - positions[1];
				CarriedTransform.rotation = Quaternion.Euler(Vector3.forward * (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f));
			}
		}
		else
		{
			if (positions.Count >= 2)
			{
				int startIndex = GetTransformTiedIndex(startTransform);
				int endIndex = GetTransformTiedIndex(endTransform);

				if (ctrlStartState == PlugState.Free) startTransform.position = positions[startIndex];
				else if (ctrlStartState == PlugState.Attach) nodes[startIndex].finalPosition = startTransform.position;
				Vector2 dir = positions[startIndex] - positions[(startIndex == 0) ? 1 : startIndex - 1];
				startTransform.rotation = Quaternion.Euler(Vector3.forward * (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f));

				//endTransform.position = positions[endIndex];
				if (ctrlEndState == PlugState.Free) endTransform.position = positions[endIndex];
				else if (ctrlEndState == PlugState.Attach) nodes[endIndex].finalPosition = endTransform.position;
				dir = positions[endIndex] - positions[(endIndex == 0) ? 1 : endIndex - 1];
				endTransform.rotation = Quaternion.Euler(Vector3.forward * (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f));
			}
		}
		//Bezier
		List<Vector3> beziers = new List<Vector3>();
		for (int i = 1; i < positions.Count - 1; i++)
		{
			Vector3 pointA = Vector3.Lerp(positions[i - 1], positions[i], bezierAmount);
			Vector3 pointB = Vector3.Lerp(positions[i + 1], positions[i], bezierAmount);
			Vector3 bezier = Vector3.Lerp(pointA, pointB, 0.5f);
			beziers.Add(bezier);
		}
		for (int i = 1; i < positions.Count - 1; i++)
		{
			positions[i] = beziers[i - 1];
		}

		lineRenderer.positionCount = positions.Count;
		lineRenderer.SetPositions(positions.ToArray());
	}

	public void SetNewState(PlugState newState, Transform transformHit, Device deviceHit = null)
	{
		if (startTransform == transformHit)
		{
			//We're dealing with the start state
			if (deviceHit)
			{
				startDevice = deviceHit;
				startDevice.connectedWire = this;
				startDevice.wireConnectAction.Invoke(this);
				if (endDevice)
				{
					startDevice.deviceConnectAction(endDevice);
					endDevice.deviceConnectAction(startDevice);
				}
			}
			if (newState != PlugState.Attach && startDevice)
			{
				if (endDevice)
				{
					startDevice.deviceDisconnectAction(endDevice);
					endDevice.deviceDisconnectAction(startDevice);
				}
				startDevice.connectedWire = null;
				startDevice.wireDisconnectAction.Invoke(this);
				startDevice = null;
			}

			ctrlStartState = newState;
		}
		else if (endTransform == transformHit)
		{
			//We're dealing with the end state
			if (deviceHit)
			{
				endDevice = deviceHit;
				endDevice.connectedWire = this;
				endDevice.wireConnectAction.Invoke(this);
				if (startDevice)
				{
					startDevice.deviceConnectAction(endDevice);
					endDevice.deviceConnectAction(startDevice);
				}
			}
			if (newState != PlugState.Attach && endDevice)
			{
				if (startDevice)
				{
					startDevice.deviceDisconnectAction(endDevice);
					endDevice.deviceDisconnectAction(startDevice);
				}
				endDevice.connectedWire = null;
				endDevice.wireDisconnectAction.Invoke(this);
				endDevice = null;
			}

			ctrlEndState = newState;
		}

		if (CarryTransform && GetTransformTiedIndex(CarryTransform) == 0 &&
			Vector2.Distance(nodes[^1].currentPosition, nodes[0].currentPosition) > segmentSize.y)
		{
			nodes.Reverse();
		}
	}

	private void CreatePlug(string name, Vector3 pos, Sprite sprite, out Transform dumpTransform)
	{
		GameObject obj = new GameObject(name);
		obj.layer = 7;
		obj.transform.SetParent(transform);
		dumpTransform = obj.transform;
		dumpTransform.localScale = spriteScale;
		dumpTransform.position = pos;
		obj.AddComponent<SpriteRenderer>().sprite = sprite;
		//BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
		//col.size = startSprite.bounds.size;
		BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
		col.size = sprite.bounds.size * 3;
		col.isTrigger = true;
		//obj.AddComponent<Rigidbody2D>();
	}

	//private void GenerateAttachBezier()
	//{
	//	Vector3 startPos = startTransform.position;
	//	Vector3 endPos = endTransform.position;
		
	//	Vector2 perp = Vector2.Perpendicular(endPos - startPos);
	//	if (perp.y > 0f) perp = -perp;
	//	Vector3 bezierPosition = (Vector2)Vector3.Lerp(startPos, endPos, 0.5f) + perp * 0.5f;

	//	attachBezierList = new List<Vector3>();
	//	for (int i = 0; i < segmentCount; i++)
	//	{
	//		float step = i / (float)segmentCount;
	//		Vector3 pointA = Vector3.Lerp(endPos, bezierPosition, step);
	//		Vector3 pointB = Vector3.Lerp(bezierPosition, startPos, step);
	//		attachBezierList.Add(Vector3.Lerp(pointA, pointB, step));
	//	}
	//	attachBezierList.Add(startPos);
	//}

	private Dictionary<Transform, Func<PlugState>> stateBinds;
	private WireNode GetTransformTiedWireNode(Transform t) => nodes[GetTransformTiedIndex(t)];
	private int GetTransformTiedIndex(Transform t)
	{
		if ((nodes[^1].currentPosition - (Vector2)t.position).sqrMagnitude >
				(nodes[0].currentPosition - (Vector2)t.position).sqrMagnitude)
		{
			return 0;
		}
		else return nodes.Count - 1;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawSphere(startPosition, 0.2f);
		Gizmos.color = Color.red;
		Gizmos.DrawSphere(endPosition, 0.2f);
	}
}