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

public enum AdminEvent
{
    BREAK_START = 777,
    BREAK_END = 778,
    EXPERIMENT_START = 998,
    EXPERIMENT_END = 999,
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
    float delayAfterChoiceToStartNext = 2f; // for auto advancing to next trial after a decision
    float delayBetweenTrials = 2f;
    bool nextExperimentInQueue = false;
    int breakAfter = -1;


    // Experiment variables
    int expCount = 0;
    int breakCount = 0;
    int skipCount = 0;
    bool instructionScreenEventSent = false;
    bool allowUserControls = true;
    bool chooseArrow = true;
    bool useMouseButtonsToChoose = true;
    bool showAbstractPlayer = false;

    // bools for event codes
    bool playerVisible = false;
    bool abstractVisible = false;
    List<Experiment> experiments = new List<Experiment>();

    public bool UseRotatableArrow { get { return !chooseArrow; } }
    public bool UseMouseButtonsToChoose { get { return useMouseButtonsToChoose; } }
    public bool UseKeysToChoose { get { return useMouseButtonsToChoose; } }
    bool AutoMovePlayer { get { return !allowUserControls; } }

    // LSL variables
    public bool sendLSLData = true;
    public int lastCode = 0;
    public int lastChoice = 0;
    private TunnelEvent lastEvent = TunnelEvent.TUNNEL_ENTRANCE;
    LSLSender lslSender = null;

    // Abstract player
    Texture abstractTexture;

    // GUI
    bool showStartScreen = true;
    bool showBreakScreen = false;

    string errMsg = "";
    public string ErrorMessage { get { return errMsg; } set { errMsg = value; } }

    public void Touch() { }

    void Awake()
    {
#if UNITY_WEBPLAYER
        sendLSLData = false;
#endif

        if (sendLSLData)
            lslSender = gameObject.AddComponent<LSLSender>();
    }

