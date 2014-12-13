#pragma strict

var simpleForce : float;

function OnTriggerEnter(other : Collider) {
	Debug.Log("Trigger Enter");
	if (!other.rigidbody) {
		other.gameObject.AddComponent("Rigidbody");
	}
	if (other.gameObject.tag === "Wood Block") {
		other.rigidbody.AddForce(other.ClosestPointOnBounds(transform.position) * simpleForce);
	}
} 