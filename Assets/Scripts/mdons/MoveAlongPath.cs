using UnityEngine;
using System.Collections;

public class MoveAlongPath : MonoBehaviour {

    public BezierSpline path = null;
    float t = 0.0f;
    float step = 0.05f;
	
	void Update () {
        if (Input.GetKeyUp(KeyCode.N) && path != null)
        {
            GameManager.Inst.LocalPlayer.playerController.navMode = PlayerController.NavMode.navmesh;
            GameManager.Inst.LocalPlayer.playerController.SetNavDestination(path.GetPoint(t += step), path.GetDirection(t)); 
        }
	}
}
