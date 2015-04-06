using UnityEngine;
using System.Collections;

public class SetPositionRelativeToSplineEnd : MonoBehaviour {

    public BezierSpline spline = null;
    public Vector3 offset = Vector3.forward;
    public bool setOrientationToSpineEnd = true;

	void Awake () {
        if (spline == null)
            return;
        Quaternion orient = Quaternion.LookRotation(spline.GetDirection(1.0f).normalized,Vector3.up);
        Vector3 newPos = spline.GetPoint(1.0f);
        newPos += offset.x * (Quaternion.AngleAxis(90.0f, Vector3.up) * orient * Vector3.forward);
        newPos += offset.y * (Quaternion.AngleAxis(-90.0f, Vector3.right) * orient * Vector3.forward);
        newPos += offset.z * (orient * Vector3.forward);
        transform.position = newPos;

        if (setOrientationToSpineEnd)
            transform.rotation = orient;
	
	}
}
