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
    DECISION_START = 500,
    DECISION = 600,
    TRIAL_DONE = 700,
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

    // Trial variables
    float delayAfterChoiceToStartNext = 2f;
    bool nextExperimentInQueue = false;


    // Experiment variables
    int expCount = 0;
    bool allowUserControls = true;
    bool chooseArrow = true;
    bool useMouseButtonsToChoose = true;
    bool showAbstractPlayer = false;
    List<Experiment> experiments = new List<Experiment>();

    public bool UseRotatableArrow { get { return !chooseArrow; } }
    public bool UseMouseButtonsToChoose { get { return useMouseButtonsToChoose; } }
    public bool UseKeysToChoose { get { return useMouseButtonsToChoose; } }
    bool AutoMovePlayer { get { return !allowUserControls; } }

    // LSL variables
    public bool sendLSLData = true;
    public int lastCode = 0;
    public int lastChoice = 0;
    LSLSender lslSender = null;

    // Abstract player
    Texture abstractTexture;

    // GUI
    bool showStartScreen = true;

    public void Touch() { }

	void Start () {
        RenderSettings.ambientLight = Color.black;
        GameManager.Inst.LocalPlayer.playerController.navMode = PlayerController.NavMode.locked;
        GameManager.Inst.LocalPlayer.Visible = false;

#if UNITY_WEBPLAYER
        sendLSLData = false;
#endif

        if (sendLSLData)
            lslSender = gameObject.AddComponent<LSLSender>();

        // Read Config file if it exists
        experiments = TunnelConfigReader.Read("config.txt");

        abstractTexture = (Texture2D)Resources.Load("Textures/abstractPlayer");
	}

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Alpha2) || Input.GetKeyUp(KeyCode.Equals))
            GameManager.Inst.LoadLevel(GameManager.Level.SCALE_GAME);
        if (Input.GetKeyUp(KeyCode.Alpha3))
            GameManager.Inst.LoadLevel(GameManager.Level.MDONS_TEST);

        if (Input.GetKeyUp(KeyCode.Space))
            StartNextExperiment();
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


    // GUI
    void OnGUI()
    {
        if(showAbstractPlayer)
        {
            float width = 0.15f * Screen.width;
            float height = 0.4f * Screen.height;
            float left = 0.5f * (Screen.width - width);
            float top = Screen.height - height;
            GUI.DrawTexture(new Rect(left,top, width, height), abstractTexture);
        }

        // Show Start/End Button
        if (expCount == 0)
        {
            GUI.skin = TunnelEnvironmentManager.Inst.guiSkin;
            float btnPercent = 0.5f;
            float btnWidth = btnPercent * Screen.width;
            float btnHeight = btnPercent * Screen.height;
            GUI.skin.button.richText = true;
            string titleText = showStartScreen ? "Start" : "All Done!";
            if (GUI.Button(new Rect(0.5f * (Screen.width - btnWidth), 0.5f * (Screen.height - btnHeight), btnWidth, btnHeight), titleText + "<size=25>\n<i>Click here or hit Spacebar</i></size>"))
            {
                StartNextExperiment();
                showStartScreen = false;
            }
        }
        else
            showStartScreen = false;

        //int buttonWidth = 200;
        //if (GUI.Button(new Rect(Screen.width - buttonWidth - 30, 20, buttonWidth, 30), "Start Next Experiment"))
        //    StartExperiment(expCount);
        //string experimentDesc = "Code: " + lastCode;
        //GUI.skin.label.fontSize = 14;
        //GUI.Label(new Rect(10, 10, 300, 100), experimentDesc);
    }

    public void HideAbstractPlayer()
    {
        showAbstractPlayer = false;
    }

    void StartNextExperiment()
    {
        StartExperiment(expCount);
    }

    void StartExperiment(int idx)
    {
        if( experiments.Count > 0 )
        {
            int nextExperiment = idx % experiments.Count;
            if (nextExperiment == 0 && idx != 0)
            {
                // Show Done Screen
                expCount = 0;
                GameManager.Inst.playerManager.SetLocalPlayerTransform(GameManager.Inst.playerManager.GetLocalSpawnTransform());
                TunnelEnvironmentManager.Inst.Reset();
                return;
            }

            StartExperiment(experiments[nextExperiment]);
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
        delayAfterChoiceToStartNext = exp.autoStartDelay;
        useMouseButtonsToChoose = !exp.mouseClickToChoose;
        StartExperiment(exp.angle, exp.avatarVisible, exp.userControl, exp.avatarPixelated);
    }

    void StartExperiment(float tunnelAngle, bool playerVis, UserControl uControl, bool playerAbstract = false)
    {
        if (expCount != 0)
            TunnelGameManager.Inst.RegisterEvent(TunnelEvent.TRIAL_DONE);
        Debug.LogError("Starting experiment: " + expCount);
        lastChoice = 0;
        GameManager.Inst.LocalPlayer.Visible = playerVis;
        showAbstractPlayer = playerAbstract;
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
        nextExperimentInQueue = false;
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

    int GetEventCode(TunnelEvent tEvent)
    {
        return GetCurrentCodeBase() + (int)tEvent;
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

        if (!nextExperimentInQueue && delayAfterChoiceToStartNext >= 0f)
        {
            nextExperimentInQueue = true;
            Invoke("StartNextExperiment", delayAfterChoiceToStartNext);
        }
    }

    public void RegisterAngleOffsets(float alloAngleOffset, float egoAngleOffset, float absoluteAngle)
    {
        if (lslSender != null)
            lslSender.SendAngleOffsets(alloAngleOffset, egoAngleOffset, absoluteAngle, GetEventCode(TunnelEvent.DECISION));


        if (!nextExperimentInQueue && delayAfterChoiceToStartNext >= 0f)
        {
            nextExperimentInQueue = true;
            Invoke("StartNextExperiment", delayAfterChoiceToStartNext);
        }
    }

}
