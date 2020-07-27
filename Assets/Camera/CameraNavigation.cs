using System;
using UnityEngine;
using System.Collections;

public enum UseAxes
{
    MouseXAndY = 0,
    MouseX = 1,
    MouseY = 2
}

public enum TouchNavigation
{
    None = 0,
    Drag1Finger = 1,
    Drag2Fingers = 2,
    Pinching = 3,
    Rotating = 4
}

public enum NavigationMode
{
    None = 0,
    Orbit = 1,
    Pan = 2,
    Look = 3,
    Dolly = 4
}

public enum DoubleTabMethod
{
    None = 0,
    Focus = 1,
    Frame = 2
}

[System.Serializable]
public partial class CameraNavigation : MonoBehaviour
{
    public bool supressNavigation;
    public UseAxes axes;
    public NavigationMode defaultMouseNavigation;
    public NavigationMode drag1FingerNavigation;
    public NavigationMode drag2FingerNavigation;
    public NavigationMode drag3FingerNavigation;
    public NavigationMode drag4FingerNavigation;
    public NavigationMode rotate2FingerNavigation;
    public NavigationMode pinch2FingerNavigation;
    public DoubleTabMethod doubleTap1Finger;
    public DoubleTabMethod doubleTap2Finger;
    public DoubleTabMethod doubleTap3Finger;
    public KeyCode frameKey;
    public bool frameOnStartup;
    private int firstUpdate;
    public float upperRotateLimit;
    public float lowerRotateLimit;
    // Invert the values used along the x and y mouse axes.
    public bool invertX;
    public bool invertY;
    // Multipliers for the orbit, look and pan speeds.
    public float orbitSpeed;
    public float lookSpeed;
    public float panSpeed;
    // When true the panning speed will always be the value panSpeed * the number of pixels moved.
    // Otherwise the panning speed will vary depending on the camera's distance from the target point.
    public bool constantPanSpeed;
    // Represents the percentage of the distance that is dollied each time.
    // A value of 1.0 will result in the distance doubling each time zoomed out and
    // returns to the target point when zooming in.
    public float dollySpeed;
    // Switches the direction of the dolly.
    public bool reverseDollyDirection;
    // When the user has paused their fingers whilst navigating, this timeout lets the
    // nagivation change to a different gesture, the same gesture can be continued however
    // this allows for change of the current gesture without the user having to remove
    // their fingers from the screen.
    public float touchPauseTimeout;
    public float longPressRadius;
    private TouchNavigation touchNavigation;
    private Vector3 targetPoint;
    private int oldTouchCount;
    // When the touch input navigation gesture is being determined, an array of Vector2's
    // is used over 'averageTouchInputs' frames to average each fingers deltaPosition.
    // Once 'averageTouchInputs' frames have passed then the averaged delta's are used to
    // determine which gesture is being performed. From there each frames deltaPosition is used.
    public int touchInputDelay;
    private int touchInputCount;
    private Vector2[] initialInputs;
    private float[] touchTimes;
    // Custom delegate for navigation actions
    public delegate void NavigationAction(params object[] arguments);
    // Used to look up which navigation function should be replaced based on the defaultMouseNavigation enum.
    private NavigationAction[][] navigationLookup;
    // Used to look up which navigation function should be used for the touch based navigation. 
    // Based off the NavigationMode enum.
    private NavigationAction[] touchNavigationLookup;
    private NavigationAction[] doubleTapMethodLookup;
    // Saves where the last place that the cursor was clicked/pressed. This is used for the long
    // touch.
    private Vector2 touchSpot;
    private Bounds sceneBoundingBox;
    private bool sceneBoundingBoxDirty;
    private float mouseWheelScrollAmount;
    public virtual void OnGUI()
    {
        if (Event.current.type == EventType.ScrollWheel)
        {
            this.mouseWheelScrollAmount = Event.current.delta.y;
        }
    }

    private void updateTargetPoint()
    {
        this.updateTargetPoint(0);
    }

    // Recalculates where the target point should be based on the distance given
    // and the camera's current transform. If a distance of 0 (zero) is given then
    // it is calculated from the current distance between the camera and the target point.
    private void updateTargetPoint(float distance)
    {
         // Distance CAN be less than zero, however that will just mean that the
         // target point ends up behind the translation point compared to where
         // it was before. The distance however CANNOT be zero as the would place
         // the target point at the translation vector which is not allowed.
        if (distance == 0)
        {
            distance = Vector3.Distance(this.transform.position, this.targetPoint);
        }
        Vector3 toTarget = Vector3.forward * distance;
        toTarget = this.transform.TransformDirection(toTarget);
        this.targetPoint = this.transform.position + toTarget;
    }

