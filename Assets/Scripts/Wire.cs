using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum WireType { Energy, Data }

[RequireComponent(typeof(LineRenderer))]
public class Wire : MonoBehaviour
{
	[SerializeField] private int segmentCount;
	[SerializeField] private Vector2 segmentSize, plugSize;
	[SerializeField] private PhysicsMaterial2D physicMaterial;
	[HideInInspector] public List<Rigidbody2D> rigids;
	[HideInInspector] public Rigidbody2D hookRigid;
	[HideInInspector] public bool hookIsPicked = false;
	[HideInInspector] public float stretching = 1f;
	public float farAwayMultiplier, pullForce, clipForce;
	private LineRenderer lineRenderer;

	public struct Plug
	{
		public bool anchored;
		public Converter<Vector3, Vector3> positionUpdater;

		public Plug(Converter<Vector3, Vector3> _positionUpdater)
		{
			anchored = false;
			positionUpdater = _positionUpdater;
		}
	}

	[SerializeField] private Sprite plugSprite;

	[Space]
	[SerializeField] private WireType wireType;

	void Start()
	{
		rigids = new List<Rigidbody2D>();

		lineRenderer = GetComponent<LineRenderer>();
		lineRenderer.positionCount = segmentCount + 1;
		lineRenderer.startWidth = segmentSize.x;
		lineRenderer.endWidth = segmentSize.x;

		Physics2D.IgnoreLayerCollision(6, 6);

		//AddPlug(false);
		GameObject hook = new GameObject($"Hook");
		hook.transform.SetParent(transform);
		hook.layer = 6;
		HingeJoint2D hookJoint = hook.AddComponent<HingeJoint2D>();
		hookJoint.autoConfigureConnectedAnchor = true;
		hookJoint.useLimits = false;
		//DistanceJoint2D hookDist = hook.AddComponent<DistanceJoint2D>();
		//hookDist.autoConfigureDistance = false;
		//hookDist.distance = segmentSize.y;
		//hookDist.maxDistanceOnly = false;
		hookRigid = hook.GetComponent<Rigidbody2D>();
		

		for (int i = 0; i < segmentCount; i++)
		{
			GameObject go = new GameObject($"Chain {i}");
			go.transform.SetParent(transform);
			go.transform.position = new Vector3(0f, -segmentSize.y * i * 1.1f, 0f);
			go.layer = 6;

			CapsuleCollider2D capCol = go.AddComponent<CapsuleCollider2D>();
			//HingeJoint2D hinge = go.AddComponent<HingeJoint2D>();
			Rigidbody2D rigidbody = go.AddComponent<Rigidbody2D>();

			capCol.size = segmentSize;
			capCol.sharedMaterial = physicMaterial;

			//hinge.connectedBody = (i == 0) ? hook.GetComponent<Rigidbody2D>() : rigids[^1];
			//hinge.autoConfigureConnectedAnchor = i != 0;
			//hinge.useLimits = true;
			//JointAngleLimits2D newLimit = new JointAngleLimits2D();
			//newLimit.max = (i == segmentCount - 1) ? 20f : 60f;
			//newLimit.min = -newLimit.max;
			//hinge.limits = newLimit;

			DistanceJoint2D dist = go.AddComponent<DistanceJoint2D>();
			dist.autoConfigureDistance = false;
			dist.maxDistanceOnly = true;
			dist.distance = (i == 0) ? 0.1f : segmentSize.y;
			dist.connectedBody = (i == 0) ? hookRigid : rigids[^1];
			//if (i == 0) hookDist.connectedBody = rigidbody;
			//if (i != 0) rigids[^1].GetComponent<DistanceJoint2D>().connectedBody = rigidbody;
			
			rigids.Add(rigidbody);
			
		}
		//AddPlug(true);

		hook.transform.position = rigids[0].transform.position;
		rigids[^1].mass *= 10f;

		//Add plugs
		AddPlug(true);
		AddPlug(false);
	}

