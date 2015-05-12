using UnityEngine;
using System.Collections.Generic;

public enum UserControl
{
    NONE,
    PARTIAL,
    FULL,
}

public enum TunnelEvent
{
    TUNNEL_ENTRANCE = 100,
    TURN_START = 200,
    TURN_END = 300,
    TUNNEL_EXIT = 400,
    DECISION = 500,
    TRIAL_DONE = 600,
}

public enum TunnelChoice
{
    ALLOCENTRIC = 1,
    EGOCENTRIC = 2,
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
    bool chooseArrow = true;
    bool useMouseButtonsToChoose = true;
    List<Experiment> experiments = new List<Experiment>();

    public bool UseRotatableArrow { get { return !chooseArrow; } }
    public bool UseMouseButtonsToChoose { get { return useMouseButtonsToChoose; } }
    bool AutoMovePlayer { get { return !allowUserControls; } }

    // LSL variables
    public bool sendLSLData = true;
    public int lastCode = 0;
    public int lastChoice = 0;
    LSLSender lslSender = null;

    public void Touch() { }

	void Start () {
        RenderSettings.ambientLight = Color.black;
        GameManager.Inst.LocalPlayer.playerController.navMode = PlayerController.NavMode.physics;

#if UNITY_WEBPLAYER
        sendLSLData = false;
#endif

        if (sendLSLData)
            lslSender = gameObject.AddComponent<LSLSender>();

        // Read Config file if it exists
        experiments = TunnelConfigReader.Read("config.txt");
	}

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Alpha2) || Input.GetKeyUp(KeyCode.Equals))
            GameManager.Inst.LoadLevel(GameManager.Level.SCALE_GAME);
        if (Input.GetKeyUp(KeyCode.Alpha3))
            GameManager.Inst.LoadLevel(GameManager.Level.MDONS_TEST);

        if (Input.GetKeyUp(KeyCode.Space))
            StartExperiment(expCount);
        if (Debug.isDebugBuild && Input.GetKeyUp(KeyCode.T))
            TunnelEnvironmentManager.Inst.ReCompute();
        if(Debug.isDebugBuild && Input.GetKeyUp(KeyCode.P))
        {
            GameGUI.Inst.customizeAvatarGui.ChangeCharacter((GameManager.Inst.LocalPlayer.ModelIdx + 1) % PlayerManager.PlayerModelNames.Length);
            GameManager.Inst.LocalPlayer.playerController.enabled = true;
        }
    }

    void OnDestroy()
    {
        mInst = null;
    }

    
    //// GUI
    //void OnGUI()
    //{
    //    int buttonWidth = 200;
    //    if (GUI.Button(new Rect(Screen.width - buttonWidth - 30, 20, buttonWidth, 30), "Start Next Experiment"))
    //        StartExperiment(expCount);
    //    string experimentDesc = "Code: " + lastCode;
    //    GUI.skin.label.fontSize = 14;
    //    GUI.Label(new Rect(10,10, 300,100), experimentDesc);
    //}

    void StartExperiment(int idx)
    {
        if( experiments.Count > 0 )
        {
            StartExperiment(experiments[idx % experiments.Count]);
        }
        else
        {
            bool[] playerVis = { true, false, false, true };
            float[] tunnelAngle = { -45f, 30f, -15f, 45f, -30f, 15f };
            UserControl[] userControl = { UserControl.NONE, UserControl.PARTIAL, UserControl.NONE, UserControl.PARTIAL };
            StartExperiment(tunnelAngle[idx % tunnelAngle.Length], playerVis[idx % playerVis.Length], userControl[idx % userControl.Length]);
        }
    }

    void StartExperiment(Experiment exp)
    {
        chooseArrow = exp.chooseArrow;
        useMouseButtonsToChoose = !exp.mouseClickToChoose;
        StartExperiment(exp.angle, exp.avatarVisible, exp.userControl);
    }

    void StartExperiment(float tunnelAngle, bool playerVis, UserControl uControl)
    {
        if (expCount != 0)
            TunnelGameManager.Inst.RegisterEvent(TunnelEvent.TRIAL_DONE);
        Debug.LogError("Starting experiment: " + expCount);
        lastChoice = 0;
        GameManager.Inst.LocalPlayer.Visible = playerVis;
        TunnelEnvironmentManager.Inst.SetTunnelAngle(tunnelAngle);
        MoveAlongPath moveScript = (MoveAlongPath)FindObjectOfType(typeof(MoveAlongPath));
        GameManager.Inst.playerManager.SetLocalPlayerTransform(GameManager.Inst.playerManager.GetLocalSpawnTransform());
        GameManager.Inst.LocalPlayer.playerController.StopMomentum();
        moveScript.Reset();
        TunnelEnvironmentManager.Inst.Reset();

        switch(uControl)
        {
            case UserControl.NONE:
                allowUserControls = false;
                moveScript.AutoStart();
                GameManager.Inst.playerManager.playerInputMgr.disableKeyPressMovement = true;
                break;
            case UserControl.PARTIAL:
                allowUserControls = true;
                GameManager.Inst.playerManager.playerInputMgr.disableKeyPressMovement = true;
                break;
            default:
                Debug.LogError("UserControl: " + uControl + " not yet handled");
                break;
        }

        ++expCount;
        RegisterEvent(TunnelEvent.TUNNEL_ENTRANCE);
    }

    public int GetCurrentCodeBase()
    {
        float tunnelAngle = TunnelEnvironmentManager.Inst.GetTunnelAngle();

        int code = 2000;
        code += (int)Mathf.Abs(tunnelAngle);
        code += GameManager.Inst.LocalPlayer.Visible ? 0 : 1000;
        code += tunnelAngle > 0 ? 0 : 1000;

        return code;
    }

    public void RegisterEvent(TunnelEvent tEvent)
    {
        lastCode = GetCurrentCodeBase() + (int)tEvent;
        if (lslSender != null)
            lslSender.SendCode(lastCode);
    }

    public void RegisterChoice(TunnelChoice choice)
    {
        lastChoice = (int)choice;
        if (lslSender != null)
            lslSender.SendChoice((int)choice);
    }

}
