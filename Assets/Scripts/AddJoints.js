#pragma strict

var breakForce : float;
var breakTorque : float;
var enableCollision : boolean;

private var isStart : boolean = false;

function Start () {
	isStart = true;
}

function OnCollisionEnter(collision : Collision) {
	if (isStart) {
	    var joint : HingeJoint;
		joint = gameObject.AddComponent("FixedJoint");
		joint.breakForce = breakForce;
		joint.breakTorque = breakTorque;
		joint.enableCollision = enableCollision;
		joint.connectedBody = collision.rigidbody;
	}
	isStart = false;
}