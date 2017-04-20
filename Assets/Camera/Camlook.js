/* Usage
	//Controls the look mode of the camera

	Attach to camera gameObject
	- use active boolean
	- use uiRect Rectangle to disable input when mouse/touch is inside

*/


var isActive : boolean = true;

enum RotationAxes {MouseXAndY, MouseX, MouseY};
public var axes = RotationAxes.MouseXAndY;
public var sensitivityX = 15;
public var sensitivityY = 15;
public var touchSensitivityX = 3;
public var touchSensitivityY = 3;

public var minimumX = -360;
public var maximumX = 360;

public var minimumY = -60;
public var maximumY = 60;

var rotationX = 0;
var rotationY = 0;

var originalRotation : Quaternion;
var requiresCtrl : boolean = false;

var touchSpot : Vector3;
var touchSpotMove : Vector3;
var lastPos : Vector2;
var touchBegan : boolean = false;

var screenRect : Rect; //disables input if input is outside the screen (important for non mobile devices)
var uiRect : Rect = Rect(-1,-1,1,1); //set this UI element when you have to disable input when in this rectangle.

function Start() {
	screenRect = Rect(0,0,Screen.width, Screen.height);
}
function LateUpdate ()
{
	var inverseMousePosition = Input.mousePosition;
		if(Input.touchCount == 1) inverseMousePosition = Input.GetTouch(0).position;
	inverseMousePosition.y = Screen.height-inverseMousePosition.y;

	if(!screenRect.Contains(inverseMousePosition)) return;
	if(uiRect.Contains(inverseMousePosition)) return;
	
	if(!isActive) return;

	if (Input.touchCount == 1) {
		if(Input.GetTouch(0).phase == TouchPhase.Began) {
			//originalRotation = transform.localRotation;
			touchSpot = Input.GetTouch(0).position;
			touchBegan = true;
		}
	} else {
		touchBegan = false;	
		return;
	}

	if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved && touchBegan) 
	{
		if(Rect(0,0,1024, 100).Contains(Input.GetTouch(0).position)) return;
		touchSpotMove = Input.GetTouch(0).position;
		
		if(Vector3.Distance(touchSpotMove, touchSpot) < 40) {
			lastPos = Input.GetTouch(0).position;
			return;
		}
		touchSpot = Vector3(-50,-50,-50);
		
		var touchDeltaPosition:Vector2; // = Input.GetTouch(0).deltaPosition;
		touchDeltaPosition = Input.GetTouch(0).deltaPosition;// - lastPos;
		lastPos = Input.GetTouch(0).position;
		if (axes == RotationAxes.MouseXAndY)
		{
			// Read the mouse input axis
			rotationX += touchDeltaPosition.x * Time.deltaTime * 4;
			rotationY -= touchDeltaPosition.y * Time.deltaTime * 4;

			rotationX = ClampAngle (rotationX, minimumX, maximumX);
			rotationY = ClampAngle (rotationY, minimumY, maximumY);

			var TxQuaternion = Quaternion.AngleAxis (rotationX, Vector3.up);
			var TyQuaternion = Quaternion.AngleAxis (rotationY, Vector3.right);

			transform.localRotation = originalRotation * TxQuaternion * TyQuaternion;
		}
		else if (axes == RotationAxes.MouseX)
		{
			rotationX += touchDeltaPosition.x * Time.deltaTime * 4;
			rotationX = ClampAngle (rotationX, minimumX, maximumX);

			var TxQuaternion2 = Quaternion.AngleAxis (rotationX, Vector3.up);
			transform.localRotation = originalRotation * TxQuaternion2;
		}
		else
		{
			rotationY += touchDeltaPosition.y * Time.deltaTime * 4;
			rotationY = ClampAngle (rotationY, minimumY, maximumY);

			var TyQuaternion2 = Quaternion.AngleAxis (-rotationY, Vector3.right);
			transform.localRotation = originalRotation * TyQuaternion2;
		}
		
		//transform.localEulerAngles.z = 0;
	}
	
	if(Input.GetMouseButtonDown(0)) {
		originalRotation = transform.localRotation;
		rotationX = 0;
		rotationY = 0;
	}

	if(Input.GetMouseButton(0))
	{
		if(Rect(0,0,1024, 80).Contains(inverseMousePosition)) return;
//		if(centralManager.sceneManager.uiRect.Contains(inverseMousePosition)) return;
		
		if(requiresCtrl) {
			if(!Input.GetKey(KeyCode.LeftControl)) return;
		} else {
			if(Input.GetKey(KeyCode.LeftControl)) return;
		}
	
		if (axes == RotationAxes.MouseXAndY)
		{
			// Read the mouse input axis
			rotationX += Input.GetAxis("Mouse X") * sensitivityX;
			rotationY += Input.GetAxis("Mouse Y") * sensitivityY;

			rotationX = ClampAngle (rotationX, minimumX, maximumX);
			rotationY = ClampAngle (rotationY, minimumY, maximumY);

			var xQuaternion = Quaternion.AngleAxis (rotationX, Vector3.up);
			var yQuaternion = Quaternion.AngleAxis (rotationY, -Vector3.right);

			transform.localRotation = originalRotation * xQuaternion * yQuaternion;
		}
		else if (axes == RotationAxes.MouseX)
		{
			rotationX += Input.GetAxis("Mouse X") * sensitivityX;
			rotationX = ClampAngle (rotationX, minimumX, maximumX);

			var xQuaternion2 = Quaternion.AngleAxis (rotationX, Vector3.up);
			transform.localRotation = originalRotation * xQuaternion2;
		}
		else
		{
			rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
			rotationY = ClampAngle (rotationY, minimumY, maximumY);

			var yQuaternion2 = Quaternion.AngleAxis (-rotationY, Vector3.right);
			transform.localRotation = originalRotation * yQuaternion2;
		}
		
		transform.localEulerAngles.z = 0;
	}
} 