    private void lookAtTargetPoint()
    {
        this.transform.LookAt(this.targetPoint);
    }

    // Null navigation functions for methods that are not linked to a navigation method.
    private void nullNav(object[] args)
    {
    }

    //private function nullNav(dx:float, dy:float) {}
    // Clamps the transform along it's x axis rotation when rotating horizontally.
    // Returns true if the navigation can continue it's navigation, returns false if
    // it's trying to navigation past where it is clamped.
    private bool clampHorizontal(float angleDiff)
    {
        Vector3 v = this.transform.eulerAngles;
        bool upper = (v.x >= this.upperRotateLimit) && (v.x < 180);
        bool lower = (v.x <= this.lowerRotateLimit) && (v.x > 180);
        if (upper)
        {
            v.x = this.upperRotateLimit + 0.1f;
            this.transform.eulerAngles = v;
        }
        else
        {
            if (lower)
            {
                v.x = this.lowerRotateLimit - 0.1f;
                this.transform.eulerAngles = v;
            }
        }
        if (((upper && (angleDiff < 0)) || (lower && (angleDiff > 0))) || (!upper && !lower))
        {
            return true;
        }
        return false;
    }

    // Performs an orbit about the target point.
    private void orbit(object[] args)
    {
        float dx = (float)args[0];
        float dy = (float)args[1];
        if (this.invertX)
        {
            dx = -dx;
        }
        if (this.invertY)
        {
            dy = -dy;
        }
        // Transform the right vector into local space.
        Vector3 right = this.transform.TransformDirection(Vector3.right);
        if (this.axes != UseAxes.MouseY)
        {
             // We always want the camera's up vector to point up so we use the global up vector.
            this.transform.RotateAround(this.targetPoint, Vector3.up, dx * this.orbitSpeed);
        }
        if (this.axes != UseAxes.MouseX)
        {
            float y = -dy * this.orbitSpeed;
            // Clamp the rotation along the horizontal axis.
            if (this.clampHorizontal(y))
            {
                this.transform.RotateAround(this.targetPoint, right, y);
            }
        }
        this.lookAtTargetPoint();
    }

    // Rotates the camera in place, moving the target point to remain infront of
    // the camera at the same distance it was before.
    private void look(object[] args)
    {
        float dx = (float)args[0];
        float dy = (float)args[1];
        if (this.invertX)
        {
            dx = -dx;
        }
        if (this.invertY)
        {
            dy = -dy;
        }
        // Transform the right vector into local space.
        Vector3 right = this.transform.TransformDirection(Vector3.right);
        if (this.axes != UseAxes.MouseY)
        {
             // We always want the camera's up vector to point up so we use the global up vector.
            this.transform.Rotate(Vector3.up, dx * this.lookSpeed, Space.World);
        }
        if (this.axes != UseAxes.MouseX)
        {
            float y = -dy * this.lookSpeed;
            // Clamp the rotation along the horizontal axis.
            if (this.clampHorizontal(y))
            {
                this.transform.Rotate(right, -dy * this.lookSpeed, Space.World);
            }
        }
        //Move target point
        this.updateTargetPoint();
        this.lookAtTargetPoint();
    }

    // Moves the camera left and right,and up and down within it's own reference frame.
    private void pan(object[] args)
    {
        float dx = (float)args[0];
        float dy = (float)args[1];
        if (this.invertX)
        {
            dx = -dx;
        }
        if (this.invertY)
        {
            dy = -dy;
        }
        float dist = 1f;
        if (!this.constantPanSpeed)
        {
            dist = Vector3.Distance(this.targetPoint, this.transform.position) * 0.02f;
        }
        float enableX = this.axes != UseAxes.MouseY ? 1f : 0f;
        float enableY = this.axes != UseAxes.MouseX ? 1f : 0f;
        this.transform.Translate(((-dx * this.panSpeed) * dist) * enableX, ((-dy * this.panSpeed) * dist) * enableY, 0);
        //Move target point
        this.updateTargetPoint();
    }

