using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SetPositionRelativeToSplineEnd : MonoBehaviour {

    private static HashSet<SetPositionRelativeToSplineEnd> all = new HashSet<SetPositionRelativeToSplineEnd>();
    public BezierSpline spline = null;
    public Vector3 offset = Vector3.forward;
    public bool setOrientationToSpineEnd = true;

	void Awake () {
        all.Add(this);
        SetPosition();
	
	}

    void OnDestroy()
    {
        all.Remove(this);
    }

    public void SetPosition()
    {
        if (spline == null)
            return;
        Quaternion orient = Quaternion.LookRotation(spline.GetDirection(1.0f).normalized, Vector3.up);
        Vector3 newPos = spline.GetPoint(1.0f);
        newPos += offset.x * (Quaternion.AngleAxis(90.0f, Vector3.up) * orient * Vector3.forward);
        newPos += offset.y * Vector3.up;
        newPos += offset.z * (orient * Vector3.forward);
        transform.position = newPos;

        if (setOrientationToSpineEnd)
            transform.rotation = orient;
    }

    public static void ResetAll()
    {
        foreach(SetPositionRelativeToSplineEnd elem in all)
            elem.SetPosition();
    }
}
