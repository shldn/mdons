using UnityEngine;

// Assumes 
//  - initial spline direction is the global frame of reference
//  - end spline direction is the end local frame of reference
//  - first point of spline is the lookAt position.
public class AllocentricTunnelTestLookAt : TunnelArrowClickChoice{

    public BezierSpline tunnel = null;
    public Transform overridelookAtObject = null;

	void Start () {
        if( overridelookAtObject != null )
            transform.LookAt(overridelookAtObject);
        else
            transform.LookAt(tunnel.GetPoint(0.0f));

        // adjust for local frame of reference
        transform.rotation = Quaternion.FromToRotation(tunnel.GetDirection(0.0f), tunnel.GetDirection(1.0f)) * transform.rotation;
        
	}	
}