    // Moves the camera in and out along it's forward axis.
    // Positive dz moves forward, negative back 
    // In orthographic mode the camera's orthographicSize is scaled
    // up and down with dz, based on dollySpeed.
    private void dolly(object[] args)
    {
        float dz = (float)args[0];
        if (args.Length == 2)
        {
            dz = (float)args[1];
        }

        float dirSpeed = this.reverseDollyDirection ? -1 : 1;
        if (this.GetComponent<Camera>().orthographic)
        {
            float mz = dz * dirSpeed;
            float speed = (1 + this.dollySpeed) + ((mz - 1) / 10);
            if (mz < 0)
            {
                speed = (1 - this.dollySpeed) + ((mz + 1) / 10);
            }
            this.GetComponent<Camera>().orthographicSize = this.GetComponent<Camera>().orthographicSize * speed;
        }
        else
        {
            this.transform.Translate(0, 0, ((dz * Vector3.Distance(this.targetPoint, this.transform.position)) * this.dollySpeed) * dirSpeed);
        }
    }

    // Resets the variables required to reset the touch input recognition.
    private void clearTouchInput()
    {
        this.touchNavigation = TouchNavigation.None;
        this.touchInputCount = 0;
    }

    // Used to look up which navigation function should be used in place of the function requested.
    // By default the given function will be the one returned, however is defaultMouseNavigation has been
    // set to something other than Orbit then the new function is returned.
    private NavigationAction getNavigateFunction(NavigationAction func)
    {
        if (this.defaultMouseNavigation == NavigationMode.None)
        {
            return this.nullNav;
        }
        // Defaults to orbit at 1.
        int offset = 0;
        if (func == (NavigationAction) this.pan)
        {
            offset = 1;
        }
        if (func == (NavigationAction) this.look)
        {
            offset = 2;
        }
        if (func == (NavigationAction) this.dolly)
        {
            offset = 3;
        }
        return this.navigationLookup[((int) (this.defaultMouseNavigation - 1))][offset];
    }

    //private var draw_bb:Vector3[];
    // Returns all 8 points for the given bounds.
    private Vector3[] getBoundsAllPoints(Bounds bounds)
    {
        Vector3[] points = new Vector3[8];
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        points[0] = new Vector3(min.x, min.y, min.z);
        points[1] = new Vector3(min.x, min.y, max.z);
        points[2] = new Vector3(min.x, max.y, min.z);
        points[3] = new Vector3(min.x, max.y, max.z);
        points[4] = new Vector3(max.x, min.y, min.z);
        points[5] = new Vector3(max.x, min.y, max.z);
        points[6] = new Vector3(max.x, max.y, min.z);
        points[7] = new Vector3(max.x, max.y, max.z);
        //draw_bb = new Array(points);
        return points;
    }

