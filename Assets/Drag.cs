using UnityEngine;
using System.Collections;
using com.migenius.rs4.unity;
using com.migenius.rs4.core;
using com.migenius.rs4.math;
using com.migenius.rs4.viewport;

public class Drag : MonoBehaviour {
	private Vector3 screenPoint;
	private Vector3 offset;
	private CameraNavigation nav;
	private UnityViewport Viewport;
	private bool moved;
	private Vector3 OldMovement;
	public string RealityServerObject;
	private Vector3 NewPosition;

	void Start () {
		GameObject cam = GameObject.Find ("Main Camera");
		if (cam != null) {
			nav = cam.GetComponent<CameraNavigation> ();
			if (nav == null) {
				Debug.Log ("- object drag requires CameraNavigation");
				this.enabled = false;
				return;
			}
		} else {
			Debug.Log ("- object drag requires Main Camera");
			this.enabled = false;
			return;
		}

		GameObject rs = GameObject.Find ("RealityServer");
		if (rs != null) {
			Viewport = rs.GetComponent<UnityViewport> ();
			if (Viewport == null) {
				Debug.Log ("- object drag component needs a UnityViewport component to work with.");
				this.enabled = false;
				return;
			}
			if (Viewport.enabled == false) {
				Debug.Log ("- object drag component needs a UnityViewport that is enabled to work with.");
				this.enabled = false;
				return;
			}
		} else {
			Debug.Log ("- object drag requires RealityServer");
			this.enabled = false;
			return;
		}
		moved = false;

		Viewport.OnAppIniting += new ApplicationInitialisingCallback(OnAppIniting);
	}
	
	protected void UpdateMovement(IAddCommand seq)
	{
		//Debug.Log ("- move in RS " + RealityServerObject + " " + NewPosition) ;

		Matrix3D m = new Matrix3D ();
		m.SetTranslation(-NewPosition.x,-NewPosition.y,-NewPosition.z);

		seq.AddCommand(new RSCommand("instance_set_world_to_obj",
		                             "instance_name", RealityServerObject,
		                             "transform", m.GetMatrixForRS()
		                             ));
		if (Viewport.Viewport.RenderLoopRunning)
		{
			Viewport.Viewport.RestartLoop();
		}
		moved = false;
	}

	void Update () 
	{
		if (Viewport != null && Viewport.Scene.Ready && moved)
		{
			Viewport.Service.AddCallback(UpdateMovement);
		}
	}

	void OnMouseDown()
	{
		screenPoint = Camera.main.WorldToScreenPoint (gameObject.transform.position);
		
		offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint (new Vector3 (Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));

		nav.supressNavigation = true;
		OldMovement = Input.mousePosition;
	}

	void OnMouseUp()
	{

		nav.supressNavigation = false;
	}

	void OnMouseDrag()
	{
		if (OldMovement != Input.mousePosition)
		{
			Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);

			Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;

			transform.position = curPosition;

			moved = true;

			OldMovement = Input.mousePosition;

			NewPosition = transform.position;
		}

	}

	protected void OnAppIniting(RSCommandSequence seq)
	{
		if (moved == true)
		{
			UpdateMovement(seq);
		}
	}
}