/*
function Start ()
{
	// Make the rigid body not change rotation
	if (rigidbody)
		rigidbody.freezeRotation = true;
	originalRotation = transform.localRotation;
	rotationX = 0;
	rotationY = 0;
}
*/

function ClampAngle (angle:float, min, max)
{
	if (angle < -360)
		angle += 360;
	if (angle > 360)
		angle -= 360;
	return Mathf.Clamp (angle, min, max);
}
/*
enum RotationAxes {MouseXAndY, MouseX, MouseY};
public var axes = RotationAxes.MouseXAndY;
public var sensitivityX : float = 15;
public var sensitivityY : float = 15;
public var touchSensitivityX : float = 3;
public var touchSensitivityY : float = 3;

public var minimumX : float = -360;
public var maximumX : float = 360;

public var minimumY : float = -60;
public var maximumY : float = 60;

var rotationX : float = 0;
var rotationY : float = 0;

var originalRotation : Quaternion;
var requiresCtrl : boolean = false;

var touchSpot : Vector3;
var touchSpotMove : Vector3;
var lastPos : Vector2;
var touchBegan : boolean = false;

function OnGUI() {
	GUI.Label(Rect(300,300, 240, 20), "OR: x:" +originalRotation.x.ToString("f3") + " y:" +originalRotation.y.ToString("f3") + " z:" +originalRotation.z.ToString("f3") + " w:" +originalRotation.w.ToString("f3"));	
	GUI.Label(Rect(300,320, 220, 20), "RotX: " + rotationX.ToString());
	GUI.Label(Rect(300,340, 240, 20), "RotY: " + rotationY.ToString());
}

function LateUpdate ()
{
	inverseMousePosition = Input.mousePosition;
		if(Input.touchCount == 1) inverseMousePosition = Input.GetTouch(0).position;
	inverseMousePosition.y = Screen.height-inverseMousePosition.y;

	if(!Rect(0,0,1024,768).Contains(inverseMousePosition)) return;
	if(centralManager.sceneManager.showRender) return;
	if(!centralManager.sceneManager.canUseCamera) return;

	if (Input.touchCount == 1) {
		if(Input.GetTouch(0).phase == TouchPhase.Began) {
			//originalRotation = transform.localRotation;
			touchSpot = Input.GetTouch(0).position;
			touchBegan = true;
			originalRotation = transform.localRotation;
			rotationX = 0;
			rotationY = 0;
		}
	}
	
	if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Ended) {
		originalRotation = transform.localRotation;
		rotationX = 0;
		rotationY = 0;
		touchBegan = false;
	}

	if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved && touchBegan) 
	{
		if(Rect(0,0,1024, 80).Contains(Input.GetTouch(0).position)) return;
		if(centralManager.sceneManager.uiRect.Contains(Input.GetTouch(0).position)) return;
		
		var touchDeltaPosition:Vector2 = Input.GetTouch(0).deltaPosition;
		if (axes == RotationAxes.MouseXAndY)
		{
			// Read the mouse input axis
			rotationX += touchDeltaPosition.x * Time.deltaTime * touchSensitivityX;
			rotationY += touchDeltaPosition.y * Time.deltaTime * touchSensitivityY;

			rotationX = ClampAngle (rotationX, minimumX, maximumX);
			rotationY = ClampAngle (rotationY, minimumY, maximumY);

			var TxQuaternion = Quaternion.AngleAxis (rotationX, Vector3.up);
			var TyQuaternion = Quaternion.AngleAxis (rotationY, -Vector3.right);

			transform.localRotation = originalRotation * TxQuaternion * TyQuaternion;
		}
		else if (axes == RotationAxes.MouseX)
		{
			rotationX += touchDeltaPosition.x * Time.deltaTime * touchSensitivityX;
			rotationX = ClampAngle (rotationX, minimumX, maximumX);

			var TxQuaternion2 = Quaternion.AngleAxis (rotationX, Vector3.up);
			transform.localRotation = originalRotation * TxQuaternion2;
		}
		else
		{
			rotationY += touchDeltaPosition.y * Time.deltaTime * touchSensitivityY;
			rotationY = ClampAngle (rotationY, minimumY, maximumY);

			var TyQuaternion2 = Quaternion.AngleAxis (-rotationY, Vector3.right);
			transform.localRotation = originalRotation * TyQuaternion2;
		}
		
		transform.localEulerAngles.z = 0;
	}

	if(Input.GetMouseButtonDown(0)) {
		originalRotation = transform.localRotation;
		rotationX = 0;
		rotationY = 0;
	}

	if(Input.GetMouseButton(0))
	{
		if(Rect(0,0,1024, 80).Contains(inverseMousePosition)) return;
		if(centralManager.sceneManager.uiRect.Contains(inverseMousePosition)) return;
		
		if(requiresCtrl) {
			if(!Input.GetKey(KeyCode.LeftControl)) return;
		} else {
			if(Input.GetKey(KeyCode.LeftControl)) return;
		}
	
		if (axes == RotationAxes.MouseXAndY)
		{
			// Read the mouse input axis
			rotationX += Input.GetAxis("Mouse X") * sensitivityX;
			rotationY += Input.GetAxis("Mouse Y") * sensitivityY;

			rotationX = ClampAngle (rotationX, minimumX, maximumX);
			rotationY = ClampAngle (rotationY, minimumY, maximumY);

			var xQuaternion = Quaternion.AngleAxis (rotationX, Vector3.up);
			var yQuaternion = Quaternion.AngleAxis (rotationY, -Vector3.right);

			transform.localRotation = originalRotation * xQuaternion * yQuaternion;
		}
		else if (axes == RotationAxes.MouseX)
		{
			rotationX += Input.GetAxis("Mouse X") * sensitivityX;
			rotationX = ClampAngle (rotationX, minimumX, maximumX);

			var xQuaternion2 = Quaternion.AngleAxis (rotationX, Vector3.up);
			transform.localRotation = originalRotation * xQuaternion2;
		}
		else
		{
			rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
			rotationY = ClampAngle (rotationY, minimumY, maximumY);

			var yQuaternion2 = Quaternion.AngleAxis (-rotationY, Vector3.right);
			transform.localRotation = originalRotation * yQuaternion2;
		}
		
		transform.localEulerAngles.z = 0;
	}
} 


function Start ()
{
	// Make the rigid body not change rotation
	if (rigidbody)
		rigidbody.freezeRotation = true;
	originalRotation = transform.localRotation;
	rotationX = 0;
	rotationY = 0;
}

function ClampAngle (angle, min, max)
{
	if (angle < -360)
		angle += 360;
	if (angle > 360)
		angle -= 360;
	return Mathf.Clamp (angle, min, max);
}*/
