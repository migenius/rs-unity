/* Usage
	//Controls the dolly/pan of the camera

	Attach to camera gameObject
	- use active boolean
	- use uiRect Rectangle to disable input when mouse/touch is inside

*/

var isActive : boolean = true;

public var speed : float = 10.0;
private var moveDirection = Vector3.zero;
private var characterController : CharacterController;
var touchSpeed : float = 5.0;

var screenRect : Rect; //disables input if input is outside the screen (important for non mobile devices)
var uiRect : Rect = Rect(-1,-1,1,1); //set this UI element when you have to disable input when in this rectangle.

function Start() {
	characterController = GetComponent(CharacterController);
	screenRect = Rect(0,0,Screen.width, Screen.height);
}

function LateUpdate () {
	if(isActive) return;

	inverseMousePosition = Input.mousePosition;
	inverseMousePosition.y = Screen.height-inverseMousePosition.y;
	if(!screenRect.Contains(inverseMousePosition)) return;
	if(uiRect.Contains(inverseMousePosition)) return;

	moveDirection = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Height"), Input.GetAxis("Vertical"));

	if(Input.touchCount == 2 && Input.GetTouch(0).phase == TouchPhase.Moved) 
	{
		var touchDeltaPosition:Vector2 = Input.GetTouch(0).deltaPosition;
  //old moveDirection = new Vector3(-touchDeltaPosition.x * Time.deltaTime, 0, -touchDeltaPosition.y * Time.deltaTime);
		moveDirection = new Vector3(0, 0, -touchDeltaPosition.y * Time.deltaTime * touchSpeed);
	}
	else if(Input.touchCount >= 3 && Input.GetTouch(0).phase == TouchPhase.Moved)
	{
		var touchDeltaPosition2:Vector2 = Input.GetTouch(0).deltaPosition;
		moveDirection = new Vector3(-touchDeltaPosition2.x * Time.deltaTime * touchSpeed, -touchDeltaPosition2.y * Time.deltaTime * touchSpeed, moveDirection.z);
	}

	moveDirection = transform.TransformDirection(moveDirection);
	moveDirection *= speed;
	
	characterController.Move(moveDirection);
}

