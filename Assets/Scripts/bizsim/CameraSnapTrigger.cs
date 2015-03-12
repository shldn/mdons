using UnityEngine;
using System.Collections;
[RequireComponent (typeof(BoxCollider))]
public class CameraSnapTrigger : MonoBehaviour {
	
	public Camera cameraObject;
	private BoxCollider bCollider;

	void Start()
	{
		bCollider = GetComponent<BoxCollider>();
		bCollider.isTrigger = true;
	}

	void OnTriggerEnter( Collider other)
	{

        if (GameManager.Inst.LocalPlayer != null && other.gameObject == GameManager.Inst.LocalPlayer.gameObject)
    		SnapCam(cameraObject);
	}
	
	private void SnapCam(Camera cam)
	{
		MainCameraController.Inst.cameraType = CameraType.SNAPCAM;
		
		if (cam != null)
		{
			Camera.main.transform.position = cam.transform.position;
			Camera.main.transform.rotation = cam.transform.rotation;
		}
	}
	
}