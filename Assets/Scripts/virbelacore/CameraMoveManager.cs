using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Class to handle cinematic camera moves.
public class CameraMoveManager : MonoBehaviour {

    Camera cam;
    GameObject target;
    Vector3 targetOffset;
    int prevNativeGUIVisibilityFlags = 0;
    static private bool enabled = false;

    private static CameraMoveManager mInstance;
    public static CameraMoveManager Inst
    {
        get
        {
            if (mInstance == null)
                mInstance = (new GameObject("CameraMoveManager")).AddComponent(typeof(CameraMoveManager)) as CameraMoveManager;
            return mInstance;
        }
    }
    void Awake()
    {
        GameObject go = GameObject.Instantiate(Resources.Load("Cameras/Screenshot Cam", typeof(GameObject))) as GameObject;
        cam = go.GetComponent<Camera>();
    }

	void Update () {
        if (Input.GetKeyUp(KeyCode.Return) && cam != null && cam.enabled)
            GameGUI.Inst.Visible = true;
	}

    void OnDestroy()
    {
        cam = null;
        mInstance = null;
        enabled = false;
    }

    public void SetCameraTarget(GameObject targetGO, bool followPos)
    {
        Enabled = true;
        cam.GetComponent<MouseOrbitZoom>().SetTarget(targetGO);
        cam.GetComponent<MouseOrbitZoom>().FollowPosition = followPos;
    }

    public static bool Enabled
    {
        get { return enabled; }
        set
        {
            enabled = value;
            Inst.cam.enabled = enabled;
            Camera.main.depth = enabled ? -2 : -1;
            Camera billboardCam = enabled ? Inst.cam : Camera.main;
            if (enabled)
                GameGUI.Inst.Visible = false;
            if (GameManager.Inst.LevelLoaded == GameManager.Level.BIZSIM)
                BizSimManager.Inst.EnableInputOnAllBrowserPlanes(!enabled);

            foreach (KeyValuePair<int, Player> playerPair in GameManager.Inst.playerManager)
                playerPair.Value.SetNameBillboardCam(billboardCam);
            if( ReplayManager.Initialized )
                foreach (KeyValuePair<int, Player> playerPair in ReplayManager.Inst.replayPlayers)
                    playerPair.Value.SetNameBillboardCam(billboardCam);
        }
    }
    public Camera Cam { get { return cam; } }
}
