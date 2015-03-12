using UnityEngine;
using System.Collections;

public class CameraSpinRig : MonoBehaviour {

    float panAngle = 45f;
    float panTime = 120f;
    float tiltAngle = 5f;


    private Quaternion initialRotation = Quaternion.identity;

    void Start(){
        initialRotation = transform.rotation;
    } // End of Start().

	// Update is called once per frame
	void Update(){

        float panRunner = Mathf.Sin((Time.realtimeSinceStartup * 2f * Mathf.PI) / panTime);

        // Horizontal rotation
        transform.rotation = initialRotation * Quaternion.AngleAxis(((panRunner * 0.5f) + 0.5f) * -panAngle * 0.5f, Vector3.up);
        transform.rotation *= Quaternion.AngleAxis(tiltAngle - (((panRunner * 0.5f) + 0.5f) * tiltAngle), Vector3.right);
	} // End of Update().
} // End of CameraSpinRig.
