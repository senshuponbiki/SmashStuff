#pragma strict

var block : GameObject;
var rows : int;
var cols : int;
var height : int;

function Start () {
	for (var i=0; i < rows; i++) {
		for (var j=0; j < cols; j++) {
			for (var k=0; k < height; k++) {
			    var newPosition : Vector3 = new Vector3(
			    	transform.position.x + i + 0.5,
			    	transform.position.y + k + 0.5,
			    	transform.position.z + j + 0.5
			    );
				Instantiate(block, newPosition, Quaternion.identity);
			}
		}
	}
}