using UnityEngine;
using Sfs2X.Core;
using System;
using System.IO;

//----------------------------------------------------------
// GameManager
//----------------------------------------------------------
[assembly: System.Reflection.AssemblyVersion("1.0.*.*")]
public class GameManager : MonoBehaviour {

	private bool initialized = false;
    public PlayerManager playerManager;
    private OverlayBrowserManager overlayMgr;
    public bool voiceToggleInitSetting = false; // to help keep setting across level loads
	public GUIPatcher guipatcher = null;
    private bool warningDisplayed = false;
    private DateTime warningStart = DateTime.UtcNow;

    // Build types
    public enum BuildType { DEBUG = 0, DEMO = 1, RELEASE = 2, REPLAY = 3 };
    public static BuildType buildType = BuildType.DEMO;

    // Level definitions, cooresponds to build order in unity
    public enum Level { NONE = -1, CONNECT = 0, CAMPUS = 1, BIZSIM = 2, INVPATH = 3, ORIENT = 4, AVATARSELECT = 5, TEAMROOM = 6, CMDROOM = 7, MINICAMPUS = 8, NAVTUTORIAL = 9, COURTROOM = 10, HOSPITALROOM = 11, BOARDROOM = 12, BOARDROOM_MED = 13, BOARDROOM_SM = 14, ASSEMBLY_CROQ = 15, OFFICE = 16, OPENCAMPUS = 17, MOTION_TEST = 18, SCALE_GAME = 19, MDONS_TEST = 20 };
    private Level loadLevel = Level.NONE;

    public int lastRoomId = -1;
    private Vector3 lastPosition = Vector3.zero;
    private Quaternion lastRotation = Quaternion.identity;

#if !STAGING
    private static string serverConfigName = "MDONS";
#else
    private static string serverConfigName = "staging";
#endif

    public Level lastLevel = (serverConfigName == "Demo" || serverConfigName == "Tycoon" || serverConfigName == "Helomics") ? Level.CAMPUS : Level.MINICAMPUS;
    public Level lastCampus = (serverConfigName == "Demo" || serverConfigName == "Tycoon" || serverConfigName == "Helomics") ? Level.CAMPUS : Level.MINICAMPUS;

    private static GameManager mInstance;
    public static GameManager Instance
    {
        get
        {
            if (mInstance == null)
            {
                Application.LoadLevel("Connection");
                GameObject go = GameObject.Find("Game");
                if (go == null)
                {
                    go = new GameObject("Game");
                    go.AddComponent(typeof(GameManager));
                }
                mInstance = go.GetComponent(typeof(GameManager)) as GameManager;
            }
            return mInstance;
        }
    }
    
    public static GameManager Inst
    {
        get{return Instance;}
    }


    void Awake() {
        DontDestroyOnLoad(this);
    }

	void Start() {
        if (mInstance != null)
            Destroy(mInstance.gameObject);
        mInstance = this;

#if (UNITY_STANDALONE && SKIP_SEVERCONFIG) || UNITY_WEBPLAYER
        ParseObject serverconfig = null;
#else
        ParseObject serverconfig = null; // ParseObjectFactory.FindByParseObjectByColumnValue("ServerConfig", "name", serverConfigName); // parse.com went down.
#endif

        CommunicationManager.Instance.InitializeSmartFoxServer(serverconfig);
#if UNITY_STANDALONE && PATCHER_ENABLE
		guipatcher = this.gameObject.AddComponent<GUIPatcher>();
#endif
        AddComponents();
        initialized = true;
	}

    void AddComponents()
    {
        if (playerManager == null)
            playerManager = this.gameObject.AddComponent<PlayerManager>();

        string[] componentsToAdd = { "GameGUI", "TeleportGUI" };
        foreach (string componentName in componentsToAdd)
            if (this.gameObject.GetComponent(componentName) == null)
                this.gameObject.AddComponent(componentName);
    }

    void OnLevelWasLoaded(int level)
    {
        IsLoadingLevel = false;
        if (LevelLoaded != Level.CONNECT)
            playerManager.OnLevelWasLoaded_(level); // make sure players are created before level managers are initialized.

        switch (LevelLoaded)
        {
            case Level.CONNECT:
            case Level.CAMPUS:
            case Level.MINICAMPUS:
                break;
            case Level.INVPATH:
                //InvisiblePathManager.Inst.OnRoomJoin(RoomJoinEvt);
                break;
            case Level.ORIENT:
                OrientationManager.Inst.OnRoomJoin(RoomJoinEvt);
                break;
            case Level.BIZSIM:
                BizSimManager.Inst.OnRoomJoin(RoomJoinEvt);
                break;
            case Level.AVATARSELECT:
                AvatarSelectionManager.Inst.Touch();
                break;
            case Level.NAVTUTORIAL:
                //TutorialManager.Inst.Touch();
                break;
            case Level.TEAMROOM:
                TeamRoomManager.Inst.Touch();
                break;
            case Level.CMDROOM:
                //CmdRoomManager.Inst.Touch();
                break;
            case Level.MOTION_TEST:
                TunnelGameManager.Inst.Touch();
                break;
            case Level.SCALE_GAME:
            case Level.MDONS_TEST:
                ScaleGameManager.Inst.Touch();
                break;
            default:
                Debug.LogError("Unknown Level Loaded");
                break;
        }

        RoomVariableUrlController.HandleRoomJoin(RoomJoinEvt);
        RoomVariableToEnable.HandleRoomJoin(RoomJoinEvt);

        UpdateLevelInGuiLayer();

        // server doesn't need to know each room change at this point.
        //if( LevelLoaded != Level.AVATARSELECT )
        //    CommunicationManager.CurrentUserProfile.UpdateProfile("room", level.ToString());

        ConsoleInterpreter.Inst.Touch(); // preload console instance
        RoomJoinEvt = null;

        if (GameManager.Inst.ServerConfig == "Assembly" || GameManager.Inst.ServerConfig == "MDONS")
            if(GameGUI.Inst.Visible != (LevelLoaded == Level.AVATARSELECT) )
                GameGUI.Inst.Visible = LevelLoaded == Level.AVATARSELECT;
    }

