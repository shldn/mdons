using UnityEngine;
using System.Collections;

public class MoveAlongPath : MonoBehaviour {

    public BezierSpline path = null;
    float t = 0.0f;
    float step = 0.05f;
    private bool automatic = false;
	
	void Update () {
        if (Input.GetKeyUp(KeyCode.N) && path != null)
        {
            GameManager.Inst.LocalPlayer.playerController.navMode = PlayerController.NavMode.navmesh;
            GameManager.Inst.LocalPlayer.playerController.SetNavDestination(path.GetPoint(t += step), path.GetDirection(t)); 
        }
        if( automatic && path != null )
        {
            if( GameManager.Inst.LocalPlayer.playerController.NavAgent.remainingDistance < 4.0f)
            {
                GameManager.Inst.LocalPlayer.playerController.navMode = PlayerController.NavMode.navmesh;
                GameManager.Inst.LocalPlayer.playerController.SetNavDestination(path.GetPoint(t += step), path.GetDirection(t));
            }
        }
	}

    public void Reset()
    {
        t = 0.0f;
        automatic = false;
        GameManager.Inst.LocalPlayer.playerController.NavAgent.Stop();
    }

    public void AutoStart()
    {
        automatic = true;
        GameManager.Inst.LocalPlayer.playerController.navMode = PlayerController.NavMode.navmesh;
        GameManager.Inst.LocalPlayer.playerController.SetNavDestination(path.GetPoint(t += step), path.GetDirection(t));

    }

}
