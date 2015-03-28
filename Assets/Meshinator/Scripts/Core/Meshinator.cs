/*
 * Meshinator
 * Copyright Mike Mahoney 2013
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(Rigidbody))]
public class Meshinator : MonoBehaviour
{
	#region Fields & Properties
	
	public enum CacheOptions
	{
		None = 0,
		CacheAfterCollision = 1,
		CacheOnLoad = 2,
	};
	
	public enum ImpactShapes
	{
		FlatImpact = 0,
		SphericalImpact = 1,
	};
	
	public enum ImpactTypes
	{
		Compression = 0,
		Fracture = 1,
	};
	
	// This is how many FixedUpdates we require before we will do anything in the OnCollisionEnter function.
	// If this many FixedUpdate calls have occurred and we have not collided with anything, then the 
	// OnCollisionEnter function will the work normally. This is meant to prevent SubHulls creating new
	// GameObjects with colliders that overlap from the start and cause bad behavior.
	private const int c_FixedUpdateCountToIgnoreCollisions = 3;
	
	// This determines when we load up the Hull object.
	// - "None" means that nothing is loaded until a collision occurs; this has no extra memory or CPU
	// cost except when a collision is occuring, but the collision will take longer to compute.
	// - "CacheAfterCollision" loads a copy of the mesh during the first collision and maintains that
	// information from that point forward. This will mean a heavier memory footprint for each object
	// that has already had at least one collsion, but each collsion after the first will compute faster.
	// - "CacheOnLoad" loads a copy of the mesh on Start, which means a heavier footprint from the
	// beginning, but every deformation computation will be faster than they would without caching.
	public CacheOptions m_CacheOption = CacheOptions.CacheAfterCollision;
	
	// This determines what kind of deformation calculations will occur on a collision.
	// - "FlatImpact" treats the deformation as if the collision ocurred against a flat plane. This is
	// generally fine for collisions between objects of roughly the same size.
	// - "Spherical Impact" treats the deformation is if the collision occured against a sphere. This is
	// good when objects colliding against this one are expected to be much smaller than this object.
	public ImpactShapes m_ImpactShape = ImpactShapes.FlatImpact;
	
	// This determines what the result of the deformation calculations will be after a collision.
	// - "Compression" compresses the mesh on impact, but does not create any extra debris
	// - "Fracture" shatters a mesh into seperate meshes on seperate game objects
	public ImpactTypes m_ImpactType = ImpactTypes.Compression;

	// Subtracted from the force of an impact before any deformation is done. If the force becomes negative
	// due to this, no deformation will occur (the material resisted the impact).
	public float m_ForceResistance = 10f;
	
	// This is the maximum force that can affect a mesh on any given impact. Any force beyond this amount is
	// negated. If m_MaxForcePerImpact is less than or equal to m_ForceResistance, then no impact will ever
	// deform this GameObject's mesh.
	public float m_MaxForcePerImpact = 12f;
	
	// Multiplied by the force of an impact to determine the depth of an impact/explosion/etc. Higher
	// values indicate less dense materials (and thus more deformation), while smaller values indicate
	// more dense materials (and thus less deformation).
	public float m_ForceMultiplier = 0.25f;
	
	// Is an impact currently being calculated? If so, we'll end up ignoring other Impact calls to prevent
	// concurrent modifications to the mesh.
	private bool m_Calculating = false;
	
	// This is the cached Hull object that we may or may not use based on the set CacheOptions above.
	private Hull m_Hull = null;
	
	// These are the bounding boxes used to guide the mesh deformation so that it doesn't contract or expand
	// an unreasonable size. These are established either on Start (if CacheOptions.None), or on the first
	// collsion (if CacheOptions.CacheOnLoad or CacheOptions.CacheAfterCollision).
	private bool m_BoundsSet = false;
	private Bounds m_InitialBounds;
	
	// Collision-enabling info
	private bool m_ClearedForCollisions = true;
	private int m_CollisionCount = 0;
	private int m_FixedUpdatesSinceLastCollision = 0;

	// global variables to be used in gizmo debugging
	//TODO: DELETE THIS WHEN NO LONGER DEBUGGING
	private bool impactOccurred = false;
	private Vector3 debugImpactPoint;
	private Vector3 debugImpactForce;
	private List<Vector3> debugFractureVertices;

	public float maxFractures = 5.0f;

	public bool debug = false;
	
	#endregion Fields & Properties
	
	#region Unity Functions
	
	public void Start()
	{
		if (m_CacheOption == CacheOptions.CacheOnLoad)
		{
			// Make sure we have a MeshFilter
			MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
			if (meshFilter == null || meshFilter.sharedMesh == null)
				return;
			
			// Get the initial bounding box for this mesh
			m_InitialBounds = meshFilter.sharedMesh.bounds;
			m_BoundsSet = true;
			
			// Generate a hull to work with
			m_Hull = new Hull(meshFilter.sharedMesh);
		}
	}
	
	public void FixedUpdate()
	{
		// Keep track of how many FixedUpdates occur while m_ClearedForCollisions is false.
		// If enough FixedUpdate calls have occurred and we have not collided with anything, then the 
		// OnCollisionEnter function will the work normally. This is meant to prevent SubHulls creating new
		// GameObjects with colliders that overlap from the start and cause bad behavior.
		if (m_ClearedForCollisions == false)
		{
			if (m_CollisionCount != 0)
				m_FixedUpdatesSinceLastCollision = 0;
			else
				m_FixedUpdatesSinceLastCollision++;
			
			if (m_FixedUpdatesSinceLastCollision > c_FixedUpdateCountToIgnoreCollisions)
				m_ClearedForCollisions = true;
		}
	}
	
	#endregion Unity Functions
	
	#region Collision Callback Functions
	
	public void OnCollisionEnter(Collision collision)
	{
		m_CollisionCount++;

		if (m_ClearedForCollisions && collision.impactForceSum.magnitude >= m_ForceResistance)
		{
			// Find the impact point
			foreach (ContactPoint contact in collision.contacts)
			{
				if (contact.otherCollider == collision.collider)
				{
					Impact(contact.point, collision.transform.forward, collision.impactForceSum, m_ImpactShape, m_ImpactType);
					break;
				}
			}
		}
	}
	
	public void OnCollisionExit()
	{
		m_CollisionCount--;
	}
	
	#endregion Collision Callback Functions
	
	#region Impact Functions
	
	public void DelayCollisions()
	{
		m_ClearedForCollisions = false;
	}

	public void OnDrawGizmos() {
		if (impactOccurred && debug) {
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(debugImpactPoint, debugImpactForce.magnitude);
			Gizmos.color = Color.green;
			Gizmos.DrawWireCube(gameObject.transform.position, new Vector3(3,3,3));
			foreach (Vector3 vertex in debugFractureVertices){
				Gizmos.color = Color.blue;
				Gizmos.DrawLine(debugImpactPoint, vertex);
				Gizmos.color = Color.red;
				Gizmos.DrawWireSphere(vertex, 0.1f);
			}
			Debug.Break();
		}
	}

	public void Impact(Vector3 point, Vector3 impactDirection, Vector3 force, ImpactShapes impactShape, ImpactTypes impactType)
	{
		// See if we can do this right now
		if (!CanDoImpact(point, force))
			return;
		
		// We're now set on course to calculate the impact deformation
		m_Calculating = true;

		// Set up m_Hull
		InitializeHull ();

		// Figure out the true impact force
		if (force.magnitude > m_MaxForcePerImpact)
			force = force.normalized * m_MaxForcePerImpact;
		float impactFactor = (force.magnitude - m_ForceResistance) * m_ForceMultiplier;
		if (impactFactor <= 0)
			return;

		// set impactpoint and impactforce to be used for debugging
		debugImpactPoint = point;

		// Localize the point and the force to account for transform scaling (and maybe rotation or translation)
		Vector3 impactPoint = transform.InverseTransformPoint(point);
		Vector3 impactForce = transform.InverseTransformDirection(force.normalized) * impactFactor;
		
		// Limit the force by the extents of the initial bounds to keep things reasonable
		float impactForceX = Mathf.Max(Mathf.Min(impactForce.x, m_InitialBounds.extents.x), -m_InitialBounds.extents.x);
		float impactForceY = Mathf.Max(Mathf.Min(impactForce.y, m_InitialBounds.extents.y), -m_InitialBounds.extents.y);
		float impactForceZ = Mathf.Max(Mathf.Min(impactForce.z, m_InitialBounds.extents.z), -m_InitialBounds.extents.z);
		impactForce = new Vector3(impactForceX, impactForceY, impactForceZ);

		//DEBUG
		if (debug) {
			debugImpactPoint = point;
			debugImpactForce = impactForce;
			// make sure the debug vertices are the same as the actual fractures
			Random.seed = 1;
			debugFractureVertices = createFractureVertices(impactDirection, impactForce, debugImpactPoint);
			Random.seed = 1;
			impactOccurred = true;
		}

		List<Vector3> fractureVertices = createFractureVertices(impactDirection, impactForce, impactPoint);

		// Run the mesh deformation on another thread
		ThreadManager.RunAsync(()=>
		{
			// Do all the math to deform this mesh
			m_Hull.Impact(impactPoint, impactForce, impactShape, impactType, fractureVertices);
			
			// Queue the final mesh setting on the main thread once the deformations are done
			ThreadManager.QueueOnMainThread(()=>
			{
				// Clear out the current Mesh and MeshCollider (if we have one) for now
				MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
				if (meshFilter != null)
					meshFilter.sharedMesh = null;
				MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
				if (meshCollider != null)
					meshCollider.sharedMesh = null;
				
				// Get the newly-adjusted Mesh so we can work with it
				Mesh newMesh = m_Hull.GetMesh(impactPoint, impactForce.magnitude, fractureVertices);

				// Add inner meshes
				foreach (Mesh innerMesh in m_Hull.GetInnerMeshes()) {
					// Create the new GameObject
					GameObject newGO = (GameObject)GameObject.Instantiate(gameObject);
					
					// Set the new Mesh onto the MeshFilter and MeshCollider
					MeshFilter newMeshFilter = newGO.GetComponent<MeshFilter>();
					MeshCollider newMeshCollider = newGO.GetComponent<MeshCollider>();
					if (newMeshFilter != null)
						newMeshFilter.sharedMesh = innerMesh;
					if (newMeshCollider != null)
						newMeshCollider.sharedMesh = innerMesh;

					// apply force to rigidbodies
//					newGO.rigidbody.AddForce(impactPoint * impactForce.magnitude);
				}

				// Set the hull's new mesh back onto this game object
				if (meshFilter != null)
					meshFilter.sharedMesh = newMesh;
				
				// If this GameObject has a MeshCollider, put the new mesh there too
				if (meshCollider != null)
					meshCollider.sharedMesh = newMesh;
				
				// Drop our cached Hull if we're not supposed to keep it around
				if (m_CacheOption == CacheOptions.None)
					m_Hull = null;
				
				// Our calculations are done
				m_Calculating = false;
			});
		});
	}

	/**
	 * This function will randomly rotate around the impact vector to create fracture lines, then select a point on each fracture line
	 * to represent a new vertex in the fractured mesh.
	 **/
	private List<Vector3> createFractureVertices(Vector3 impactDirection, Vector3 impactForce, Vector3 impactPoint) {
		List<Vector3> fractureVertices = new List<Vector3>();
		int numFractures = (int)Random.Range(1.0f, maxFractures);
		for (int i=0; i < numFractures; i++) {
			int randomAngle = (int)Random.Range(0.0f, 75.0f);
			Vector3 rotatedVector = Quaternion.AngleAxis(randomAngle, impactDirection) * impactForce;
			Ray fractureRay = new Ray(impactPoint, rotatedVector);
			float randomPoint = Random.Range(0.05f, impactForce.magnitude);
			Vector3 fractureVertex = fractureRay.GetPoint(randomPoint);
			// determine if random point is inside game object
			//TODO: This doesnt seem to work in all cases
			Collider collider = gameObject.GetComponent<Collider>();
			// localize the object center since the impact point has been localized
			Vector3 objectCenter = transform.InverseTransformPoint(collider.bounds.center);
			Vector3 direction = objectCenter - fractureVertex;
			Ray checkRay = new Ray(fractureVertex, direction);
			RaycastHit hitInfo;
			bool hit = collider.Raycast(checkRay, out hitInfo, direction.magnitude);
			// If we hit the collider, point is outside. So check for !hit
			if(!hit) {
				fractureVertices.Add(fractureVertex);
			}
			else {
				i--;
			}
		}
		return fractureVertices;
	}
	
	private void InitializeHull()
	{
		// See if we already have a hull
		if (m_Hull == null)
		{
			// Make sure we have a MeshFilter
			MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
			if (meshFilter == null || meshFilter.sharedMesh == null)
			{
				m_Calculating = false;
				return;
			}
			
			// Get the initial bounding box for this mesh if needed
			if (m_BoundsSet == false)
			{
				m_InitialBounds = meshFilter.sharedMesh.bounds;
				m_BoundsSet = true;
			}
			
			// Generate a hull to work with
			m_Hull = new Hull(meshFilter.sharedMesh);
		}
	}
	
	private bool CanDoImpact(Vector3 point, Vector3 force)
	{
		//If we are already handling a call to FlatImpact, we'll end up ignoring other Impact calls to
		// prevent concurrent modifications to the mesh.
		if (m_Calculating)
			return false;

		// Make sure this force can affect this object
		float forceMagnitude = force.magnitude;
		if (forceMagnitude - m_ForceResistance <= 0)
			return false;
		
		// Figure out the true impact force, and make sure it's greater than zero
		float impactFactor = (forceMagnitude - m_ForceResistance) * m_ForceMultiplier;
		if (impactFactor <= 0)
			return false;
		
		return true;
	}
	
	#endregion Impact Functions
}
