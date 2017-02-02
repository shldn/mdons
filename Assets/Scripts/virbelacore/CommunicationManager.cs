using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Variables;
using Sfs2X.Requests;
using Boomlagoon.JSON;
using Sfs2X.Entities.Data;


//----------------------------------------------------------
// CommunicationManager
//
// This class handles connection and communication with the server
// First implementation assumes a SmartFox server
//----------------------------------------------------------
public class CommunicationManager : MonoBehaviour {

    private SmartFox smartFox;
    private string loginErrorMessage = "";
    private string serverConnectionStatusMessage = "";
    public bool loadUnityLevelOnRoomJoin = true; // false for message monitor -- doesn't need to load levels, just needs to save messages.
    public int roomNumToLoad = 0;
    public Sfs2X.Logging.LogLevel logLevel = Sfs2X.Logging.LogLevel.DEBUG;
    public static readonly int MAXUSERS = 100;
	public static readonly bool DEBUG_SERVER = false; // need more global debugging flag...

    public static string serverName = "132.239.235.112";
    public static int serverPort = 443;
    public static string zone = "BizSimDemo";
    public static string username = "Guest";

    public static string defaultLevel = "motion";
    public static string bizsimType = "multiplayer";
    public static string buildType = "demo";
    public static string patchFileUrl = "http://virbela.com/img/test.zip";
    public static string guihtmlUrl = "";
    public static int patchFileVersion = 0;
    public static string clientVersionRequirement = "";
    public static string voiceMinFreq = "2.7";
    public static string voiceDefault = "off";
    public static int nativeGUILevel = 7;
    public static string shareTeams = "";
    public static string uploadUrl = "http://virbela.com/upload.php";
    public static bool redirectVideo = false;
    public static bool teamRmLockdown = false;
    public static List<int> parseScreens = new List<int>();
    public static string LastUserProfileReturnedStatus = "";
	
	private static UserProfile mCurrentUserProfile = null;
    private static DateTime lastSentMsgTime = DateTime.Now;
	
    private GameManager.Level levelToLoad = GameManager.Level.BIZSIM;
    public string roomToJoin = "";
    public bool useBuddyList = false;
    private int minTeamPrivateRoomID = 1000; // non-instanced rooms are instanced after this value

    private static CommunicationManager mInstance;
    public static CommunicationManager Instance
    {
        get
        {
            if (mInstance == null)
            {
                mInstance = (new GameObject("CommunicationManager")).AddComponent(typeof(CommunicationManager)) as CommunicationManager;
                CommunicationManager.username += DateTime.UtcNow.ToString("mm:ss:f");
            }
            return mInstance;
        }
    }
	
	public static CommunicationManager Inst
	{
		get{return Instance;}
	}

    void Awake()
    {
#if !UNITY_STANDALONE
        nativeGUILevel = 39;
#endif
    }

    public bool SetServerConfig(string serverConfigName)
    {
        ParseObject serverconfig = string.IsNullOrEmpty(serverConfigName) ? null : ParseObjectFactory.FindByParseObjectByColumnValue("ServerConfig", "name", serverConfigName);
        return SetServerConfig(serverconfig);
    }

