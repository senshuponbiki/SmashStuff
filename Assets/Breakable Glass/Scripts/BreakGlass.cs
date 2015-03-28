using UnityEngine;
using System.Collections.Generic;

public class BreakGlass : MonoBehaviour {
	public List<GameObject> BrokenGlassGO; // The broken glass GameObject
	GameObject BrokenGlassInstance; 
	public bool BreakSound=true;
	public GameObject SoundEmitter; //An object that will emit sound
	public float SoundEmitterLifetime=2.0f;
	public float ShardsLifetime=3.0f; //Lifetime of shards in seconds (0 if you don't want shards to disappear)
	public float ShardMass=0.5f; //Mass of each shard
	public Material ShardMaterial;
	
	public bool BreakByVelocity=true;
	public float BreakVelocity=2.0f;
	
	public bool BreakByImpulse=false;
	public float BreakImpulse=2.0f; // Impulse of rigidbody Impulse = Mass * Velocity.magnitude
	
	public bool BreakByClick=false;
	
	public float SlowdownCoefficient=0.6f; // Percent of speed that hitting object has after the hit 

	public float simpleForce;
	public float explosiveForce;
	public float explosiveRadius;
	public float upwardsModifier;

	/*
	/ If you want to break the glass call this function ( myGlass.SendMessage("BreakIt") )
	*/
	public void BreakIt(bool explode){
		BrokenGlassInstance = Instantiate(BrokenGlassGO[Random.Range(0,BrokenGlassGO.Count)], transform.position, transform.rotation) as GameObject;
		
		BrokenGlassInstance.transform.localScale = transform.lossyScale;
		
		foreach(Transform t in BrokenGlassInstance.transform){
			t.GetComponent<Renderer>().material = ShardMaterial;
			t.GetComponent<Rigidbody>().mass=ShardMass;

			if (explode) {
				var cameraPosition = Camera.main.transform.position;
				var differenceRay = (cameraPosition - t.position).normalized;
				var objectFront = t.position + differenceRay;
				t.GetComponent<Rigidbody>().AddExplosionForce(
					explosiveForce, 
					objectFront, 
					explosiveRadius, 
					upwardsModifier
				);
			} else {
				var cameraPosition = Camera.main.transform.position;
				var differenceRay = (cameraPosition - t.position).normalized;
				var objectFront = t.position + differenceRay;

				t.GetComponent<Rigidbody>().AddForceAtPosition (Camera.main.transform.forward * simpleForce, objectFront);
			}
		}
		
		if(BreakSound) Destroy(Instantiate(SoundEmitter, transform.position, transform.rotation) as GameObject, SoundEmitterLifetime);
		
		if(ShardsLifetime>0) Destroy(BrokenGlassInstance,ShardsLifetime);
		Destroy(gameObject);
	}
	
	void OnMouseDown () {
		if(BreakByClick) BreakIt(false);	
	}

	void FixedUpdate() {
		if (Input.GetMouseButtonDown(1)) {
			BreakIt(true);	
		} 
	}
}
