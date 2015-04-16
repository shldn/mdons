﻿using UnityEngine;
using System.Collections;

// This component attaches to a spline assumed to have 4 points and turns it into a tunnel at the specified angle.
public class SplineToTunnelTurn : MonoBehaviour {

    public float angle = 45.0f;
    public float curveCuspDist = 40f;
    public float percentStraight = 0.667f; // each side will have this percentage straight
    public Vector3 startDir = -Vector3.right;
    public BezierSpline spline = null;

	void Awake () {
        if (spline == null)
            spline = GetComponent<BezierSpline>();

        if( angle >= 65.0f )
            Debug.LogError("SplineToTunnelTurn: Please consider an angle less than 65 degrees, the tunnel starts turning in on itself at this point.");

	    if( spline.ControlPointCount != 10 )
        {
            Debug.LogError("SplineToTunnelTurn: spline has " + spline.ControlPointCount + " needs 10 points -- bailing...");
            return;
        }

        SetSplineToAngle();

	}

    void SetSplineToAngle()
    {
        // start section

        // main points
        spline.SetControlPoint(1, spline.GetControlPoint(0) + 0.5f * percentStraight * curveCuspDist * startDir);
        spline.SetControlPoint(3, spline.GetControlPoint(0) + percentStraight * curveCuspDist * startDir);

        // handles
        spline.SetControlPoint(2, spline.GetControlPoint(1));        
        spline.SetControlPoint(4, spline.GetControlPoint(0) + curveCuspDist * startDir);


        // end section

        // forming an isosolese triangle, so 2 times dist of right triangle
        float endPtDist = 2.0f * curveCuspDist * Mathf.Cos(angle * Mathf.Deg2Rad);
        Vector3 endPoint = spline.GetControlPoint(0) + endPtDist * (Quaternion.AngleAxis(angle, Vector3.up) * startDir);
        Vector3 endToCuspV = spline.GetControlPoint(4) - endPoint;

        // main points
        spline.SetControlPoint(9, endPoint);
        spline.SetControlPoint(6, endPoint + percentStraight * endToCuspV);

        // handles
        spline.SetControlPoint(8, endPoint + 0.5f * percentStraight * endToCuspV);
        spline.SetControlPoint(7, spline.GetControlPoint(8));
        spline.SetControlPoint(5, spline.GetControlPoint(4));


    }

    public void ReCompute()
    {
        SetSplineToAngle();
    }

}