    public bool SetServerConfig(ParseObject serverconfig)
    {
        if (serverconfig != null)
        {
            JSONObject jsonObject = JSONObject.Parse(serverconfig.RawJson);
            Debug.LogError("Setting to server config: " + jsonObject.GetString("name"));

            serverName = jsonObject.GetString("ip");
            serverPort = int.Parse(jsonObject.GetString("port"));
            zone = jsonObject.GetString("defaultZone");
            username = jsonObject.GetString("defaultGuestName");
            defaultLevel = jsonObject.GetString("defaultLevel");
            bizsimType = jsonObject.GetString("bizsimType");
            buildType = jsonObject.GetString("buildType");
            guihtmlUrl = jsonObject.GetString("guihtmlUrl");
            patchFileUrl = jsonObject.GetString("patchFileUrl");
            patchFileVersion = (int)jsonObject.GetNumber("patchFileVersion");
            clientVersionRequirement = jsonObject.GetString("clientVersionRequirement");
            voiceMinFreq = jsonObject.GetString("voiceMinFreq");
            voiceDefault = jsonObject.GetString("voiceDefault");
            shareTeams = jsonObject.GetString("shareTeams");
            uploadUrl = jsonObject.GetString("uploadUrl");
            nativeGUILevel = (int)jsonObject.GetNumber("nativeGUI");
            redirectVideo = jsonObject.GetBoolean("redirectVideo");
            teamRmLockdown = jsonObject.GetBoolean("teamRmLockdown");
            parseScreens = StringToIntList(jsonObject.GetString("parseScreens"));
        }

        // Level info may change based on new server config
        LevelInfo.Reset();

        // set build type from server config
        try{ 
            GameManager.buildType = (GameManager.BuildType)Enum.Parse(typeof(GameManager.BuildType), buildType, true);
        }
        catch (Exception) { Debug.LogError("Bad buildType: " + buildType); }

        if( GameGUI.Inst.guiLayer )
            GameGUI.Inst.guiLayer.SetBuildType();

        BizSimManager.SetPlayMode(CommunicationManager.bizsimType);
        GameManager.Inst.voiceToggleInitSetting = (voiceDefault == "on" || voiceDefault == "1" || voiceDefault == "true" || voiceDefault == "t");
        if (GameGUI.Inst.guiLayer != null && GameGUI.Inst.guiLayer.URL != guihtmlUrl && GameGUI.Inst.guiLayer.URL != "")
        {
            string errorMsg = "Compiled ServerConfig guilayer URL is different than user defined ServerConfig, this isn't supported at the moment. \n" + GameGUI.Inst.guiLayer.URL + " != " + guihtmlUrl;
            GameGUI.Inst.WriteToConsoleLog(errorMsg);
            Debug.LogError(errorMsg);
            GameGUI.Inst.guiLayer.ReloadLayer(true); // pulls url from CommunicationManager.guihtmlUrl
        }
        CommunicationManager.CurrentUserProfile.CheckLogin();
        return serverconfig != null;
    }

    public void InitializeSmartFoxServer(ParseObject serverconfig)
	{
        if( !SetServerConfig(serverconfig) )
        {
            Debug.LogError("Using builtin ServerConfig!");
            WebHelper.ClearCache = true;
        }

		smartFox = new SmartFox(DEBUG_SERVER);

		//CommunicationManager.CurrentUserProfile.CheckLogin(); // parse.com went down.
        HandleCommandLineArgs();
	}

