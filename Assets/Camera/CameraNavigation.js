enum UseAxes {MouseXAndY, MouseX, MouseY};
enum TouchNavigation {None, Drag1Finger, Drag2Fingers, Pinching, Rotating};
enum NavigationMode {None, Orbit, Pan, Look, Dolly};
enum DoubleTabMethod {None, Focus, Frame};

public var supressNavigation = false;
public var axes = UseAxes.MouseXAndY;
public var defaultMouseNavigation = NavigationMode.Orbit;

public var drag1FingerNavigation:NavigationMode = NavigationMode.Orbit;
public var drag2FingerNavigation:NavigationMode = NavigationMode.Pan;
public var drag3FingerNavigation:NavigationMode = NavigationMode.Look;
public var drag4FingerNavigation:NavigationMode = NavigationMode.None;
public var rotate2FingerNavigation:NavigationMode = NavigationMode.Look;
public var pinch2FingerNavigation:NavigationMode = NavigationMode.Dolly;

public var doubleTap1Finger:DoubleTabMethod = DoubleTabMethod.Focus;
public var doubleTap2Finger:DoubleTabMethod = DoubleTabMethod.None;
public var doubleTap3Finger:DoubleTabMethod = DoubleTabMethod.Frame; 

public var frameKey:KeyCode = KeyCode.F;
public var frameOnStartup:boolean = true;
private var firstUpdate:int = 0;

public var upperRotateLimit:float = 80;
public var lowerRotateLimit:float = 280;

// Invert the values used along the x and y mouse axes.
public var invertX:boolean = false;
public var invertY:boolean = false;

// Multipliers for the orbit, look and pan speeds.
public var orbitSpeed:float = 1.0;
public var lookSpeed:float = 1.0;
public var panSpeed:float = 1.0;
// When true the panning speed will always be the value panSpeed * the number of pixels moved.
// Otherwise the panning speed will vary depending on the camera's distance from the target point.
public var constantPanSpeed:boolean = false;
// Represents the percentage of the distance that is dollied each time.
// A value of 1.0 will result in the distance doubling each time zoomed out and
// returns to the target point when zooming in.
public var dollySpeed:float = 0.1;
// Switches the direction of the dolly.
public var reverseDollyDirection:boolean = false;
// When the user has paused their fingers whilst navigating, this timeout lets the
// nagivation change to a different gesture, the same gesture can be continued however
// this allows for change of the current gesture without the user having to remove
// their fingers from the screen.
public var touchPauseTimeout:float = 0.6;

public var longPressRadius:float = 40;

private var touchNavigation:TouchNavigation = TouchNavigation.None;
private var targetPoint:Vector3;
private var oldTouchCount:int = 0;
// When the touch input navigation gesture is being determined, an array of Vector2's
// is used over 'averageTouchInputs' frames to average each fingers deltaPosition.
// Once 'averageTouchInputs' frames have passed then the averaged delta's are used to
// determine which gesture is being performed. From there each frames deltaPosition is used.
public var touchInputDelay:int = 2;
private var touchInputCount:int = 0;
private var initialInputs:Vector2[];
private var averageInputs:Vector2[];

private var touchTimes:float[];

// Used to look up which navigation function should be replaced based on the defaultMouseNavigation enum.
private var navigationLookup = [[orbit, pan, look, dolly],
								[pan, orbit, look, dolly],
								[look, pan, orbit, dolly],
								[dolly, pan, look, orbit]];
									  
// Used to look up which navigation function should be used for the touch based navigation. 
// Based off the NavigationMode enum.
private var touchNavigationLookup = [nullNav, orbit, pan, look, dolly];

private var doubleTapMethodLookup = [nullTap, focus, frameCamera];

// Saves where the last place that the cursor was clicked/pressed. This is used for the long
// touch.
private var touchSpot:Vector2 = new Vector2(-50, -50);

private var sceneBoundingBox:Bounds;
private var sceneBoundingBoxDirty:boolean = true; 

private var mouseWheelScrollAmount:float = 0;

function OnGUI() 
{ 
	if(Event.current.type == EventType.ScrollWheel)
	{
		mouseWheelScrollAmount = Event.current.delta.y;
	}
}

