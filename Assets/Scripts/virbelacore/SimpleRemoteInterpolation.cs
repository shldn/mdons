using UnityEngine;
using System.Collections;

public class SimpleRemoteInterpolation : MonoBehaviour {

	// Extremely simple and dumb interpolation script
	private Vector3 desiredPos;
	private Quaternion desiredRot;
	
	private float dampingFactor = 5f;
	
	void Awake() {
		desiredPos = this.transform.position;
		desiredRot = this.transform.rotation;
	}
	
	public void SetTransform(Vector3 pos, Quaternion rot, bool interpolate) {

		desiredPos = pos;
		desiredRot = rot;
		if (!interpolate) {
			this.transform.position = pos;
			this.transform.rotation = rot;
		}
	}
	
	void Update () {
		// Really dumb interpolation, but works for this example
		this.transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * dampingFactor);
		this.transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Time.deltaTime * dampingFactor);
	}
}
