#pragma strict

var simpleForce : float;
var explosiveForce : float;
var explosiveRadius : float;
var upwardsModifier : float;
var useRayCasting : boolean;

function OnMouseDown() {
    var cameraDirection = Camera.main.transform.forward;
	rigidbody.AddForce(cameraDirection * simpleForce);
}

function Update() {
	if (useRayCasting) {
		GetMouseInfo();
	}
}

function FixedUpdate() {
	if (Input.GetMouseButtonDown(1)) {
	    var cameraPosition = Camera.main.transform.position;
	    var differenceRay = (cameraPosition - transform.position).normalized;
	    var objectFront = transform.position + differenceRay;
	    if (!rigidbody) {
	    	gameObject.AddComponent("Rigidbody");
	    }
		rigidbody.AddExplosionForce(
			explosiveForce, 
			objectFront, 
			explosiveRadius, 
			upwardsModifier
		);
	} 
}

function GetMouseInfo() {
    var ray : Ray = Camera.main.ScreenPointToRay(Input.mousePosition);
 	var hit : RaycastHit;

	if (Input.GetMouseButtonDown(0)) {
	 	if (Physics.Raycast(ray,hit, Mathf.Infinity)) {
	     	if(hit.collider.transform.name == name) {
	 	 		var cameraDirection = Camera.main.transform.forward;
			    var objectRigidbody = rigidbody;
			    if (!rigidbody) {
			    	objectRigidbody = gameObject.AddComponent("Rigidbody");
			    }
				objectRigidbody.AddForce(cameraDirection * simpleForce);
	     	}
	 	}
 	}
}