private function updateTargetPoint() {	updateTargetPoint(0); }
// Recalculates where the target point should be based on the distance given
// and the camera's current transform. If a distance of 0 (zero) is given then
// it is calculated from the current distance between the camera and the target point.
private function updateTargetPoint(distance:float)
{
	// Distance CAN be less than zero, however that will just mean that the
	// target point ends up behind the translation point compared to where
	// it was before. The distance however CANNOT be zero as the would place
	// the target point at the translation vector which is not allowed.
	if(distance == 0)
	{
		distance = Vector3.Distance(transform.position, targetPoint);
	}
	var toTarget:Vector3 = Vector3.forward * distance;
	toTarget = transform.TransformDirection(toTarget);
	targetPoint = transform.position + toTarget;
}

private function lookAtTargetPoint()
{
	transform.LookAt(targetPoint);
}

// Null navigation functions for methods that are not linked to a navigation method.
private function nullNav(dx:float) {}
//private function nullNav(dx:float, dy:float) {}

// Clamps the transform along it's x axis rotation when rotating horizontally.
// Returns true if the navigation can continue it's navigation, returns false if
// it's trying to navigation past where it is clamped.
private function clampHorizontal(angleDiff:float)
{
	var v:Vector3 = transform.eulerAngles;
	var upper:boolean = v.x >= upperRotateLimit && v.x < 180;
	var lower:boolean = v.x <= lowerRotateLimit && v.x > 180;
	
	if(upper)
	{
		v.x = upperRotateLimit + 0.1;
		transform.eulerAngles = v;
	}
	else if(lower)
	{
		v.x = lowerRotateLimit - 0.1;
		transform.eulerAngles = v;
	}
	if((upper && angleDiff < 0) || (lower && angleDiff > 0) || (!upper && !lower))
	{
		return true;
	}
	return false;
}

// Performs an orbit about the target point.
private function orbit(dx:float, dy:float)
{
	if(invertX) dx = -dx;
	if(invertY) dy = -dy;

	// Transform the right vector into local space.
	var right:Vector3 = transform.TransformDirection(Vector3.right);
	if(axes != UseAxes.MouseY)
	{
		// We always want the camera's up vector to point up so we use the global up vector.
		transform.RotateAround(targetPoint, Vector3.up, dx * orbitSpeed);
	}
	if(axes != UseAxes.MouseX)
	{
		var y:float = -dy * orbitSpeed;
		// Clamp the rotation along the horizontal axis.
		if(clampHorizontal(y))
		{
			transform.RotateAround(targetPoint, right, y);
		}
	}
	
	lookAtTargetPoint();
}

// Rotates the camera in place, moving the target point to remain infront of
// the camera at the same distance it was before.
private function look(dx: float, dy: float)
{
	if(invertX) dx = -dx;
	if(invertY) dy = -dy;

	// Transform the right vector into local space.
	var right:Vector3 = transform.TransformDirection(Vector3.right);
	if(axes != UseAxes.MouseY)
	{
		// We always want the camera's up vector to point up so we use the global up vector.
		transform.Rotate(Vector3.up, dx * lookSpeed, Space.World);
	}
	if(axes != UseAxes.MouseX)
	{
		var y:float = -dy * lookSpeed;
		// Clamp the rotation along the horizontal axis.
		if(clampHorizontal(y))
		{
			transform.Rotate(right, -dy * lookSpeed, Space.World);
		}
	}
	
	//Move target point
	updateTargetPoint();
	lookAtTargetPoint();
}

// Moves the camera left and right,and up and down within it's own reference frame.
private function pan(dx:float, dy:float)
{
	if(invertX) dx = -dx;
	if(invertY) dy = -dy;
	
	var dist:float = 1.0;
	if(!constantPanSpeed)
	{
		dist = Vector3.Distance(targetPoint, transform.position) * 0.02;
	}	
	
	var enableX:float = (axes != UseAxes.MouseY ? 1.0 : 0.0);
	var enableY:float = (axes != UseAxes.MouseX ? 1.0 : 0.0);
	transform.Translate(-dx * panSpeed * dist * enableX, -dy * panSpeed * dist * enableY, 0);

	//Move target point
	updateTargetPoint();
}

