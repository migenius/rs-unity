using UnityEngine;
using System.Collections;
using System.Reflection;
using com.migenius.rs4.unity;
using com.migenius.rs4.core;
using com.migenius.rs4.math;
using com.migenius.rs4.viewport;
using Logger = com.migenius.rs4.core.Logger;

namespace com.migenius.unity {
	public class Drag : MonoBehaviour {
		private Vector3 ObjectInitialPosition;
		private Vector3 Delta;
		private Component NavigationScript;
		private UnityViewport Viewport;
		private bool Moved;
		private Vector3 LastMousePosition;
		private Vector3 NewPosition;

		public string RealityServerObject;

		void Start () {
			GameObject rs = GameObject.Find ("RealityServer");
			if (rs != null) {
				Viewport = rs.GetComponent<UnityViewport> ();
				if (Viewport == null) {
					Logger.Log("error", "Object drag component needs a UnityViewport component to work with.");
					this.enabled = false;
					return;
				}
				if (Viewport.enabled == false) {
					Logger.Log("error", "Object drag component needs a UnityViewport that is enabled to work with.");
					this.enabled = false;
					return;
				}
			} else {
				Logger.Log("error", "Object drag requires RealityServer");
				this.enabled = false;
				return;
			}

			Camera cam = Viewport.RenderCamera;
			if (cam != null) {
				NavigationScript = cam.GetComponent("CameraNavigation");
				if (NavigationScript == null) {
					Logger.Log("error", "Object drag requires CameraNavigation");
					this.enabled = false;
					return;
				}
			} else {
				Logger.Log("error", "Object drag requires Main Camera");
				this.enabled = false;
				return;
			}
			

			Moved = false;

			Viewport.OnAppIniting += new ApplicationInitialisingCallback(OnAppIniting);
		}
		
		protected void UpdateMovement(IAddCommand seq)
		{
			Matrix3D m = new Matrix3D ();
			// NB: X is not negated to deal with handedness difference between RS and Unity
			m.SetTranslation(NewPosition.x,-NewPosition.y,-NewPosition.z);

			seq.AddCommand(new RSCommand("instance_set_world_to_obj",
			                             "instance_name", RealityServerObject,
			                             "transform", m.GetMatrixForRS()
			                             ));
			if (Viewport.Viewport.RenderLoopRunning)
			{
				Viewport.Viewport.RestartLoop();
			}
			Moved = false;
		}

		void Update () 
		{
			if (Viewport != null && Viewport.Scene.Ready && Moved)
			{
				Viewport.Service.AddCallback(UpdateMovement);
			}
		}

		void OnMouseDown()
		{
			ObjectInitialPosition = Camera.main.WorldToScreenPoint (gameObject.transform.position);
			
			Delta = gameObject.transform.position - Camera.main.ScreenToWorldPoint (new Vector3 (Input.mousePosition.x, Input.mousePosition.y, ObjectInitialPosition.z));

            FieldInfo supress = NavigationScript.GetType().GetField("supressNavigation");
            supress.SetValue(NavigationScript, true);
			LastMousePosition = Input.mousePosition;
		}

		void OnMouseUp()
		{
            FieldInfo supress = NavigationScript.GetType().GetField("supressNavigation");
            supress.SetValue(NavigationScript, false);
        }

        void OnMouseDrag()
		{
			if (LastMousePosition != Input.mousePosition)
			{
				Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, ObjectInitialPosition.z);

				Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + Delta;

				transform.position = curPosition;

				Moved = true;

				LastMousePosition = Input.mousePosition;

				NewPosition = transform.position;
			}

		}

		protected void OnAppIniting(RSCommandSequence seq)
		{
			if (Moved == true)
			{
				UpdateMovement(seq);
			}
		}
	}
}
