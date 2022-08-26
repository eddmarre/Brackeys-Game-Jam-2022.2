using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BetterWire : MonoBehaviour
{
	[SerializeField] private int segmentCount;
	[SerializeField] private Vector2 segmentSize, plugSize;
	[SerializeField] private int plugGrab = 0;
	[SerializeField] private float intensity = 1f, gravity = 1f;

	private List<Segment> segments;
	private List<Plug> plugs;

	private LineRenderer lineRenderer;

	void Start()
	{
		lineRenderer = GetComponent<LineRenderer>();
		lineRenderer.positionCount = segmentCount + 2;
		lineRenderer.startWidth = segmentSize.x; lineRenderer.endWidth = segmentSize.x;

		segments = new List<Segment>();
		plugs = new List<Plug>();

		plugs.Add(new Plug(this));
		for (int i = 0; i < segmentCount; i++) segments.Add(new Segment(this));
		plugs.Add(new Plug(this));

		plugs[0].connectedSegment = segments[0];
		plugs[1].connectedSegment = segments[^1];
		for (int i = 0; i < segmentCount; i++)
		{
			if (i != 0) segments[i].previousSegment = segments[i - 1];
			if (i != segmentCount - 1) segments[i].nextSegment = segments[i + 1];
		}

		Physics2D.IgnoreLayerCollision(6, 6);
		Physics2D.IgnoreLayerCollision(6, 7);
		Physics2D.IgnoreLayerCollision(7, 7);

		lastPlugPosition = plugs[plugGrab].transform.position;
	}

	private class Segment
	{
		public Segment previousSegment, nextSegment;
		public Rigidbody2D rigidbody;
		public Transform transform;

		public Segment(BetterWire parentWire)
		{
			GameObject obj = new GameObject($"Segment {parentWire.segments.Count}");
			obj.layer = 6;
			transform = obj.transform;
			transform.SetParent(parentWire.transform);

			rigidbody = obj.AddComponent<Rigidbody2D>();
			rigidbody.gravityScale = 0f;
			obj.AddComponent<CapsuleCollider2D>().size = parentWire.segmentSize;

			previousSegment = nextSegment = null;
		}
	}

	private class Plug
	{
		public Segment connectedSegment;
		public Rigidbody2D rigidbody;
		public Transform transform;

		public Plug(BetterWire parentWire)
		{
			if (parentWire.plugs.Count >= 2) return;

			GameObject obj = new GameObject($"Plug {parentWire.plugs.Count}");
			obj.layer = 7;
			transform = obj.transform;
			transform.SetParent(parentWire.transform);

			rigidbody = obj.AddComponent<Rigidbody2D>();
			rigidbody.gravityScale = 0f;
			obj.AddComponent<CapsuleCollider2D>().size = parentWire.plugSize;

			connectedSegment = null;
		}
	}

	Vector3 lastPlugPosition;
	private void FixedUpdate()
	{
		if(Vector3.Distance(plugs[0].transform.position, plugs[1].transform.position) > plugSize.y + segmentSize.y * segmentCount)
		{
			plugs[plugGrab].transform.position = lastPlugPosition;
		}
		
		if (plugGrab == 0)
		{
			Segment seg = plugs[0].connectedSegment;
			Vector3 lastPos = plugs[0].transform.position;
			Vector3 offset = new Vector3(0f, (plugSize.y + segmentSize.y) / 2f, 0f);
			do
			{
				MoveInWire(lastPos + offset, seg.rigidbody, offset.y);

				lastPos = seg.transform.position;
				offset = new Vector3(0f, (seg.nextSegment == null) ? (plugSize.y + segmentSize.y) / 2f : segmentSize.y, 0f);
				seg = seg.nextSegment;
			}
			while (seg != null);
			MoveInWire(lastPos + offset, plugs[1].rigidbody, offset.y);
			//plugs[1].rigidbody.AddForce((segments[^1].transform.position - plugs[1].transform.position) * Time.deltaTime * intensity);
		}
		else if (plugGrab == 1)
		{
			Segment seg = plugs[1].connectedSegment;
			Vector3 lastPos = plugs[1].transform.position;
			do
			{
				seg.rigidbody.AddForce((lastPos - seg.transform.position) * Time.deltaTime * intensity);
				lastPos = seg.transform.position;
				seg = seg.previousSegment;
			}
			while (seg != null);
			plugs[0].rigidbody.AddForce((segments[0].transform.position - plugs[0].transform.position) * Time.deltaTime * intensity);
		}

		List<Vector3> positions = new List<Vector3>() { plugs[0].transform.position };
		positions.AddRange(segments.ConvertAll<Vector3>(seg => seg.transform.position));
		positions.Add(plugs[1].transform.position);
		lineRenderer.SetPositions(positions.ToArray());

		print($"Distance: {Vector3.Distance(plugs[0].transform.position, plugs[1].transform.position)} - Expected: {plugSize.y + segmentSize.y * segmentCount}");
		lastPlugPosition = plugs[plugGrab].transform.position;
	}

	void MoveInWire(Vector3 nextPosition, Rigidbody2D currentRigid, float maxDistance)
	{
		Vector2 dir = nextPosition - currentRigid.transform.position;
		Vector2 toAdd = dir * dir.magnitude * intensity;
		toAdd += Vector2.down * gravity;
		//Vector2 toAdd = (dir.magnitude > maxDistance) ? dir.normalized * intensity : Vector2.zero;
		//currentRigid.rotation
		//currentRigid.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg));
		currentRigid.SetRotation(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f);
		currentRigid.velocity = toAdd * Time.deltaTime;
	}

	private void OnDrawGizmosSelected()
	{
		//Gizmos.color = Color.red;
		//Gizmos.Draw
	}
}
