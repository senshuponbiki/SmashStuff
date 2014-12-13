#pragma strict

var projectile : GameObject;
var speed : float;

function Update() {
	if (Input.GetMouseButtonDown(0)) {
		var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
	    var hit : RaycastHit;
	    if (Physics.Raycast(ray, hit, 1000)){
	        var clone : GameObject;
			clone = Instantiate(projectile, transform.position, transform.rotation);
			
	        clone.transform.LookAt(hit.point); 
	        clone.rigidbody.AddForce(clone.transform.forward * speed);
	        clone.transform.Rotate(Vector3.right * 90);
	        
	        Destroy(clone, 3);
   	    }
	}
}