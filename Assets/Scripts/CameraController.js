#pragma strict

var player : GameObject;
private var offset : Vector3;

function Start () {
	offset = transform.position;
}

function LateUpdate () {
	transform.position = player.transform.position + offset;
	
	var mouseX = Input.GetAxis("Mouse X");
	var mouseY = Input.GetAxis("Mouse Y");
	transform.Rotate(-mouseY, mouseX, 0);
}