    void UpdateLevelInGuiLayer()
    {
        string cmd = "updateGameLevel(\"" + GameManager.LevelToShortString(LevelLoaded) + "\", " + CommunicationManager.Inst.roomNumToLoad + ", \"" + CommunicationManager.Inst.roomToJoin +"\");";
        if( !CommunicationManager.CurrentUserProfile.HasTutorialBeenDisplayed((int)LevelLoaded) )
            cmd += "handleTutorialMessage(" + (int)LevelLoaded + ");";
        if( GameGUI.Inst.guiLayer != null )
            GameGUI.Inst.ExecuteJavascriptOnGui(cmd);
    }

    void Update()
    {
        LoadLevelImpl(loadLevel);

        if (!GameGUI.Inst.Visible && Input.GetKeyUp(KeyCode.Return))
        {
            GameGUI.Inst.Visible = true;
			if( GameGUI.Inst.guiLayer != null )
	            GameGUI.Inst.guiLayer.Visible = true;
        }

        if (Input.GetKeyUp(KeyCode.Escape))
        {
            FocusManager.Inst.UnfocusKeyInputBrowserPanels();
            if (LocalPlayer.IsSitting)
                LocalPlayer.Stand();
            if (GameManager.Inst.ServerConfig == "MDONS")
                Application.Quit();
        }
    }

    public void LoadLastLevel()
    {
        LoadLevel(lastLevel);
    }
	
	public void LoadLevel(Level level, bool privateRm = false)
	{
        warningDisplayed = false;
        LoadLevelImpl(level, privateRm);
	}

    private void LoadLevelImpl(Level level, bool privateRm = false)
    {
        loadLevel = level;
        if( loadLevel == Level.NONE )
            return;

        if (!WebViewManager.Inst.AreAnyBusy() || (warningDisplayed && DateTime.UtcNow.Subtract(warningStart).TotalSeconds > 1.0f))
        {
            // save current state
            lastLevel = (level != GameManager.Inst.LevelLoaded) ? GameManager.Inst.LevelLoaded : lastLevel;
            lastCampus = (level == GameManager.Level.CAMPUS || level == GameManager.Level.MINICAMPUS) ? level : lastCampus;
            lastRoomId = CommunicationManager.Inst.roomNumToLoad;
            lastPosition = GameManager.Inst.LocalPlayer ? GameManager.Inst.LocalPlayer.gameObject.transform.position : Vector3.zero;
            lastRotation = GameManager.Inst.LocalPlayer ? GameManager.Inst.LocalPlayer.gameObject.transform.rotation : Quaternion.identity;
            CommunicationManager.LevelToLoad = loadLevel;
            if (privateRm)
                CommunicationManager.Inst.roomToJoin = "";
            IsLoadingLevel = true;
            CommunicationManager.SendJoinRoomRequest();
            loadLevel = Level.NONE;
            warningDisplayed = false;
        }
        else if (!warningDisplayed)
        {
            if( GameGUI.Inst.fadeAlpha < 0.9f )
                InfoMessageManager.Display("The level is still loading, you will be teleported shortly");
            WebViewManager.Inst.StopAll();
            warningDisplayed = true;
            warningStart = DateTime.UtcNow;
        }
    }

    private void Shutdown()
    {
        playerManager.Shutdown();
        CommunicationManager.Inst.Shutdown();
        Application.Quit();
    }

    private void OnApplicationQuit()
    {
#if UNITY_STANDALONE_WIN
        // Make a copy of the output log and preserve the previous copy if it exists 
        // -- better to do this stuff on Application start, but we don't have that control before the previous log gets overwritten
        string currentOutputLog = System.Diagnostics.Process.GetCurrentProcess().ProcessName + "_Data/output_log.txt";
        string backupOutputLog = System.Diagnostics.Process.GetCurrentProcess().ProcessName + "_Data/output_log_2.txt";
        string backupOutputLog2 = System.Diagnostics.Process.GetCurrentProcess().ProcessName + "_Data/output_log_3.txt";
        try {
            File.Move(Path.GetFullPath(backupOutputLog), Path.GetFullPath(backupOutputLog2));
        }catch(Exception){}
        try {
            File.Copy(Path.GetFullPath(currentOutputLog), Path.GetFullPath(backupOutputLog));
        }
        catch (Exception) { }
#endif
    }

