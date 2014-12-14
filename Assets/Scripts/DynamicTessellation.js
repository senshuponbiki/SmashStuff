#pragma strict

function OnMouseDown() {
	var ray : Ray = Camera.main.ScreenPointToRay(Input.mousePosition);
 	var hit : RaycastHit;
 	
 	if (Physics.Raycast(ray, hit, Mathf.Infinity)) {
 		if (hit.collider.transform.name == name) {
 			var hitPoint = (transform.position - hit.point).normalized;
 			Debug.Log(hitPoint);
		}
 	}

	var meshFilter : MeshFilter = gameObject.GetComponent("MeshFilter");
	var meshCollider : MeshCollider = gameObject.GetComponent("MeshCollider");
	var mesh = meshFilter.mesh;
	var vertices : Vector3[] = mesh.vertices;
	var newVertices = new Array();
	for (var i=0; i < mesh.vertexCount + 1; i++) {
		if (i < mesh.vertexCount) {
			newVertices.Push(vertices[i]);
		} else {
			newVertices.Push(vertices[mesh.vertexCount - 1] + new Vector3(-0.1,-0.1,0));
		}
	}
	mesh.vertices = newVertices;
	mesh.RecalculateBounds();
    mesh.RecalculateNormals();
    
    meshCollider.sharedMesh = mesh;
}