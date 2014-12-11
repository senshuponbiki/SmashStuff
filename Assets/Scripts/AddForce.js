#pragma strict

var simpleForce : float;
var explosiveForce : float;
var explosiveRadius : float;
var upwardsModifier : float;

function OnMouseDown() {
    var cameraDirection = Camera.main.transform.forward;
	rigidbody.AddForce(cameraDirection * simpleForce);
}

function FixedUpdate() {
	if (Input.GetMouseButtonDown(1)) {
	    var cameraPosition = Camera.main.transform.position;
	    var differenceRay = (cameraPosition - transform.position).normalized;
	    var objectFront = transform.position + differenceRay;
		rigidbody.AddExplosionForce(
			explosiveForce, 
			objectFront, 
			explosiveRadius, 
			upwardsModifier
		);
	} 
}