// Moves the camera in and out along it's forward axis.
// Positive dz moves forward, negative back 
// In orthographic mode the camera's orthographicSize is scaled
// up and down with dz, based on dollySpeed.
private function dolly(dummy: float, dz: float) { dolly(dz); }
private function dolly(dz: float)
{
	var dirSpeed:float = reverseDollyDirection ? -1 : 1;
	if(GetComponent.<Camera>().orthographic)
	{    
		var mz:float = dz * dirSpeed;
		var speed:float = 1 + dollySpeed + (mz - 1) / 10;
		if(mz < 0)
		{
			speed = 1 - dollySpeed + (mz + 1) / 10; 
		}
		GetComponent.<Camera>().orthographicSize *= speed;
	}
	else
	{
		transform.Translate(0, 0, (dz * Vector3.Distance(targetPoint, transform.position) * dollySpeed * dirSpeed)); 
	}
}

// Resets the variables required to reset the touch input recognition.
private function clearTouchInput()
{
	touchNavigation = TouchNavigation.None;
	touchInputCount = 0;
}

// Used to look up which navigation function should be used in place of the function requested.
// By default the given function will be the one returned, however is defaultMouseNavigation has been
// set to something other than Orbit then the new function is returned.
private function getNavigateFunction(func:function(float, float))
{
	if(defaultMouseNavigation == NavigationMode.None)
	{
		return nullNav;
	}
	// Defaults to orbit at 1.
	var offset:int = 0;
	if(func == pan)		offset = 1;
	if(func == look)	offset = 2;
	if(func == dolly)	offset = 3;
	
	return navigationLookup[defaultMouseNavigation - 1][offset];
}

//private var draw_bb:Vector3[];

// Returns all 8 points for the given bounds.
private function getBoundsAllPoints(bounds:Bounds)
{
	var points:Vector3[] = new Vector3[8];
	var min:Vector3 = bounds.min;
	var max:Vector3 = bounds.max;
	points[0] = Vector3(min.x, min.y, min.z);
	points[1] = Vector3(min.x, min.y, max.z);
	points[2] = Vector3(min.x, max.y, min.z);
	points[3] = Vector3(min.x, max.y, max.z);
	points[4] = Vector3(max.x, min.y, min.z);
	points[5] = Vector3(max.x, min.y, max.z);
	points[6] = Vector3(max.x, max.y, min.z);
	points[7] = Vector3(max.x, max.y, max.z);
	
	//draw_bb = new Array(points);
	
	return points;
}

// Attempts to move the camera such that for it's given direction and field of view that the
// entire scene is in shot. The fit parameter will multiple the overall size of the 
private function frameCamera() 
{ 
	if(sceneBoundingBoxDirty)
	{
		sceneBoundingBoxDirty = false;
		var renderers:Renderer[] = FindObjectsOfType(Renderer) as Renderer[];
		if(renderers.length == 0)
		{
			sceneBoundingBox = Bounds(Vector3.zero, Vector3.zero);
			return;
		}
		
		// While creating a bounding box that starts at Vector3.zero, Vector3.zero, if the scene
		// itself never intersects Vector3.zero then the bounding box will be incorrect.
		sceneBoundingBox = renderers[0].bounds;
		for(var i:int = 1; i < renderers.length; i++)
		{
			if (renderers[i].isVisible && renderers[i].shadowCastingMode != Rendering.ShadowCastingMode.Off) 
			{
				sceneBoundingBox.Encapsulate(renderers[i].bounds);
			}
		}
	}
	frameCamera(1.0, sceneBoundingBox); 
}