	void Start () {
        RenderSettings.ambientLight = Color.black;
        GameManager.Inst.LocalPlayer.playerController.navMode = PlayerController.NavMode.locked;
        GameManager.Inst.LocalPlayer.Visible = false;

        // Read Config file if it exists
        experiments = TunnelConfigReader.Read("config.txt");
        if (experiments.Count == 0 && ErrorMessage == "")
            ErrorMessage = "Sorry, No experiments configured";

        breakAfter = TunnelConfigReader.breakAfter;
        skipCount = TunnelConfigReader.skipCount;
        breakCount = breakAfter == 0 ? -1 : breakAfter;
        abstractTexture = (Texture2D)Resources.Load("Textures/abstractPlayer");
	}

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Alpha2) || Input.GetKeyUp(KeyCode.Equals))
            GameManager.Inst.LoadLevel(GameManager.Level.SCALE_GAME);
        if (Input.GetKeyUp(KeyCode.Alpha3))
            GameManager.Inst.LoadLevel(GameManager.Level.MDONS_TEST);

        if (Input.GetKeyUp(KeyCode.Space) && (!IsRunningTunnel() || showStartScreen || showBreakScreen))
        {
            if (expCount == 0)
                breakCount = breakAfter;
            if(showBreakScreen)
                HandleBreakDone();
            else
                StartNextExperiment();
        }
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
            if (!instructionScreenEventSent && lslSender != null && lslSender.HasConsumers)//Time.timeSinceLevelLoad> 1.5f)
            {
                RegisterAdminEvent(showStartScreen ? AdminEvent.EXPERIMENT_START : AdminEvent.EXPERIMENT_END);
                instructionScreenEventSent = true;
            }
            GUI.skin = TunnelEnvironmentManager.Inst.guiSkin;
            float btnPercent = showStartScreen ? 0.75f : 0.5f;
            float btnWidth = btnPercent * Screen.width;
            float btnHeight = btnPercent * Screen.height;
            GUI.skin.button.richText = true;
            GUI.skin.button.wordWrap = true;
            GUI.skin.button.fontSize = (int)(Screen.height * 0.04f);
            int titleSize = (int)(Screen.height * 0.05f);
            string titleText = showStartScreen ? "Hi and Welcome!" : "All Done!";
            string bodyText = showStartScreen ? TunnelConfigReader.instructions : "";
            if (GUI.Button(new Rect(0.5f * (Screen.width - btnWidth), 0.5f * (Screen.height - btnHeight), btnWidth, btnHeight), "<size=" + titleSize + "><b>" + titleText + "</b>\n</size>\n" + bodyText + "<size=" + (titleSize - 2) + ">\n\n<i>To Start: Click here or hit Spacebar</i></size>"))
            {
                // backup if the experiment start event hasn't fired (hasn't reached the delay timeout...) The initial event isn't registering consistently
                if (!instructionScreenEventSent)
                    RegisterAdminEvent(showStartScreen ? AdminEvent.EXPERIMENT_START : AdminEvent.EXPERIMENT_END);
                showStartScreen = false;
                breakCount = breakAfter;
                StartNextExperiment();
            }
        }
        else
        {
            showStartScreen = false;
            instructionScreenEventSent = false;
        }

        // Break Screen
        if (showBreakScreen)
        {
            GUI.skin = TunnelEnvironmentManager.Inst.guiSkin;
            float btnPercent = 0.5f;
            float btnWidth = btnPercent * Screen.width;
            float btnHeight = btnPercent * Screen.height;
            GUI.skin.button.richText = true;
            GUI.skin.button.wordWrap = true;
            GUI.skin.button.fontSize = (int)(Screen.height * 0.04f);
            int titleSize = (int)(Screen.height * 0.06f);
            string titleText = "Wanna Break?";
            if (GUI.Button(new Rect(0.5f * (Screen.width - btnWidth), 0.5f * (Screen.height - btnHeight), btnWidth, btnHeight), "<size=" + titleSize + "><b>" + titleText + "</b>\n</size><size=" + (titleSize - 2) + ">\n<i>To Continue: Click here or hit Spacebar</i></size>"))
                HandleBreakDone();
        }

        if( errMsg != "" )
        {
            GUI.skin.label.alignment = TextAnchor.LowerCenter;
            GUI.color = Color.red;
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), errMsg);
            GUI.color = Color.white;

        }

        if( skipCount > 0 )
        {
            GUI.skin.label.alignment = TextAnchor.LowerCenter;
            GUI.color = Color.red;
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "Skipping first " + skipCount + " trials.");
            GUI.color = Color.white;
        }

        //int buttonWidth = 200;
        //if (GUI.Button(new Rect(Screen.width - buttonWidth - 30, 20, buttonWidth, 30), "Start Next Experiment"))
        //    StartExperiment(expCount);
        //string experimentDesc = "Code: " + lastCode;
        //GUI.skin.label.fontSize = 14;
        //GUI.Label(new Rect(10, 10, 300, 100), experimentDesc);
    }

    void HandleBreakDone()
    {
        breakCount = breakAfter;
        showBreakScreen = false;
        RegisterAdminEvent(AdminEvent.BREAK_END);
        StartNextExperiment();
    }

    public bool IsRunningTunnel()
    {
        return (int)lastEvent < (int)TunnelEvent.TUNNEL_EXIT;
    }

    public void HideAbstractPlayer()
    {
        showAbstractPlayer = false;
    }

    void StartNextExperiment()
    {
        if (skipCount > 0)
        {
            expCount += skipCount;
            skipCount = 0;
        }

        StartExperiment(expCount, expCount > 0 ? delayBetweenTrials : 0);
    }

    void StartNextExperimentImpl()
    {
        GameGUI.Inst.fadeOut = false;
        GameGUI.Inst.fadeAlpha = 0f;
        StartExperiment(experiments[expCount % experiments.Count]);
    }

    void ShowDoneScreen()
    {
        GameGUI.Inst.fadeOut = false;
        GameGUI.Inst.fadeAlpha = 0f;
        expCount = 0;
        GameManager.Inst.playerManager.SetLocalPlayerTransform(GameManager.Inst.playerManager.GetLocalSpawnTransform());
        TunnelEnvironmentManager.Inst.Reset();
    }

    void ShowBreakScreen()
    {
        GameGUI.Inst.fadeOut = false;
        GameGUI.Inst.fadeAlpha = 0f;
        showBreakScreen = true;
        GameManager.Inst.playerManager.SetLocalPlayerTransform(GameManager.Inst.playerManager.GetLocalSpawnTransform());
        TunnelEnvironmentManager.Inst.Reset();
        RegisterAdminEvent(AdminEvent.BREAK_START);
    }

    void StartExperiment(int idx, float delay = 0f)
    {
        if( experiments.Count > 0 )
        {
            if (expCount != 0 && lastEvent != TunnelEvent.TRIAL_DONE)
                TunnelGameManager.Inst.RegisterEvent(TunnelEvent.TRIAL_DONE);

            if( delay != 0f)
                GameGUI.Inst.fadeOut = true;

            int nextExperiment = idx % experiments.Count;
            if (nextExperiment == 0 && idx != 0)
            {
                Invoke("ShowDoneScreen", delay);
                return;
            }
            if (breakCount == 0)
            {
                Invoke("ShowBreakScreen", delay);
                return;
            }

            Invoke("StartNextExperimentImpl", delay);
        }
        else
        {
#if DEMO_MODE
            bool[] playerVis = { true, false, false, true };
            float[] tunnelAngle = { -45f, 30f, -15f, 45f, -30f, 15f };
            UserControl[] userControl = { UserControl.NONE, UserControl.PARTIAL, UserControl.NONE, UserControl.PARTIAL };
            StartExperiment(tunnelAngle[idx % tunnelAngle.Length], playerVis[idx % playerVis.Length], userControl[idx % userControl.Length]);
#endif
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
        //Debug.LogError("Starting experiment: " + expCount);
        lastChoice = 0;
        playerVisible = playerVis;
        abstractVisible = playerAbstract;
        showAbstractPlayer = playerAbstract;
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
        --breakCount;
        nextExperimentInQueue = false;
        RegisterTrialStart();
        RegisterEvent(TunnelEvent.TUNNEL_ENTRANCE);
        Screen.showCursor = false;
    }

    public int GetCurrentCodeBase()
    {
        float tunnelAngle = TunnelEnvironmentManager.Inst.GetTunnelAngle();

        int code = 2000;
        code += (int)Mathf.Abs(tunnelAngle);
        code += (!playerVisible && !abstractVisible) ? 4000 : 0;
        code += !abstractVisible ? 0 : 2000;
        code += tunnelAngle > 0 ? 0 : 1000;

        return code;
    }

    int GetEventCode(TunnelEvent tEvent)
    {
        return GetCurrentCodeBase() + (int)tEvent;
    }

    public void RegisterEvent(TunnelEvent tEvent, float metaData = 0f)
    {
        lastEvent = tEvent;
        lastCode = GetCurrentCodeBase() + (int)tEvent;
        if (lslSender != null)
            lslSender.SendCode(lastCode, metaData);
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

    public void RegisterAdminEvent(AdminEvent aEvent)
    {
        if (lslSender != null)
            lslSender.SendCode((int)aEvent);

    }

    public void RegisterTrialStart()
    {
        if (lslSender != null)
        {
            int code = 1000 + expCount;
            lslSender.SendCode(code);
            if (expCount > 1)
                lslSender.WriteLineToTextFile("");
            lslSender.WriteLineToTextFile(code.ToString());
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
