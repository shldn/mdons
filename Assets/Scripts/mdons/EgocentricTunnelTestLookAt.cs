using UnityEngine;

public class EgocentricTunnelTestLookAt : MonoBehaviour {

    public BezierSpline tunnel = null;

	void Start () {
	    transform.LookAt(tunnel.GetPoint(0.0f));
	}
}