	private void FixedUpdate()
	{
		stretching = Vector3.Distance(hookRigid.transform.position, rigids[^1].transform.position) / (segmentCount * segmentSize.y * farAwayMultiplier);
		
		if (stretching > 1f)
		{
			//print("Too far away!");

			for (int i = rigids.Count - 1; i > 1; i--)
			{
				//rigids[i].AddForce((Vector2)(rigids[i - 1].transform.position - rigids[i].transform.position) * 10f);
				Vector2 pull = (Vector2)(rigids[i - 1].transform.position - rigids[i].transform.position);
				float angle = Mathf.Atan2(pull.y, pull.x);
				rigids[i].AddForce(pull * pullForce * Mathf.Pow(Mathf.Sin(angle), 2) * Time.deltaTime);
				//rigids[i].velocity += hookRigid.velocity * hookForce;

				foreach(Collider2D col in Physics2D.OverlapPointAll(rigids[i].transform.position))
				{
					if (col.gameObject.layer == 6 || col.gameObject.layer == 7) continue;

					for(int j = rigids.Count-1; j > i; j--)
					{
						rigids[j].AddForce(new Vector2(0f, pull.magnitude) * clipForce * Time.deltaTime);
					}
					Vector2 perp = Vector2.Perpendicular(pull);
					if (perp.y < 0f) perp = -perp;
					rigids[i].AddForce(perp * clipForce * Time.deltaTime);

					break;
					//print($"{i}: {col.gameObject.name}");
				}
			}
		}
		
		//rigids[0].transform.position = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10));
		//if(!Physics2D.CircleCast(hookRigid.transform.position, plugSize.y, Vector2.down, plugSize.y)) hookRigid.transform.position += Vector3.down * 2f * Time.deltaTime;
		//Vector3.Move
		//print(Physics2D.CircleCast(hookRigid.transform.position, plugSize.y, Vector2.down, plugSize.y, 6));
		List<RaycastHit2D> hits;
		if (!hookIsPicked)
		{
			hits = new List<RaycastHit2D>(Physics2D.CircleCastAll(hookRigid.transform.position, plugSize.x, Vector2.down, 0.2f));
			if(!hits.Exists(h => h.collider.gameObject.layer != 6 && h.collider.gameObject.layer != 7))
			{
				hookRigid.transform.position += Vector3.down * 2f * Time.deltaTime;
			}
		}

		hits = new List<RaycastHit2D>(Physics2D.CircleCastAll(rigids[^1].transform.position, plugSize.x, Vector2.down, 0.2f));
		if (!hits.Exists(h => h.collider.gameObject.layer != 6 && h.collider.gameObject.layer != 7))
		{
			rigids[^1].transform.position += Vector3.down * 2f * Time.deltaTime;
		}

		//if(!Physics2D.CircleCast(rigids[^1].transform.position, plugSize.y, Vector2.down, plugSize.y)) rigids[^1].transform.position += Vector3.down * 2f * Time.deltaTime;
		//rigids[^1].transform.position += Vector3.down * 2f * Time.deltaTime;

		List<Vector3> positions = rigids.ConvertAll<Vector3>(hj => GetSegmentEnd(hj.transform));
		//positions.Insert(0, rigids[0].transform.position);
		positions.Insert(0, GetSegmentStart(rigids[0].transform));

		List<Vector3> beziers = new List<Vector3>();
		for (int i = 1; i < positions.Count - 1; i++)
		{
			Vector3 pointA = Vector3.Lerp(positions[i - 1], positions[i], 0.5f);
			Vector3 pointB = Vector3.Lerp(positions[i], positions[i + 1], 0.5f);
			Vector3 bezier = Vector3.Lerp(pointA, pointB, 0.5f);
			beziers.Add(bezier);
		}
		for (int i = 1; i < positions.Count - 1; i++)
		{
			positions[i] = beziers[i - 1];
		}

		lineRenderer.SetPositions(positions.ToArray());
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		//Gizmos.
	}

	[ContextMenu("Flip")]
	public void FlipWire()
	{
		List<Transform> transforms = new List<Transform>() { hookRigid.transform };
		transforms.AddRange(rigids.ConvertAll<Transform>(rb => rb.gameObject.transform));

		List<Vector3> positions = new List<Vector3>();
		List<Vector3> rotations = new List<Vector3>();

		transforms.Reverse();
		transforms.ForEach(t =>
		{
			positions.Add(GetSegmentEnd(t));
			Vector3 rot = t.rotation.eulerAngles;
			rot.z += 180f;
			rotations.Add(rot);
		});
		transforms.Reverse();

		transforms.ForEach(t =>
		{
			t.SetPositionAndRotation(positions[0], Quaternion.Euler(rotations[0]));
			
			positions.RemoveAt(0);
			rotations.RemoveAt(0);
		});
	}

	private void AddPlug(bool start)
	{
		GameObject plugObj = new GameObject("Plug");
		plugObj.layer = 7;
		plugObj.transform.SetParent((start) ? rigids[0].transform : rigids[^1].transform);
		plugObj.transform.localPosition = (start) ? GetSegmentStart(plugObj.transform) : GetSegmentEnd(plugObj.transform);
		plugObj.AddComponent<SpriteRenderer>().sprite = plugSprite;
		CircleCollider2D cirCol = plugObj.AddComponent<CircleCollider2D>();
		cirCol.isTrigger = true;
		cirCol.radius = plugSize.y;
	}

	private Vector3 GetSegmentStart(Transform t)
	{
		return t.position + new Vector3(
			segmentSize.y * Mathf.Sin(Mathf.Deg2Rad * t.rotation.eulerAngles.z),
			segmentSize.y * -Mathf.Cos(Mathf.Deg2Rad * t.rotation.eulerAngles.z),
			0) / (-2f);
	}

	private Vector3 GetSegmentEnd(Transform t)
	{
		return t.position + new Vector3(
			segmentSize.y * Mathf.Sin(Mathf.Deg2Rad * t.rotation.eulerAngles.z),
			segmentSize.y * -Mathf.Cos(Mathf.Deg2Rad * t.rotation.eulerAngles.z),
			0) / 2f;
	}
}