    public void Destroy()
    {
        Debug.LogError("Destroy GameManager");
        GameGUI.Destroy();
        playerManager.Shutdown();
        mInstance = null;
        DestroyImmediate(playerManager);
        DestroyImmediate(gameObject);
    }

    public Player GetPlayer(int id)
    {
        if (playerManager == null)
            return null;
        Player player = null;
        playerManager.TryGetPlayer(id, out player);
        return player;
    }

    public static string GetSmartFoxRoom(Level level)
    {
        return LevelInfo.GetInfo(level).sfsRoom;
    }

    public static bool DoesLevelHaveSmartFoxRoom(Level level)
    {
        return GetSmartFoxRoom(level) != "";
    }

    // these are levels that use the team id to create separate instances per team.
    public static bool IsTeamInstanceLevel(GameManager.Level level)
    {
        return LevelInfo.GetInfo(level).teamInstanceRoom;
    }

    public static bool InBatchMode()
    {
#if UNITY_WEBPLAYER
        return false;
#else
        return Array.IndexOf(System.Environment.GetCommandLineArgs(), "-batchmode") != -1;
#endif
    }

    public static string LevelToString(GameManager.Level level)
    {
        switch (level)
        {
            case GameManager.Level.BIZSIM:
                return "Simulation Room";
            case GameManager.Level.CAMPUS:
                return "Campus";
            case GameManager.Level.MINICAMPUS:
                return "Campus";
            case GameManager.Level.INVPATH:
                return "Invisible Path Arena";
            case GameManager.Level.ORIENT:
                return "Lecture Hall";
            case GameManager.Level.AVATARSELECT:
                return "Avatar Customization Room";
            case GameManager.Level.NAVTUTORIAL:
                return "Navigation Tutorial";
            case GameManager.Level.TEAMROOM:
                return "Team Room";
            case GameManager.Level.CMDROOM:
                return "Command Room";
			case GameManager.Level.CONNECT:
                return "Connection Screen";
            default:
                Debug.LogError("Level: " + level + " unknown");
                break;
        }
        return "";
    }

    public static string LevelToShortString(GameManager.Level level)
    {
        switch (level)
        {
            case GameManager.Level.BIZSIM:
                return "bizsim";
            case GameManager.Level.CAMPUS:
                return "campus";
            case GameManager.Level.MINICAMPUS:
                return "minicampus";
            case GameManager.Level.INVPATH:
                return "invpath";
            case GameManager.Level.ORIENT:
                return "lecture";
            case GameManager.Level.AVATARSELECT:
                return "avatar";
            case GameManager.Level.NAVTUTORIAL:
                return "nav";
            case GameManager.Level.TEAMROOM:
                return "teamrm";
            case GameManager.Level.CMDROOM:
                return "cmdrm";
            case GameManager.Level.CONNECT:
                return "connect";
            case GameManager.Level.COURTROOM:
                return "court";
            case GameManager.Level.HOSPITALROOM:
                return "hospital";
            case GameManager.Level.BOARDROOM:
                return "boardrm";
            case GameManager.Level.BOARDROOM_MED:
                return "medboardrm";
            case GameManager.Level.BOARDROOM_SM:
                return "smboardrm";
            case GameManager.Level.OFFICE:
                return "office";
            case GameManager.Level.MOTION_TEST:
                return "tunnel";
            case GameManager.Level.SCALE_GAME:
                return "scale";
            case GameManager.Level.MDONS_TEST:
                return "playground";
            default:
                Debug.LogError("Level: " + level + " unknown");
                break;
        }
        return "";
    }

    //----------------------------------------------------------
    // Accessors
    //----------------------------------------------------------

    public bool Initialized { get { return initialized; } }
    public string ServerConfig { get { return !string.IsNullOrEmpty(CommunicationManager.CurrentUserProfile.ServerConfig) ? CommunicationManager.CurrentUserProfile.ServerConfig : serverConfigName; } }
    public Player LocalPlayer { get { return (playerManager == null) ? null : playerManager.GetLocalPlayer(); } }
    public PlayerType LocalPlayerType { get { return (LocalPlayer != null) ? LocalPlayer.Type : Player.GetPlayerType(CommunicationManager.CurrentUserProfile.GetField("permissionType")); } }
    public BaseEvent RoomJoinEvt { get; set; }
    public Level LevelLoaded { get { return LevelLoadedInfo.level; } }
    public LevelInfo LevelLoadedInfo { get { return LevelInfo.GetInfo(Application.loadedLevelName); } }
    public Level LastLevel { get { return lastLevel; } }
    public int LastRoomID { get { return lastRoomId; } }
    public bool IsLoadingLevel { get; private set; }
    public OverlayBrowserManager OverlayMgr
    {
        get
        {
            if (overlayMgr == null)
                overlayMgr = this.gameObject.AddComponent<OverlayBrowserManager>();
            return overlayMgr;
        }
    }
}