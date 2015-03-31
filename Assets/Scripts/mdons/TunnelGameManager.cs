using UnityEngine;

public enum UserControl
{
    NONE,
    PARTIAL,
}

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

    // Experiment variables
    int expCount = 0;
    bool allowUserControls = true;
    bool AutoMovePlayer { get { return !allowUserControls; } }

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

        if (Debug.isDebugBuild && Input.GetKeyUp(KeyCode.E))
            StartExperiment(expCount);
    }

    void OnDestroy()
    {
        mInst = null;
    }

    
    // GUI
    void OnGUI()
    {
        int buttonWidth = 200;
        if (GUI.Button(new Rect(Screen.width / 2 - buttonWidth / 2, 20, buttonWidth, 30), "Start Next Experiment"))
            StartExperiment(expCount);
        string experimentDesc = "User Control: " + allowUserControls.ToString();
        GUI.Label(new Rect(10,10, 300,100), experimentDesc);
    }

    void StartExperiment(int idx)
    {
        bool[] playerVis = { true, false, false, true };
        UserControl[] userControl = { UserControl.NONE, UserControl.PARTIAL, UserControl.PARTIAL, UserControl.NONE };
        StartExperiment(playerVis[idx % playerVis.Length], userControl[idx % userControl.Length]);
    }

    void StartExperiment(bool playerVis, UserControl uControl)
    {
        Debug.LogError("Starting experiment: " + expCount);
        GameManager.Inst.LocalPlayer.Visible = playerVis;

        MoveAlongPath moveScript = (MoveAlongPath)FindObjectOfType(typeof(MoveAlongPath));
        GameManager.Inst.playerManager.SetLocalPlayerTransform(GameManager.Inst.playerManager.GetLocalSpawnTransform());
        moveScript.Reset();
        TunnelEnvironmentManager.Inst.Reset();

        switch(uControl)
        {
            case UserControl.NONE:
                allowUserControls = false;
                moveScript.AutoStart();
                break;
            case UserControl.PARTIAL:
                allowUserControls = true;
                break;
            default:
                Debug.LogError("UserControl: " + uControl + " not yet handled");
                break;
        }

        ++expCount;
    }

}
