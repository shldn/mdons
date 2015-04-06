using UnityEngine;

public class EgocentricTunnelTestLookAt : TunnelArrowClickChoice {

    public BezierSpline tunnel = null;

	void Start () {
	    transform.LookAt(tunnel.GetPoint(0.0f));
	}
}