private function frameCamera(fit:float, bounds:Bounds)
{
	var bb:Bounds = new Bounds(bounds.center, bounds.size);
	bb.extents = bb.extents * fit;
	
	var location:Vector3 = bb.center - (transform.forward * bb.size.magnitude);
	transform.position = location;
	targetPoint = bb.center;
	
	var verticalAperture = 1;
	var horizontalAperture = verticalAperture * GetComponent.<Camera>().aspect;
	var focal:float = verticalAperture / Mathf.Tan((GetComponent.<Camera>().fieldOfView/2) * Mathf.Deg2Rad);
	
	var points:Vector3[] = getBoundsAllPoints(bb);
	
	var maxIdxX:int = 0;
	var maxIdxY:int = 0;
	
	var maxProjectedX:float = -1;
	var maxProjectedY:float = -1;
	
	for(var i = 0; i < 8; i++)
	{
		var projX:float = 0;
		var projY:float = 0;
		
		var v:Vector3 = points[i];
		var v2:Vector3 = GetComponent.<Camera>().worldToCameraMatrix.MultiplyPoint(v);
		var v3:Vector3 = GetComponent.<Camera>().WorldToViewportPoint(v);
		
		points[i] = v2;
		if(GetComponent.<Camera>().orthographic)
		{
			projX = Mathf.Abs(v2.x);
			projY = Mathf.Abs(v2.y);
		}
		else
		{
			projX = Mathf.Abs(v3.x*2-1);
			projY = Mathf.Abs(v3.y*2-1);
		}
		//Debug.Log("projected (" + projX + "," + projY + "," + (-1*v3.z) +")");
		if(projX > maxProjectedX)
		{
			maxProjectedX = projX;
			maxIdxX = i;
		}
		if(projY > maxProjectedY)
		{
			maxProjectedY = projY;
			maxIdxY = i;
		}
	}
	
	//Debug.Log("max X: " + maxProjectedX + ", Y:" + maxProjectedY + ", Idx:" + maxIdxX + ", Idy:" + maxIdxY);
	
	if(GetComponent.<Camera>().orthographic)
	{
		if (maxProjectedX / GetComponent.<Camera>().aspect > maxProjectedY)
		{
			GetComponent.<Camera>().orthographicSize = maxProjectedX / GetComponent.<Camera>().aspect;
		}
		else
		{
			GetComponent.<Camera>().orthographicSize = maxProjectedY;
		}
	}
	else
	{
		var d:float = 0;
		if(maxProjectedX > maxProjectedY)
		{
			d = (Mathf.Abs(points[maxIdxX].x) / (horizontalAperture / focal)) + points[maxIdxX].z;
		}
		else
		{
			d = (Mathf.Abs(points[maxIdxY].y) / (verticalAperture / focal)) + points[maxIdxY].z;
		}
		
		transform.Translate(0, 0, -d);
	}
}

// Sets the camera to focus on the object at the given screen location.
// If there is no object at that location, the camera is not affected.
private function focus() { focus(Input.mousePosition); }
private function focus(position:Vector2)
{
	var hit:RaycastHit;
	if(Physics.Raycast(GetComponent.<Camera>().ScreenPointToRay(position), hit)) 
	{
		targetPoint = hit.point;
		transform.LookAt(targetPoint);
	}
}

private function nullTap() {}

// Calls the appropirate function for the current double tap gesture.
// The count parameter indicates the number of fingers involved in the current double tap.
private function doubleTap(count:int)
{
	if(count == 0)
	{
		doubleTapMethodLookup[doubleTap1Finger]();
	}
	if(count == 1)
	{
		doubleTapMethodLookup[doubleTap2Finger]();
	}
	else if(count >= 2)
	{
		doubleTapMethodLookup[doubleTap3Finger]();
	}
}

function Update()
{ 
	if (Input.GetKeyDown(frameKey))
	{
		frameCamera();
	}

	
	/*if(draw_bb.length > 0)
	{
		for(var i:int = 0; i < 8; i++)
		{
			if(i < 7)
				Debug.DrawLine(draw_bb[i], draw_bb[i + 1]);
			else
				Debug.DrawLine(draw_bb[i], draw_bb[0]);
		}
	}*/
}
		
