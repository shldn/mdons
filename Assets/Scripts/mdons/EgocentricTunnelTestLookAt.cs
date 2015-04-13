using UnityEngine;

public class EgocentricTunnelTestLookAt : TunnelArrowClickChoice {

    public BezierSpline tunnel = null;

	void Start () {
        ReCompute();
    }

    override public void ReCompute()
    {
        transform.LookAt(tunnel.GetPoint(0.0f));
    }
}