    private void HandleCommandLineArgs()
    {


        bool useMonitor = false;
        int teamid = 0;
        string level = "bizsim";
        string username = "comp";
        string password = "2468!";
        LinkedList<string> cmds = new LinkedList<string>();
#if !UNITY_WEBPLAYER
        string[] cmdLnArgs = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < cmdLnArgs.Length; i++)
        {
            switch (cmdLnArgs[i])
            {
                case "-batchmode":
                    useMonitor = true;
                    break;
                case "-room":
                case "-level":
                case "-roomselect":
                    level = cmdLnArgs[++i];
                    break;
                case "-instance":
                case "-teamid":
                case "-team":
                    teamid = Convert.ToInt32(cmdLnArgs[++i]);
                    break;
                case "-cmd":
                    cmds.AddLast(cmdLnArgs[++i]);
                    break;
                default:
                    
                    // hacky, but assume this is extra pieces of the last command.
                    if (cmds.Count > 0)
                    {
                        string newLast = cmds.Last.Value + " " + cmdLnArgs[i];
                        cmds.RemoveLast();
                        cmds.AddLast(newLast);
                    }
                    break;
            }
        }
#endif
        if (useMonitor)
        {
            // These are here so the file name pulls the correct data
            LevelToLoad = ConsoleInterpreter.GetLevel((level != "") ? level : defaultLevel);
            roomNumToLoad = teamid;

            RecordingPlayer.Inst.Init("", true, false);
            mCurrentUserProfile.Login(username, password);
            loadUnityLevelOnRoomJoin = false; // no need for the monitor to actually load the unity scene, just needs to save the messages.
            ConnectToSmartFox(username, teamid.ToString(), level);
            while (cmds.Count > 0)
            {
                ConsoleInterpreter.Inst.ProcCommand(cmds.First.Value);
                cmds.RemoveFirst();
            }
        }
        else if (GameManager.Inst.ServerConfig == "Assembly" || GameManager.Inst.ServerConfig == "MDONS")
        {
            try
            {
                mCurrentUserProfile.Login("facguest", "fac[123]guest");
            }
            catch (Exception e)
            {
                Debug.LogError("Profile login failed: " + e.ToString());
            }
            Invoke("GuestConnectToSmartFox", 0.1f);
        }
    }

    private void GuestConnectToSmartFox()
    {
        string levelToLoad = GameManager.Inst.ServerConfig == "Assembly" ? "assembly" : "motion";
        ConnectToSmartFox("guest", "0", levelToLoad);
    }

    public void ConnectToSmartFox(string n, string r, string l="") {
        if (smartFox != null && (smartFox.IsConnecting || smartFox.IsConnected))
            return;

#if UNITY_WEBPLAYER
        Debug.LogError("WebPlayer setup: " + serverName + ":" + serverPort);
        if (!Security.PrefetchSocketPolicy(serverName, serverPort, 500))
        {
            Debug.LogError("Security Exception. Policy file load failed!");
        }
#endif

		username = n;
        roomNumToLoad = int.Parse(r);

        LevelToLoad = ConsoleInterpreter.GetLevel((l != "") ? l : defaultLevel);

        //string[] cmdLnArgs = System.Environment.GetCommandLineArgs();
        //BotManager.Inst.SetBotOptions(cmdLnArgs);

        // Register SFS callbacks
        smartFox.AddEventListener(SFSEvent.CONNECTION, OnConnection);
        smartFox.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
        smartFox.AddEventListener(SFSEvent.CONNECTION_RETRY, OnConnectionRetry);
        smartFox.AddEventListener(SFSEvent.CONNECTION_ATTEMPT_HTTP, OnConnectionAttemptHttp);
        smartFox.AddEventListener(SFSEvent.LOGIN, OnLogin);
        smartFox.AddEventListener(SFSEvent.LOGIN_ERROR, OnLoginError);
        smartFox.AddEventListener(SFSEvent.ROOM_JOIN, OnRoomJoin);
        smartFox.AddEventListener(SFSEvent.ROOM_JOIN_ERROR, OnRoomJoinError);
        smartFox.AddEventListener(SFSEvent.ROOM_CREATION_ERROR, OnRoomCreationError);
        smartFox.AddEventListener(SFSEvent.EXTENSION_RESPONSE, OnExtensionResponse);

        // Data messages
        AddEventListener(SFSEvent.ROOM_VARIABLES_UPDATE, DataMessageManager.Inst.OnRoomVariablesUpdate);
        AddEventListener(SFSEvent.PUBLIC_MESSAGE, DataMessageManager.Inst.OnPublicMessage);
        AddEventListener(SFSEvent.PRIVATE_MESSAGE, DataMessageManager.Inst.OnPrivateMessage);
        AddEventListener(SFSEvent.OBJECT_MESSAGE, DataMessageManager.Inst.OnObjectMessage);
        AddEventListener(SFSEvent.ADMIN_MESSAGE, DataMessageManager.Inst.OnAdminMessage);
        AddEventListener(SFSEvent.USER_VARIABLES_UPDATE, DataMessageManager.Inst.OnUserVariableUpdate);
        AddEventListener(SFSEvent.USER_ENTER_ROOM, DataMessageManager.Inst.OnUserEnterRoom);
        AddEventListener(SFSEvent.USER_EXIT_ROOM, DataMessageManager.Inst.OnUserExitRoom);

        // Callbacks for buddy events
        smartFox.AddEventListener(SFSBuddyEvent.BUDDY_LIST_INIT, OnBuddyListInit);
        smartFox.AddEventListener(SFSBuddyEvent.BUDDY_ERROR, OnBuddyError);
        smartFox.AddEventListener(SFSBuddyEvent.BUDDY_ONLINE_STATE_UPDATE, OnBuddyListUpdate);
        smartFox.AddEventListener(SFSBuddyEvent.BUDDY_VARIABLES_UPDATE, OnBuddyListUpdate);
        smartFox.AddEventListener(SFSBuddyEvent.BUDDY_ADD, OnBuddyAdded);
        smartFox.AddEventListener(SFSBuddyEvent.BUDDY_REMOVE, OnBuddyRemoved);
        smartFox.AddEventListener(SFSBuddyEvent.BUDDY_BLOCK, OnBuddyBlocked);
        smartFox.AddEventListener(SFSBuddyEvent.BUDDY_MESSAGE, OnBuddyMessage);
        smartFox.AddEventListener(SFSBuddyEvent.BUDDY_VARIABLES_UPDATE, OnBuddyVarsUpdate);

        Debug.LogError("Connecting to " + serverName + ":" + serverPort);
        smartFox.AddLogListener(logLevel, OnDebugMessage);
        smartFox.Connect(serverName, serverPort);
        serverConnectionStatusMessage = "Establishing Connection";
		DontDestroyOnLoad(this); // keep persistent when loading new levels
	}
	
    //no longer needed..
    //public bool UserAuthenticationLogin(string username, string password) {
    //    bool ret = false;
    //    if (mCurrentUserProfile != null)
    //    {
    //        if (mCurrentUserProfile.CheckLogin() == false)
    //            mCurrentUserProfile.Logout();
    //        CommunicationManager.LastUserProfileReturnedStatus = mCurrentUserProfile.Login(username, password);
    //        if (CommunicationManager.LastUserProfileReturnedStatus.ToLower().Contains("error"))
    //            Debug.Log("User authentication failed!\n" + CommunicationManager.LastUserProfileReturnedStatus);
    //        else
    //        {
    //            ret = true;
    //            Debug.Log("User authentication success! - sessionid: " + CommunicationManager.LastUserProfileReturnedStatus);
    //        }
    //    }
    //    else
    //        Debug.LogError("Something is really wrong! current user should NEVER be NULL!!!");
    //    return ret;
    //}
	
    public string RefreshCurrentUserProfile(string rawJson)
    {
        return mCurrentUserProfile.RefreshUserSession(rawJson);
    }

    public void Shutdown()
    {
        ShutdownServer();
    }

    private void ShutdownServer()
    {
        if (smartFox != null)
        {
            smartFox.RemoveAllEventListeners();
            if (smartFox.IsConnected)
                smartFox.Disconnect();
        }
    }
	
	void FixedUpdate() {
        if (smartFox != null) {
            smartFox.ProcessEvents();
        }
    }
	
	public Sfs2X.Entities.User FindUser(string username)
	{
        foreach (Room rm in CommunicationManager.JoinedRooms)
        {
            User user = rm.GetUserByName(username);
            if (user != null)
                return user;
        }
		return null;
	}
	
    public void AddEventListener(string eventType, EventListenerDelegate listener)
    {
        if (BotManager.botControlled) // bots don't need to handle messages at this point
            return;
        smartFox.AddEventListener(eventType, listener);
    }

    //----------------------------------------------------------
    // Handle connection response from server
    //----------------------------------------------------------
    public void OnConnection(BaseEvent evt)
    {
        bool success = (bool)evt.Params["success"];
        string error = (string)evt.Params["errorMessage"];

        Debug.Log("On Connection callback got: " + success + " (error : <" + error + ">)");

        if (success)
        {
            serverConnectionStatusMessage = "Connection succesful!";
            if (username == "guest")
                username += DateTime.UtcNow.ToString(":mmssff");
            if (username == "comp")
                username += DateTime.UtcNow.ToString(":mmssff");
            Instance.smartFox.Send(new LoginRequest(username, "", CommunicationManager.zone));
        }
        else
        {
            serverConnectionStatusMessage = "Can't connect to server!";
            GameGUI.Inst.fadeOut = false;
            GameGUI.Inst.ExecuteJavascriptOnGui("showDialog(\"Warning\", \"Unable to connect to virbela server at " + serverName + ":" + serverPort + ". This may be a company firewall issue.\");");
        }
        Debug.Log(serverConnectionStatusMessage);

        // if running in headless batch mode, launch appropriate level without connection gui.
        if( BotManager.botControlled )
            Instance.smartFox.Send(new LoginRequest(username, "v[pa](ss)", CommunicationManager.zone));
    }

    public void OnConnectionLost(BaseEvent evt)
    {
        if (GameManager.Inst.LevelLoaded == GameManager.Level.MOTION_TEST)
            return;

        Debug.Log("---------------------CommunicationMgr---- OnConnectionLost---------------");
        serverConnectionStatusMessage = "Connection was lost, Reason: " + (string)evt.Params["reason"];
        Debug.Log(serverConnectionStatusMessage);
        LogOut();
    }

    public void OnConnectionRetry(BaseEvent evt)
    {
        Debug.LogError("OnConnectionRetry");
    }

    public void OnConnectionAttemptHttp(BaseEvent evt)
    {
        Debug.LogError("OnConnectionAttemptHttp");
    }

    public void LogOut()
    {
        // Reset all internal states so we kick back to login screen
        if (Application.loadedLevel > 0)
        {
            if( smartFox != null )
                smartFox.RemoveAllEventListeners();
            Shutdown();
            GameManager.Inst.Destroy();
            Application.LoadLevel((int)GameManager.Level.CONNECT); // go back to login screen
        }
    }

	//----------------------------------------------------------
	// SFS Callbacks
	//----------------------------------------------------------
    public void OnLoginError(BaseEvent evt)
    {
        loginErrorMessage = (string)evt.Params["errorMessage"];
        Debug.LogError("Login error: " + loginErrorMessage);
    }

    public void OnDebugMessage(BaseEvent evt)
    {
        string message = (string)evt.Params["message"];
        Debug.LogError("[SFS DEBUG] " + message);
    }

    // TODO: this needs to be moved and settings exposed...
	public void OnLogin(BaseEvent evt) {
        Debug.Log("Logged in successfully, roomToJoin: " + roomToJoin);
        SendJoinRoomRequestImpl(Inst.roomToJoin);

        if( useBuddyList )
            smartFox.Send(new InitBuddyListRequest());
	}

    public static Room LastValidRoom()
    {
        Room rm = Inst.smartFox.LastJoinedRoom;
        if (Inst.smartFox.JoinedRooms.Count > 0 && !Inst.smartFox.JoinedRooms.Contains(rm))
            rm = Inst.smartFox.JoinedRooms[0];
        return rm;
    }
	
	private static void SendJoinRoomRequestImpl(string roomToJoinName, bool leaveAllCurrentRooms = true)
	{
        if (leaveAllCurrentRooms)
        {
            foreach (Room rm in Inst.smartFox.JoinedRooms)
                Inst.smartFox.Send(new LeaveRoomRequest(rm));
        }

        if (roomToJoinName == "")
        {
            Inst.OnRoomJoin(null);
            return;
        }

       // We either create the Game Room or join it if it exists already
        if (Inst.smartFox.RoomManager.ContainsRoom(roomToJoinName))
            Inst.smartFox.Send(new JoinRoomRequest(roomToJoinName, "", -1));
        else
        {
            RoomSettings settings = new RoomSettings(roomToJoinName);
            settings.MaxUsers = (short)MAXUSERS;
            settings.IsGame = roomToJoinName.Contains(GameManager.GetSmartFoxRoom(GameManager.Level.BIZSIM)) || roomToJoinName.Contains(GameManager.GetSmartFoxRoom(GameManager.Level.INVPATH)) || roomToJoinName.Contains(GameManager.GetSmartFoxRoom(GameManager.Level.TEAMROOM));
            settings.MaxVariables = 10;
            Inst.smartFox.Send(new CreateRoomRequest(settings, true));
        }
	}

    public static bool IsPrivateRoom(string roomName)
    {
        return roomName.StartsWith("PR");
    }
	
	public void OnRoomJoin(BaseEvent evt)
    {
        Room room = evt == null ? null : (Room)evt.Params["room"];
        if (room != null && IsPrivateRoom(room.Name))
        {
            OnPrivateRoomJoin(room);
            return;
        }

        GameManager.Inst.RoomJoinEvt = evt;
        AnnouncementManager.Inst.Clear();
        if (loadUnityLevelOnRoomJoin)
        {
            Debug.LogError("Loading next level ( " + levelToLoad.ToString() + " )");
            if (GameManager.Inst.ServerConfig == "UCI" && levelToLoad == GameManager.Level.CAMPUS)
            {
                levelToLoad = GameManager.Level.MINICAMPUS;
                Debug.LogError("Detected trying to load campus for UCI, loading minicampus");
            }
            Application.LoadLevel(LevelInfo.GetInfo(levelToLoad).sceneName);
            //StartCoroutine(LoadLevelAync(LevelInfo.GetInfo(levelToLoad).sceneName)); // Only supported in Unity Pro
        }
        else
            HideUser(); // users were still displaying in the scenes for other clients, so hide them.

        if (GameManager.InBatchMode())
            AddMonitorUserVariables();
    }

    IEnumerator LoadLevelAync(string loadingLevelName)
    {
        AsyncOperation async = Application.LoadLevelAsync(loadingLevelName);
        if (async == null)
        {
            Debug.LogError("Error loading scene: " + loadingLevelName + ". It might not be built into the game");
            GameManager.Inst.LoadLevel(GameManager.Inst.lastLevel);
            yield return null;
        }
        else
        {
            async.allowSceneActivation = false;
            while (!(async.progress > 0.89f))
            {
                yield return null;
            }
            Debug.LogError("Loading complete");
            async.allowSceneActivation = true;
        }
    }

    private void HideUser()
    {
        List<UserVariable> userVariables = new List<UserVariable>();
        userVariables.Add(new SFSUserVariable("ptype", (int)PlayerType.STEALTH));
        CommunicationManager.SendMsg(new SetUserVariablesRequest(userVariables));
    }

    private void AddMonitorUserVariables()
    {
        // not having this was causing a null reference exception on xnor for guiversion 12.
        List<UserVariable> userVariables = new List<UserVariable>();
        userVariables.Add(new SFSUserVariable("op", "0,0,"));
        CommunicationManager.SendMsg(new SetUserVariablesRequest(userVariables));
    }

    private void OnPrivateRoomJoin(Room room)
    {
        Debug.Log("Private Room Join: " + room.Name);
        PrivateVolume privateVol = GameManager.Inst.LocalPlayer.InPrivateVolume;
        if (privateVol == null)
        {
            Debug.LogError("Private Volume is null?");
            return;
        }
        privateVol.room = room;
        GameGUI.Inst.ExecuteJavascriptOnGui(GameManager.Inst.LocalPlayer.GetUserEnterPrivateRoomJSCmd(room.Name));

        for (int i = 0; i < room.UserList.Count; ++i)
        {
            Player player = null;
            if(GameManager.Inst.playerManager.TryGetPlayer(room.UserList[i].Id, out player))
                GameGUI.Inst.ExecuteJavascriptOnGui(player.GetUserEnterPrivateRoomJSCmd(room.Name));
        }
    }
	
	private void OnRoomJoinError(BaseEvent evt)
	{
        string errMsg = (string)evt.Params["errorMessage"];
        Debug.LogError("Error Joining Room: " + errMsg);
	}

    private void OnExtensionResponse(BaseEvent evt)
    {
        VDebug.LogError("Extension response");
        string cmd = (string)evt.Params["cmd"];
        if (cmd == "add")
        {
            SFSObject pObj = (SFSObject)evt.Params["params"];
            VDebug.LogError("Success! " + pObj.GetInt("res"));
        }
        else if (cmd == "admMsg")
        {
            VDebug.LogError("admin message response!");
        }
        else if (cmd == "guicmd")
        {
            SFSObject pObj = (SFSObject)evt.Params["params"];
            string js = pObj.GetUtfString("js");
            VDebug.LogError("guicmd executing: " + js);
            GameGUI.Inst.ExecuteJavascriptOnGui(js);
        }
        else if (cmd == "numUsers")
        {
            SFSObject pObj = (SFSObject)evt.Params["params"];
            string room = pObj.GetUtfString("room");
            int users = pObj.GetInt("users");
            VDebug.LogError(room + " has " + users + " users");
            GameGUI.Inst.guiLayer.SendGuiLayerNumUsersInRoom(room, users);
        }
    }

    public void OnRoomCreationError(BaseEvent evt) {
        string errMsg = (string)evt.Params["errorMessage"];
        Debug.LogError("Error Creating Room: " + errMsg);
        GameManager.Inst.LoadLastLevel();
        AnnouncementManager.Inst.Announce("Warning", "Error creating a room, you may have to log off and back on to complete this action");
    }

    // Buddy list handlers
    private void OnBuddyListInit(BaseEvent evt)
    {
        VDebug.LogError("Buddy list init");
    }

    private void OnBuddyError(BaseEvent evt)
    {
        VDebug.LogError("Buddy list error: " + (string)evt.Params["errorMessage"]);
    }

    private void OnBuddyListUpdate(BaseEvent evt)
    {
    }

    private void OnBuddyAdded(BaseEvent evt)
    {
        Buddy buddy = (Buddy)evt.Params["buddy"];
        VDebug.LogError("Buddy " + buddy.Name + " added");
    }

    private void OnBuddyRemoved(BaseEvent evt)
    {
        VDebug.LogError("buddy removed");
    }

    private void OnBuddyBlocked(BaseEvent evt)
    {
        Buddy buddy = (Buddy)evt.Params["buddy"];
        string message = (buddy.IsBlocked ? " blocked" : "unblocked");
        VDebug.LogError("Buddy " + buddy.Name + " is " + message);
    }

    private void OnBuddyMessage(BaseEvent evt)
    {
        Buddy buddy;
        Buddy sender;
        Boolean isItMe = (bool)evt.Params["isItMe"];
        string message = (string)evt.Params["message"];
        string buddyName;

        sender = (Buddy)evt.Params["buddy"];
        if (isItMe)
        {
            ISFSObject playerData = (SFSObject)evt.Params["data"];
            buddyName = playerData.GetUtfString("recipient");
            buddy = smartFox.BuddyManager.GetBuddyByName(buddyName);
        }
        else
        {
            buddy = sender;
            buddyName = "";
        }

        if (buddy != null)
        {
            // Store the message according to the sender's name
            message = (isItMe ? "To " + buddyName : "From " + buddy.Name) + ": " + message;
            GameGUI.Inst.WriteToConsoleLog(message);
        }
    }

    private void OnBuddyVarsUpdate(BaseEvent evt)
    {
    }



    void OnApplicationQuit()
    {
        ShutdownServer();
    } 

    //----------------------------------------------------------
    // Static helpers
    //----------------------------------------------------------
	public static UserProfile CurrentUserProfile
    {
		get
		{
			if (mCurrentUserProfile == null)
				 mCurrentUserProfile = new UserProfile();
			return mCurrentUserProfile;
		}
	}
	
    // Get Current User -- MySelf
	public static Sfs2X.Entities.User MySelf{ 
        get{ return Instance.smartFox.MySelf; } 
    }
    
	public static Sfs2X.Entities.User CurrentUser{ 
        get{ return MySelf; } 
    }

    public static int CurrentUserID
    {
        get { return CurrentUser.Id; }
    }   

	public static Sfs2X.Entities.Room LastJoinedRoom{ 
        get{ return Instance.smartFox.LastJoinedRoom; } 
    }

    public static Sfs2X.Entities.Managers.IUserManager UserManager{ 
        get{ return Instance.smartFox.UserManager; } 
    }

    // Will only send a message if connected to a smartfox room, so don't use for logging in to SFS, etc.
    public static void SendMsg( IRequest request) {
        if (!InASmartFoxRoom)
            return;
        lastSentMsgTime = DateTime.Now;
		Instance.smartFox.Send (request);
	}

    // Will only send a message if connected to a smartfox room, so don't use for logging in to SFS, etc.
    public static void SendObjectMsg(ISFSObject obj)
    {
        SendMsg(new ObjectMessageRequest(obj, LastValidRoom()));
        if (RecordingPlayer.Active && RecordingPlayer.Inst.RecordMyActions)
            DataMessageManager.Inst.RecordObjectMessage(MySelf, (SFSObject)obj);
    }

    public static void SendJoinRoomRequest() {
        SendJoinRoomRequestImpl(Inst.roomToJoin);
    }

    public static void SendJoinPrivateRoomRequest(string roomName)
    {
        SendJoinRoomRequestImpl(roomName, false);
    }

    public static void SendLeavePrivateRoomRequest(Room privateRm)
    {
        Inst.smartFox.Send(new LeaveRoomRequest(privateRm));
    }


    //----------------------------------------------------------
    // Local Helpers
    //----------------------------------------------------------

    // assumes comma separated string
    private List<int> StringToIntList(string str)
    {
        List<int> intList = new List<int>();
        if (!string.IsNullOrEmpty(str))
        {
            int tempInt = 0;
            string[] tokens = str.Split(',');
            foreach (string tok in tokens)
                if (int.TryParse(tok, out tempInt))
                    intList.Add(tempInt);
        }
        return intList;
    }



	//----------------------------------------------------------
	// Accessors
	//----------------------------------------------------------

    public static int CurrentTeamID { get { return Inst.roomNumToLoad; } }
	public static bool IsUserAuthenticated { get{ return CommunicationManager.CurrentUserProfile.CheckLogin();} }
    public static bool IsConnected { get { return Instance.smartFox.IsConnected; } }
    public static bool IsConnecting { get { return Instance.smartFox != null && Instance.smartFox.IsConnecting; } }
    public static List<Room> JoinedRooms { get { return Instance.smartFox.JoinedRooms; } }
    public static bool InASmartFoxRoom { get { return JoinedRooms.Count != 0; } }
    public static string LoginErrorMsg { get { return Instance.loginErrorMessage; } }
    public static string ServerConnectionStatus { get { return Instance.serverConnectionStatusMessage; } }
    public static string LevelToLoadName { get { return LevelInfo.GetInfo(Inst.levelToLoad).sceneName; } }
    public static GameManager.Level LevelToLoad
    {
        get { return Inst.levelToLoad; }
        set
        {
            Inst.levelToLoad = value;
            Inst.roomToJoin = GameManager.GetSmartFoxRoom(value);
            if (Inst.roomToJoin != "" && ((GameManager.IsTeamInstanceLevel(Inst.levelToLoad) && Inst.roomNumToLoad > 0) || Inst.roomNumToLoad >= Inst.minTeamPrivateRoomID))
                Inst.roomToJoin += Inst.roomNumToLoad.ToString();
        }
    }
    public static TimeSpan IdleTime { get { return DateTime.Now.Subtract(lastSentMsgTime); } }
	
}