function LateUpdate ()
{
    if (supressNavigation == true) {
        return;
    }
    var dx:float = 0;
	var dy:float = 0;
	
	var tCount:int = Input.touchCount;
	
	// Calculates when a double click has occured.
	if (Input.GetMouseButtonDown(0) && tCount == 0)
	{
		touchSpot = Input.mousePosition;
		if(Time.time - touchTimes[0] < 0.3)
		{
			doubleTap(0);
		}
		touchTimes[0] = Time.time;
	}
	
	// Calculates when a double tap of any number of fingers has occured.
	if (tCount > 0)
	{
		var t:int = tCount - 1;
		if(Input.GetTouch(t).phase == TouchPhase.Began && t < touchTimes.length)
		{
			var diff:float = Time.time - touchTimes[t];
			if(diff < 0.3)
			{
				doubleTap(t);
			}
			touchTimes[t] = Time.time;
		}
	}
	
    dx = Input.GetAxis("Mouse X");
    dy = Input.GetAxis("Mouse Y");
	if (dx || dy)
	{
		// Checks if the mouse button is down and that there are no fingers touching the screen.
		if (Input.GetMouseButton(0) && tCount == 0)
		{
			// If the mouse cursor has not moved at least 'longPressRadius' pixels away from
			// its inital clicked location, then noting happens.
			if (Vector2.Distance(Input.mousePosition, touchSpot) < longPressRadius)
			{
				return;
			}
			touchSpot = Vector2(-50, -50);
			
			if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) )
			{
				if(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				{
					getNavigateFunction(look)(dx, dy);
				}
				else
				{
					getNavigateFunction(pan)(dx, dy);
				}
			}
			else if(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				getNavigateFunction(dolly)(dx, dy);
			}
			else 
			{
				getNavigateFunction(orbit)(dx, dy);
			}
		}
        // Right click
        else if (Input.GetMouseButton(1))
        {
            getNavigateFunction(look)(dx, dy);
        }
        // Middle click
        else if (Input.GetMouseButton(2))
        {
            getNavigateFunction(pan)(dx, dy);
        }
	}
	
	//if (Input.GetAxis("Mouse ScrollWheel")) 
	if (mouseWheelScrollAmount != 0)
	{ 
		var scrollAmount:float = Mathf.Log10(Mathf.Abs(mouseWheelScrollAmount) + 1);
		dolly(mouseWheelScrollAmount < 0 ? scrollAmount : -scrollAmount); 
		mouseWheelScrollAmount = 0;
	}
	
	if (tCount != oldTouchCount)
	{
		for(var i:int = 0; i < Mathf.Min(initialInputs.length, tCount); i++)
		{
			initialInputs[i] = Input.GetTouch(i).position;
		}
		oldTouchCount = tCount;
		clearTouchInput();
	}
	
	// Sets the touchSpot for one finger press on the screen.
	if (tCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
	{
		touchSpot = Input.GetTouch(0).position;
	}
	
	if (tCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved)
	{
		if (tCount == 1 && Vector2.Distance(Input.mousePosition, touchSpot) < longPressRadius)
		{
			return;
		}
		else if(tCount == 1)
		{
			check1FingerNavigation();
		}
		else if (tCount == 2)
		{
			check2FingerNavigation();
		}
		else if (tCount == 3)
		{
			check3FingerNavigation();
		}
		else if (tCount > 3)
		{
			check4FingerNavigation();
		}
		touchSpot = Vector2(-50, -50);
	}
    if (firstUpdate < 3)
    {
        firstUpdate++;
        if (firstUpdate == 3)
        {
            frameCamera();
        }
    }
}

private function check1FingerNavigation()
{
	// Currently we only support 1 finger dragging.
	
	if (Vector2.Distance(touchSpot, Input.GetTouch(0).position) < longPressRadius)
	{
		return;
	}
	touchSpot = Vector2(-50, -50);
	var touch0:Touch = Input.GetTouch(0);
	var func = touchNavigationLookup[drag1FingerNavigation];
	if (func == dolly)
	{
		func(touch0.deltaPosition.y, 0);
	}
	else
	{
		func(touch0.deltaPosition.x, touch0.deltaPosition.y);
	}
}

