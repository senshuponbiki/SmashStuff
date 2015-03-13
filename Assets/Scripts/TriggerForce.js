#pragma strict

var simpleForce : float;

function OnTriggerEnter(other : Collider) {
	Debug.Log("Trigger Enter");
	if (!other.GetComponent.<Rigidbody>()) {
		other.gameObject.AddComponent.<Rigidbody>();
	}
	if (other.gameObject.tag === "Wood Block") {
		other.GetComponent.<Rigidbody>().AddForce(other.ClosestPointOnBounds(transform.position) * simpleForce);
	}
} 