    // Attempts to move the camera such that for it's given direction and field of view that the
    // entire scene is in shot. The fit parameter will multiple the overall size of the 
    private void frameCamera(object[] args /*float fit, Bounds bounds*/)
    {
        float fit = 1f;
        Bounds bounds;
        if (args.Length == 0)
        {
            if (this.sceneBoundingBoxDirty)
            {
                this.sceneBoundingBoxDirty = false;
                Renderer[] renderers = ((Renderer[])UnityEngine.Object.FindObjectsOfType(typeof(Renderer))) as Renderer[];
                if (renderers.Length == 0)
                {
                    this.sceneBoundingBox = new Bounds(Vector3.zero, Vector3.zero);
                    return;
                }
                // While creating a bounding box that starts at Vector3.zero, Vector3.zero, if the scene
                // itself never intersects Vector3.zero then the bounding box will be incorrect.
                this.sceneBoundingBox = renderers[0].bounds;
                int j = 1;
                while (j < renderers.Length)
                {
                    if (renderers[j].isVisible && (renderers[j].shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off))
                    {
                        this.sceneBoundingBox.Encapsulate(renderers[j].bounds);
                    }
                    j++;
                }
            }
            bounds = this.sceneBoundingBox;
        } else
        {
            fit = (float)args[0];
            bounds = (Bounds)args[1];
        }        

        Bounds bb = new Bounds(bounds.center, bounds.size);
        bb.extents = bb.extents * fit;
        Vector3 location = bb.center - (this.transform.forward * bb.size.magnitude);
        this.transform.position = location;
        this.targetPoint = bb.center;
        int verticalAperture = 1;
        float horizontalAperture = verticalAperture * this.GetComponent<Camera>().aspect;
        float focal = verticalAperture / Mathf.Tan((this.GetComponent<Camera>().fieldOfView / 2) * Mathf.Deg2Rad);
        Vector3[] points = this.getBoundsAllPoints(bb);
        int maxIdxX = 0;
        int maxIdxY = 0;
        float maxProjectedX = -1;
        float maxProjectedY = -1;
        int i = 0;
        while (i < 8)
        {
            float projX = 0;
            float projY = 0;
            Vector3 v = points[i];
            Vector3 v2 = this.GetComponent<Camera>().worldToCameraMatrix.MultiplyPoint(v);
            Vector3 v3 = this.GetComponent<Camera>().WorldToViewportPoint(v);
            points[i] = v2;
            if (this.GetComponent<Camera>().orthographic)
            {
                projX = Mathf.Abs(v2.x);
                projY = Mathf.Abs(v2.y);
            }
            else
            {
                projX = Mathf.Abs((v3.x * 2) - 1);
                projY = Mathf.Abs((v3.y * 2) - 1);
            }
            //Debug.Log("projected (" + projX + "," + projY + "," + (-1*v3.z) +")");
            if (projX > maxProjectedX)
            {
                maxProjectedX = projX;
                maxIdxX = i;
            }
            if (projY > maxProjectedY)
            {
                maxProjectedY = projY;
                maxIdxY = i;
            }
            i++;
        }
        //Debug.Log("max X: " + maxProjectedX + ", Y:" + maxProjectedY + ", Idx:" + maxIdxX + ", Idy:" + maxIdxY);
        if (this.GetComponent<Camera>().orthographic)
        {
            if ((maxProjectedX / this.GetComponent<Camera>().aspect) > maxProjectedY)
            {
                this.GetComponent<Camera>().orthographicSize = maxProjectedX / this.GetComponent<Camera>().aspect;
            }
            else
            {
                this.GetComponent<Camera>().orthographicSize = maxProjectedY;
            }
        }
        else
        {
            float d = 0;
            if (maxProjectedX > maxProjectedY)
            {
                d = (Mathf.Abs(points[maxIdxX].x) / (horizontalAperture / focal)) + points[maxIdxX].z;
            }
            else
            {
                d = (Mathf.Abs(points[maxIdxY].y) / (verticalAperture / focal)) + points[maxIdxY].z;
            }
            this.transform.Translate(0, 0, -d);
        }
    }

    // Sets the camera to focus on the object at the given screen location.
    // If there is no object at that location, the camera is not affected.
    // Note only objects with colliders will be found.
    private void focus(object[] args)
    {
        Vector2 position;
        position.x = Input.mousePosition.x;
        position.y = Input.mousePosition.y;
        if (args.Length > 0 && args[0].GetType() == typeof(Vector2))
        {
            position = (Vector2)args[0];
        }

        Ray ray = this.GetComponent<Camera>().ScreenPointToRay(position);
        RaycastHit hit = default(RaycastHit);
        if (Physics.Raycast(ray, out hit))
        {
            this.targetPoint = hit.point;
            this.transform.LookAt(this.targetPoint);
        }
    }

    private void nullTap(object[] args)
    {
    }

    // Calls the appropirate function for the current double tap gesture.
    // The count parameter indicates the number of fingers involved in the current double tap.
    private void doubleTap(int count)
    {
        if (count == 0)
        {
            NavigationAction tapAction = this.doubleTapMethodLookup[((int) this.doubleTap1Finger)];
            tapAction();
        }
        if (count == 1)
        {
            NavigationAction tapAction = this.doubleTapMethodLookup[((int) this.doubleTap2Finger)];
            tapAction();
        }
        else
        {
            if (count >= 2)
            {
                NavigationAction tapAction = this.doubleTapMethodLookup[((int) this.doubleTap3Finger)];
                tapAction();
            }
        }
    }

    public virtual void Update()
    {
        if (Input.GetKeyDown(this.frameKey))
        {
            NavigationAction frameAction = this.frameCamera;
            frameAction();
        }
    }