// Performs the recognition and nagivation for 2 fingured inputs.
private function check2FingerNavigation()
{
	var touch0: Touch = Input.GetTouch(0);
	var touch1: Touch = Input.GetTouch(1);
	
	var diffTime:float = (touch0.deltaTime + touch1.deltaTime) * 0.5;
	
	// If the user has not moved their fingers in 0.6 seconds we assume that we should 
	// attempt to recognise the gesture. This allows the user to keep their fingers 
	// on the screen and start a new gesture.
	if(diffTime > touchPauseTimeout)
	{
		clearTouchInput();
	}
	
	var delta0:Vector2;
	var delta1:Vector2;
	
	if(touchInputCount < touchInputDelay)
	{
		touchInputCount++;
		return;
	}
	else if(touchInputCount == touchInputDelay)
	{
		touchInputCount++;
		delta0 = touch0.position - initialInputs[0];
		delta1 = touch1.position - initialInputs[1];
	}
	else
	{
		delta0 = touch0.deltaPosition;
		delta1 = touch1.deltaPosition;
	}

	if(touchNavigation == TouchNavigation.None || touchNavigation == TouchNavigation.Drag2Fingers)
	{
		var angleDifference:float = Vector2.Angle(delta0, delta1);
		// If the angle between the difference vectors is less than 60 then it is likely the two fingers
		// are moving in the same direction which indicates a drag.
		if(Mathf.Abs(angleDifference) < 60 || touchNavigation == TouchNavigation.Drag2Fingers || 
			(pinch2FingerNavigation == NavigationMode.None && rotate2FingerNavigation == NavigationMode.None))
		{
			// Dragging.
			// While averaging should be just 0.5, often this results in movement that's much faster
			// that what a normal drag with the mouse would be.
			var averageMove:Vector2 = (touch0.deltaPosition + touch1.deltaPosition) * 0.5;
			//var averageMove:Vector2 = delta0 * 0.5;
			
			touchNavigation = TouchNavigation.Drag2Fingers;
			touchNavigationLookup[drag2FingerNavigation](averageMove.x, averageMove.y);
			return;
		}
	}
	
	// Makes use of the average vectors. These are used to actually calculate
	// the gesture being executed.
	var oldVectorAverage:Vector2 = (touch0.position - delta0) - (touch1.position - delta1);
	
	// These always use the most recent deltaPosition 
	var newVector:Vector2 = touch0.position - touch1.position;
	var newMagnitude:float = newVector.magnitude;
	
	var oldVector:Vector2 = (touch0.position - touch0.deltaPosition) - (touch1.position - touch1.deltaPosition);
	
	var diffAverage:float = newMagnitude - oldVectorAverage.magnitude;
	var curlAverage:Vector3 = Vector3.Cross(oldVectorAverage.normalized, newVector.normalized);
	
	// Find the ratio between the curl of the vectors and the difference in
	// maginutde. When this is over 1000 this usually indicates that the difference
	// is greater which indicates a pinch, otherwise it indicates that the curl is
	// greater and that the user is rotating.
	var ratioAverage:float = Mathf.Abs(diffAverage / curlAverage.z);
	
	if(touchNavigation == TouchNavigation.None || touchNavigation == TouchNavigation.Rotating)
	{
		if (ratioAverage < 1000 || touchNavigation == TouchNavigation.Rotating || 
			pinch2FingerNavigation == NavigationMode.None)
		{
			var curl:Vector3 = Vector3.Cross(oldVector.normalized, newVector.normalized);
			touchNavigation = TouchNavigation.Rotating;
			touchNavigationLookup[rotate2FingerNavigation](curl.z * 400, 0);
		}
	}
	
	if(touchNavigation == TouchNavigation.None || touchNavigation == TouchNavigation.Pinching)
	{
		if((ratioAverage >= 1000 && Mathf.Abs(diffAverage) > 20) || touchNavigation == TouchNavigation.Pinching || 
			rotate2FingerNavigation == NavigationMode.None)
		{
			var diff:float = newMagnitude - oldVector.magnitude;
			touchNavigation = TouchNavigation.Pinching;
			
			var diffLog:float = 0;
			if(Mathf.Abs(diff) > 1)
			{
				diffLog = Mathf.Log(Mathf.Abs(diff)) / 1.25;
			}
			var d:float = 0.1 + diffLog;
			touchNavigationLookup[pinch2FingerNavigation](diff > 0 ? d : -d, 0);
		}
	}
}

private function check3FingerNavigation()
{
	// Currently we only support 3 finger dragging.
	
	var touch0:Touch = Input.GetTouch(0);
	var touch1:Touch = Input.GetTouch(1);
	var touch2:Touch = Input.GetTouch(2);

	var average:Vector2 = (touch0.deltaPosition + touch1.deltaPosition + touch2.deltaPosition) / 3;
	var func = touchNavigationLookup[drag3FingerNavigation];
	if (func == dolly)
	{
		func(average.y, 0);
	}
	else
	{
		func(average.x, average.y);
	}
}

private function check4FingerNavigation()
{
	// Currently we only support 4 finger dragging.
	
	var touch0:Touch = Input.GetTouch(0);
	var touch1:Touch = Input.GetTouch(1);
	var touch2:Touch = Input.GetTouch(2);
	var touch3:Touch = Input.GetTouch(3);
	
	var average:Vector2 = (touch0.deltaPosition + touch1.deltaPosition + touch2.deltaPosition + touch3.deltaPosition) / 4;
	var func = touchNavigationLookup[drag4FingerNavigation];
	if (func == dolly)
	{
		func(average.y, 0);
	}
	else
	{
		func(average.x, average.y);
	}
}

function Start ()
{
	averageInputs = new Vector2[16];
	initialInputs = new Vector2[16];
	touchTimes = new float[16];
	// Make the rigid body not change rotation
	if (GetComponent.<Rigidbody>())
		GetComponent.<Rigidbody>().freezeRotation = true;
	
	targetPoint = new Vector3();
	transform.LookAt(targetPoint);
}
