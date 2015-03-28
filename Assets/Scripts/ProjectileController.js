#pragma strict

var speed : float;
var simpleForce : float;
private var target : Vector3;

function OnTriggerEnter(other : Collider) {
	if (other.gameObject.tag == "Wood Block") {
		Debug.Log(other.gameObject.tag);
	 	if (!other.GetComponent.<Rigidbody>()) {
			other.gameObject.AddComponent.<Rigidbody>();
		}
		other.GetComponent.<Rigidbody>().AddForce(other.ClosestPointOnBounds(transform.position) * simpleForce);
	}
}

function Update() {
	transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
}

function Start() {
	var ray : Ray = Camera.main.ScreenPointToRay(Input.mousePosition);
	target = ray.GetPoint(1000);
}