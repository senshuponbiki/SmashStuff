#pragma strict

var simpleForce : float;
var explosiveForce : float;
var explosiveRadius : float;
var upwardsModifier : float;

function OnMouseDown() {
	rigidbody.AddForce(transform.forward * simpleForce);
}

function Update() {
	if (Input.GetMouseButtonDown(1)) {
	    var objectFront = new Vector3(transform.position.x, transform.position.y, transform.position.z - 1);
		rigidbody.AddExplosionForce(
			explosiveForce, 
			objectFront, 
			explosiveRadius, 
			upwardsModifier
		);
	} 
}