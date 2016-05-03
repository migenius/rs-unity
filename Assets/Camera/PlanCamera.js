/* Usage
	//Controls the dolly/pan/zoom of a orthograhpic camera
	
	Attach to camera gameObject
	- use active boolean
	- use uiRect Rectangle to disable input when mouse/touch is inside

*/

var isActive : boolean = true;

private var controller : CharacterController;
var movement : Vector3;
var speed : float = 5;

var initialDistanceApart : float = 0.0;
var newDistanceApart : float = 0.0;


var screenRect : Rect; //disables input if input is outside the screen (important for non mobile devices)
var uiRect : Rect = Rect(-1,-1,1,1); //set this UI element when you have to disable input when in this rectangle.


function Start () {
	controller = GetComponent( CharacterController );
	screenRect = Rect(0,0,Screen.width, Screen.height);
}

function FixedUpdate () {
	inverseMousePosition = Input.mousePosition;
	inverseMousePosition.y = Screen.height-inverseMousePosition.y;
	
	if(!screenRect.Contains(inverseMousePosition)) return;
	if(uiRect.Contains(inverseMousePosition)) return;
	
	if(Input.GetKey(KeyCode.LeftControl)) return;
	
	if(isActive) return;



	if(Input.touchCount == 2 && Input.GetTouch(1).phase == TouchPhase.Began) {
		initialDistanceApart = Mathf.Abs(((Input.GetTouch(0).position.x-Input.GetTouch(1).position.x)+(Input.GetTouch(0).position.y-Input.GetTouch(1).position.y))/2);
	}
	
	if(Input.touchCount == 2 && Input.GetTouch(1).phase == TouchPhase.Moved) {
		newDistanceApart = Mathf.Abs(((Input.GetTouch(0).position.x-Input.GetTouch(1).position.x)+(Input.GetTouch(0).position.y-Input.GetTouch(1).position.y))/2);
		
		if(newDistanceApart > initialDistanceApart) {
			GetComponent.<Camera>().orthographicSize -= (Time.deltaTime * (newDistanceApart - initialDistanceApart)) * 5;
			initialDistanceApart = newDistanceApart;
		} else {
			GetComponent.<Camera>().orthographicSize += (Time.deltaTime * (initialDistanceApart - newDistanceApart)) * 5;
			initialDistanceApart = newDistanceApart;
		}		
		GetComponent.<Camera>().orthographicSize = Mathf.Clamp(GetComponent.<Camera>().orthographicSize, 5, 80);
		
	} else if(Input.touchCount >= 3 && Input.GetTouch(2).phase == TouchPhase.Moved) {
		var vert : float = Input.GetTouch(2).deltaPosition.x;
		var hori : float = Input.GetTouch(2).deltaPosition.y;
		controller.Move(Vector3(-hori*Time.deltaTime*speed, 0, vert*Time.deltaTime*speed));
	}
	
	if(Input.GetMouseButton(0)) {
		controller.Move(Vector3(-Input.GetAxis("Mouse Y") * Time.fixedDeltaTime * speed * GetComponent.<Camera>().orthographicSize, 0.0, Input.GetAxis("Mouse X") * Time.fixedDeltaTime * speed  * GetComponent.<Camera>().orthographicSize));
	}
	
	if(Input.GetMouseButton(2)) {
		var scroll : float = Input.GetAxis("Mouse X") + Input.GetAxis("Mouse Y");
		GetComponent.<Camera>().orthographicSize = Mathf.Clamp(GetComponent.<Camera>().orthographicSize + scroll, 5.0, 80.0);
	}
	
}