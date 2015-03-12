using UnityEngine;

public class TunnelGameManager : MonoBehaviour {

    private static TunnelGameManager mInst = null;

    public static TunnelGameManager Inst{ 
        get
        {
            if (mInst == null)
                mInst = (new GameObject("TunnelGameManager")).AddComponent(typeof(TunnelGameManager)) as TunnelGameManager;
            return mInst;
        }
    }

    // LSL variables
    public bool sendLSLData = true;

    public void Touch() { }

	void Start () {
        RenderSettings.ambientLight = Color.black;
        GameManager.Inst.LocalPlayer.playerController.navMode = PlayerController.NavMode.physics;

#if UNITY_WEBPLAYER
        sendLSLData = false;
#endif

        if (sendLSLData)
            gameObject.AddComponent<LSLSender>();
	}

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Alpha2) || Input.GetKeyUp(KeyCode.Equals))
            GameManager.Inst.LoadLevel(GameManager.Level.SCALE_GAME);
    }

    void OnDestroy()
    {
        mInst = null;
    }
}