    public virtual void LateUpdate()
    {
        if (this.supressNavigation == true)
        {
            return;
        }
        float dx = 0;
        float dy = 0;
        int tCount = Input.touchCount;
        // Calculates when a double click has occured.
        if (Input.GetMouseButtonDown(0) && (tCount == 0))
        {
            this.touchSpot = Input.mousePosition;
            if ((Time.time - this.touchTimes[0]) < 0.3f)
            {
                this.doubleTap(0);
            }
            this.touchTimes[0] = Time.time;
        }
        // Calculates when a double tap of any number of fingers has occured.
        if (tCount > 0)
        {
            int t = tCount - 1;
            if ((Input.GetTouch(t).phase == TouchPhase.Began) && (t < this.touchTimes.Length))
            {
                float diff = Time.time - this.touchTimes[t];
                if (diff < 0.3f)
                {
                    this.doubleTap(t);
                }
                this.touchTimes[t] = Time.time;
            }
        }
        dx = Input.GetAxis("Mouse X");
        dy = Input.GetAxis("Mouse Y");
        if ((dx != 0f) || (dy != 0f))
        {
             // Checks if the mouse button is down and that there are no fingers touching the screen.
            if (Input.GetMouseButton(0) && (tCount == 0))
            {
                 // If the mouse cursor has not moved at least 'longPressRadius' pixels away from
                 // its inital clicked location, then noting happens.
                if (Vector2.Distance(Input.mousePosition, this.touchSpot) < this.longPressRadius)
                {
                    return;
                }
                this.touchSpot = new Vector2(-50, -50);
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        this.getNavigateFunction(this.look)(dx, dy);
                    }
                    else
                    {
                        this.getNavigateFunction(this.pan)(dx, dy);
                    }
                }
                else
                {
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        this.getNavigateFunction(this.dolly)(dx, dy);
                    }
                    else
                    {
                        this.getNavigateFunction(this.orbit)(dx, dy);
                    }
                }
            }
            else
            {
                // Right click
                if (Input.GetMouseButton(1))
                {
                    this.getNavigateFunction(this.look)(dx, dy);
                }
                else
                {
                    // Middle click
                    if (Input.GetMouseButton(2))
                    {
                        this.getNavigateFunction(this.pan)(dx, dy);
                    }
                }
            }
        }
        //if (Input.GetAxis("Mouse ScrollWheel")) 
        if (this.mouseWheelScrollAmount != 0)
        {
            float scrollAmount = Mathf.Log10(Mathf.Abs(this.mouseWheelScrollAmount) + 1);
            NavigationAction dollyAction = this.dolly;
            dollyAction(this.mouseWheelScrollAmount < 0 ? scrollAmount : -scrollAmount);
            this.mouseWheelScrollAmount = 0;
        }
        if (tCount != this.oldTouchCount)
        {
            int i = 0;
            while (i < Mathf.Min(this.initialInputs.Length, tCount))
            {
                this.initialInputs[i] = Input.GetTouch(i).position;
                i++;
            }
            this.oldTouchCount = tCount;
            this.clearTouchInput();
        }
        // Sets the touchSpot for one finger press on the screen.
        if ((tCount == 1) && (Input.GetTouch(0).phase == TouchPhase.Began))
        {
            this.touchSpot = Input.GetTouch(0).position;
        }
        if ((tCount > 0) && (Input.GetTouch(0).phase == TouchPhase.Moved))
        {
            if ((tCount == 1) && (Vector2.Distance(Input.mousePosition, this.touchSpot) < this.longPressRadius))
            {
                return;
            }
            else
            {
                if (tCount == 1)
                {
                    this.check1FingerNavigation();
                }
                else
                {
                    if (tCount == 2)
                    {
                        this.check2FingerNavigation();
                    }
                    else
                    {
                        if (tCount == 3)
                        {
                            this.check3FingerNavigation();
                        }
                        else
                        {
                            if (tCount > 3)
                            {
                                this.check4FingerNavigation();
                            }
                        }
                    }
                }
            }
            this.touchSpot = new Vector2(-50, -50);
        }
        if (this.firstUpdate < 3)
        {
            this.firstUpdate++;
            if (this.firstUpdate == 3)
            {
                NavigationAction frameAction = this.frameCamera;
                frameAction();
            }
        }
    }

    private void check1FingerNavigation()
    {
         // Currently we only support 1 finger dragging.
        if (Vector2.Distance(this.touchSpot, Input.GetTouch(0).position) < this.longPressRadius)
        {
            return;
        }
        this.touchSpot = new Vector2(-50, -50);
        Touch touch0 = Input.GetTouch(0);
        NavigationAction func = this.touchNavigationLookup[((int) this.drag1FingerNavigation)];
        if (func == (NavigationAction) this.dolly)
        {
            func(touch0.deltaPosition.y, 0);
        }
        else
        {
            func(touch0.deltaPosition.x, touch0.deltaPosition.y);
        }
    }

    // Performs the recognition and nagivation for 2 fingured inputs.
    private void check2FingerNavigation()
    {
        Vector2 delta0 = default(Vector2);
        Vector2 delta1 = default(Vector2);
        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);
        float diffTime = (touch0.deltaTime + touch1.deltaTime) * 0.5f;
        // If the user has not moved their fingers in 0.6 seconds we assume that we should 
        // attempt to recognise the gesture. This allows the user to keep their fingers 
        // on the screen and start a new gesture.
        if (diffTime > this.touchPauseTimeout)
        {
            this.clearTouchInput();
        }
        if (this.touchInputCount < this.touchInputDelay)
        {
            this.touchInputCount++;
            return;
        }
        else
        {
            if (this.touchInputCount == this.touchInputDelay)
            {
                this.touchInputCount++;
                delta0 = touch0.position - this.initialInputs[0];
                delta1 = touch1.position - this.initialInputs[1];
            }
            else
            {
                delta0 = touch0.deltaPosition;
                delta1 = touch1.deltaPosition;
            }
        }
        if ((this.touchNavigation == TouchNavigation.None) || (this.touchNavigation == TouchNavigation.Drag2Fingers))
        {
            float angleDifference = Vector2.Angle(delta0, delta1);
            // If the angle between the difference vectors is less than 60 then it is likely the two fingers
            // are moving in the same direction which indicates a drag.
            if (((Mathf.Abs(angleDifference) < 60) || (this.touchNavigation == TouchNavigation.Drag2Fingers)) || ((this.pinch2FingerNavigation == NavigationMode.None) && (this.rotate2FingerNavigation == NavigationMode.None)))
            {
                 // Dragging.
                 // While averaging should be just 0.5, often this results in movement that's much faster
                 // that what a normal drag with the mouse would be.
                Vector2 averageMove = (touch0.deltaPosition + touch1.deltaPosition) * 0.5f;
                //var averageMove:Vector2 = delta0 * 0.5;
                this.touchNavigation = TouchNavigation.Drag2Fingers;
                NavigationAction touchAction = this.touchNavigationLookup[((int) this.drag2FingerNavigation)];
                touchAction(averageMove.x, averageMove.y);
                return;
            }
        }
        // Makes use of the average vectors. These are used to actually calculate
        // the gesture being executed.
        Vector2 oldVectorAverage = (touch0.position - delta0) - (touch1.position - delta1);
        // These always use the most recent deltaPosition 
        Vector2 newVector = touch0.position - touch1.position;
        float newMagnitude = newVector.magnitude;
        Vector2 oldVector = (touch0.position - touch0.deltaPosition) - (touch1.position - touch1.deltaPosition);
        float diffAverage = newMagnitude - oldVectorAverage.magnitude;
        Vector3 curlAverage = Vector3.Cross(oldVectorAverage.normalized, newVector.normalized);
        // Find the ratio between the curl of the vectors and the difference in
        // maginutde. When this is over 1000 this usually indicates that the difference
        // is greater which indicates a pinch, otherwise it indicates that the curl is
        // greater and that the user is rotating.
        float ratioAverage = Mathf.Abs(diffAverage / curlAverage.z);
        if ((this.touchNavigation == TouchNavigation.None) || (this.touchNavigation == TouchNavigation.Rotating))
        {
            if (((ratioAverage < 1000) || (this.touchNavigation == TouchNavigation.Rotating)) || (this.pinch2FingerNavigation == NavigationMode.None))
            {
                Vector3 curl = Vector3.Cross(oldVector.normalized, newVector.normalized);
                this.touchNavigation = TouchNavigation.Rotating;
                NavigationAction touchAction = this.touchNavigationLookup[((int) this.rotate2FingerNavigation)];
                touchAction(curl.z * 400, 0);
            }
        }
        if ((this.touchNavigation == TouchNavigation.None) || (this.touchNavigation == TouchNavigation.Pinching))
        {
            if ((((ratioAverage >= 1000) && (Mathf.Abs(diffAverage) > 20)) || (this.touchNavigation == TouchNavigation.Pinching)) || (this.rotate2FingerNavigation == NavigationMode.None))
            {
                float diff = newMagnitude - oldVector.magnitude;
                this.touchNavigation = TouchNavigation.Pinching;
                float diffLog = 0;
                if (Mathf.Abs(diff) > 1)
                {
                    diffLog = Mathf.Log(Mathf.Abs(diff)) / 1.25f;
                }
                float d = 0.1f + diffLog;
                NavigationAction touchAction = this.touchNavigationLookup[((int) this.pinch2FingerNavigation)];
                touchAction(diff > 0 ? d : -d, 0);
            }
        }
    }

    private void check3FingerNavigation()
    {
         // Currently we only support 3 finger dragging.
        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);
        Touch touch2 = Input.GetTouch(2);
        Vector2 average = ((touch0.deltaPosition + touch1.deltaPosition) + touch2.deltaPosition) / 3;
        NavigationAction func = this.touchNavigationLookup[((int) this.drag3FingerNavigation)];
        if (func == (NavigationAction) this.dolly)
        {
            func(average.y, 0);
        }
        else
        {
            func(average.x, average.y);
        }
    }

    private void check4FingerNavigation()
    {
         // Currently we only support 4 finger dragging.
        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);
        Touch touch2 = Input.GetTouch(2);
        Touch touch3 = Input.GetTouch(3);
        Vector2 average = (((touch0.deltaPosition + touch1.deltaPosition) + touch2.deltaPosition) + touch3.deltaPosition) / 4;
        NavigationAction func = this.touchNavigationLookup[((int) this.drag4FingerNavigation)];
        if (func == (NavigationAction) this.dolly)
        {
            func(average.y, 0);
        }
        else
        {
            func(average.x, average.y);
        }
    }

    public virtual void Start()
    {
        this.initialInputs = new Vector2[16];
        this.touchTimes = new float[16];
        // Make the rigid body not change rotation
        if (this.GetComponent<Rigidbody>())
        {
            this.GetComponent<Rigidbody>().freezeRotation = true;
        }
        this.targetPoint = new Vector3();
        this.transform.LookAt(this.targetPoint);
    }

    public CameraNavigation()
    {
        this.axes = UseAxes.MouseXAndY;
        this.defaultMouseNavigation = NavigationMode.Orbit;
        this.drag1FingerNavigation = NavigationMode.Orbit;
        this.drag2FingerNavigation = NavigationMode.Pan;
        this.drag3FingerNavigation = NavigationMode.Look;
        this.drag4FingerNavigation = NavigationMode.None;
        this.rotate2FingerNavigation = NavigationMode.Look;
        this.pinch2FingerNavigation = NavigationMode.Dolly;
        this.doubleTap1Finger = DoubleTabMethod.Focus;
        this.doubleTap2Finger = DoubleTabMethod.None;
        this.doubleTap3Finger = DoubleTabMethod.Frame;
        this.frameKey = KeyCode.F;
        this.frameOnStartup = true;
        this.upperRotateLimit = 80;
        this.lowerRotateLimit = 280;
        this.orbitSpeed = 1f;
        this.lookSpeed = 1f;
        this.panSpeed = 1f;
        this.dollySpeed = 0.1f;
        this.touchPauseTimeout = 0.6f;
        this.longPressRadius = 40;
        this.touchNavigation = TouchNavigation.None;
        this.touchInputDelay = 2;
        this.navigationLookup = new NavigationAction[][] {new NavigationAction[] {this.orbit, this.pan, this.look, this.dolly}, new NavigationAction[] {this.pan, this.orbit, this.look, this.dolly}, new NavigationAction[] {this.look, this.pan, this.orbit, this.dolly}, new NavigationAction[] {this.dolly, this.pan, this.look, this.orbit}};
        this.touchNavigationLookup = new NavigationAction[] {this.nullNav, this.orbit, this.pan, this.look, this.dolly};
        this.doubleTapMethodLookup = new NavigationAction[] {this.nullTap, this.focus, this.frameCamera};
        this.touchSpot = new Vector2(-50, -50);
        this.sceneBoundingBoxDirty = true;
    }

}