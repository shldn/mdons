using System.Collections;
using UnityEngine;
using Awesomium.Mono;
using Awesomium.Unity;
using Boomlagoon.JSON;
using Sfs2X.Entities.Data;
using Sfs2X.Requests;
using System;

public class HTMLGuiLayer : MonoBehaviour {
	
	public HTMLPanelGUI htmlPanel = null;
	private bool cached = false;
	private bool loading = false;
	private bool shown = false;
    private bool skipLogin = false;
    private bool loginOnUpdate = false;
    private bool hasInputFocus = false;
    private bool clickOffElementOpen = false;
    private string cmdOnUpdate = "";
    private int guiVersion = 16; // 16 -- send correct username from playerprefs, 15 -- send guilayer username from playerprefs, request hw info, fixed replay start path for local files; 14 -- handle a player with no op defined, try to recover from error in spawning local player; 13 -- after ddc, tutorial codes, fixed issue with handling batch monitors, 12 -- File dialogs for record/replay; 11 -- new add user/private room data, guilayer can change gui visibility; 10 -- execute js on webpanels, 9 -- uci teleport bug fix, 8 -- copy-paste, 7 -- send native tooltips, 6 -- send serverConfig for update message, lookat player networked,  5 -- post may 2014 release, 4 -- changed avatar init param, tooltip avoid rect, 3 -- added change mic options
	private string startingFile = ".html/guilayer.html";

    private bool loggingIn = false;
    private string loginArgs = null;
    private float loggingInFade = 0f;
    private bool loginInitialized = false;
    private DateTime startLoadHTMLTime = DateTime.Now;

#if DEBUG_LOGIN
    private string DEBUG_LOGIN = "true;";
#else
    private string DEBUG_LOGIN = "false;";
#endif
	


	// Use this for initialization
	void Start () {
		htmlPanel = this.gameObject.AddComponent(typeof(HTMLPanelGUI)) as HTMLPanelGUI;
		htmlPanel.width = Screen.width;
		htmlPanel.height = Screen.height;
        htmlPanel.RegisterNotificationCallback("InitComplete", delegate(JSValue[] args)
        {
            VDebug.LogError("Loading complete: " + (DateTime.Now - startLoadHTMLTime).TotalSeconds);
            if (loading)
            {
                Debug.LogError("JavaScript Execution started BEFORE awesomium finished loading page!!");
                loading = false;
            }
            if (!skipLogin && GameManager.Inst.LevelLoaded == GameManager.Level.CONNECT)
            {
                System.Version currClientVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                Debug.LogError("Client version: " + currClientVersion.ToString() + " req: " + CommunicationManager.clientVersionRequirement);
                if (CommunicationManager.clientVersionRequirement == "")
                {
                    // no version requirement specified, hand control to the guilayer.
                    WebHelper.ClearCache = true;
                    HandleClientVersion(currClientVersion.ToString());
                    return;
                }
                System.Version clientReq = new System.Version(CommunicationManager.clientVersionRequirement);
                if (currClientVersion >= clientReq)
                    AttemptLogin();
                else
                    ExecuteJavascriptWithValue(SetGUIVersionJSCmd() + SetBuildTypeCmd() + SetConfigCmd() + "showUpdateDialog();");
            }
            else
            {
                Debug.LogError((skipLogin) ? "Login screen skipped" : "GUI Layer has been reloaded in another scene!");
                skipLogin = false;
            }
        });
		htmlPanel.RegisterNotificationCallback("Authentication_Success", delegate(JSValue[] args)
		{
            GameGUI.Inst.fadeOut = true;

            loginArgs = args[1].ToString();
            loggingIn = true;
		});
		htmlPanel.RegisterNotificationCallback("MouseUp_MouseClick", delegate (JSValue[] args) {
			string json = args[1].ToString();
			VDebug.Log("mouseup fields: " + json);
			GameObject hitObject = MouseHelpers.GetCurrentGameObjectHit();
            if (hitObject != null)
            {
                if (GameManager.Inst.LocalPlayer.gameObject == hitObject)
                {
                    string cmd = "OnLocalPlayerClick(" + json + ");";
                    ExecuteJavascript(cmd);
                }
                else
                {
                    PlayerController playerController = hitObject.GetComponent<PlayerController>();
                    if (playerController != null)
                    {
                        Player p = playerController.playerScript;
                        string cmd = "OnPlayerClick(" + p.Id + ", " + p.Name + ", " + json + ");";
                        ExecuteJavascript(cmd);
                    }

                    WebPanel webPanel = hitObject.GetComponent<WebPanel>();
                    if (webPanel != null)
                    {
                        string cmd = "OnWebPanelClick(" + webPanel.browserTexture.id + ", " + json + ");";
                        ExecuteJavascript(cmd);
                    }


                    //string go = hitObject.name;// +hitObject.GetInstanceID();
                    //Debug.Log("We clicked something: " + go);
                    //string cmd = "OnUnityObjectClick('" + go + "', " + json + ");";
                    ////Debug.Log(cmd);
                    //ExecuteJavaScript(cmd);
                }
            }
		});
		htmlPanel.RegisterNotificationCallback("Out_UnityDebugLog", delegate (JSValue[] args) {
			string json = args[1].ToString();
			JSONObject jsonObject = JSONObject.Parse(json);
			string msg = jsonObject.GetString("msg");
			Debug.Log("Out_UnitDebugLog: " + msg);
		});
        htmlPanel.RegisterNotificationCallback("Clicked_QuitButton", delegate(JSValue[] args)
        {
            Application.Quit();
        });
        htmlPanel.RegisterNotificationCallback("Closed_DialogWindow", delegate(JSValue[] args)
        {
            AnnouncementManager.Inst.AnnouncementClosed();
        });
        htmlPanel.RegisterNotificationCallback("Clicked_AvatarButton", delegate(JSValue[] args)
        {
            GameManager.Inst.LoadLevel(GameManager.Level.AVATARSELECT);
        });
        htmlPanel.RegisterNotificationCallback("Clicked_ToggleVoice", delegate(JSValue[] args)
        {
            VoiceManager.Inst.ToggleToTalk = !VoiceManager.Inst.ToggleToTalk;
        });
        htmlPanel.RegisterNotificationCallback("VoicePush", delegate(JSValue[] args)
        {
            VoiceManager.Inst.PushToTalkButtonDown = args[1].ToBoolean();
        });
        htmlPanel.RegisterNotificationCallback("Clicked_RefreshAll", delegate(JSValue[] args)
        {
            if (GameManager.Inst.LevelLoaded == GameManager.Level.BIZSIM)
                BizSimManager.Inst.ReloadAll();
        });
        htmlPanel.RegisterNotificationCallback("Update_UserList", delegate(JSValue[] args)
        {
            GameGUI.Inst.userListMgr.RebuildHTMLUserList();
        });
        htmlPanel.RegisterNotificationCallback("Update_Timer", delegate(JSValue[] args)
        {
            UpdateTimer();
        });
        htmlPanel.RegisterNotificationCallback("Update_VoiceToggle", delegate(JSValue[] args)
        {
            // ask unity if the voice is toggled on or off 
            InitVoiceToggle();
        });
        htmlPanel.RegisterNotificationCallback("Play_ButtonClickSound", delegate(JSValue[] args)
        {
            SoundManager.Inst.PlayClick();
        });
        htmlPanel.RegisterNotificationCallback("Clicked_LogoutButton", delegate(JSValue[] args)
        {
            CommunicationManager.Inst.LogOut();
        });


        // Tooltips
        htmlPanel.RegisterNotificationCallback("Set_Tooltip", delegate(JSValue[] args)
        {
            if (args.Length <= 1)
            {
                Debug.LogError("attempting to change fixed tooltip, but didn't find an argument.");
                return;
            }
            GameGUI.Inst.SetFixedTooltip(args[1].ToString());
        });
        htmlPanel.RegisterNotificationCallback("Clear_Tooltip", delegate(JSValue[] args)
        {
            GameGUI.Inst.ClearFixedTooltip();
            GameGUI.Inst.tooltipAvoidRect = new Rect(0, 0, 0, 0); // clear avoidance rectangle
        });
        htmlPanel.RegisterNotificationCallback("Set_TooltipAvoidRect", delegate(JSValue[] args)
        {
            if (args.Length <= 4)
            {
                Debug.LogError("attempting to change fixed tooltip avoidance rectangle, but didn't find enough arguments.");
                return;
            }
            GameGUI.Inst.tooltipAvoidRect = new Rect(int.Parse(args[1].ToString()), int.Parse(args[2].ToString()), int.Parse(args[3].ToString()), int.Parse(args[4].ToString()));
        });

        // Microphone selection menu
        htmlPanel.RegisterNotificationCallback("Open_Mic_Menu", delegate(JSValue[] args)
        {
            VoiceManager.Instance.micSelectMenuOpen = true;
        });
        htmlPanel.RegisterNotificationCallback("Close_Mic_Menu", delegate(JSValue[] args)
        {
            VoiceManager.Instance.micSelectMenuOpen = false;
        });
        htmlPanel.RegisterNotificationCallback("Toggle_Mic_Menu", delegate(JSValue[] args)
        {
            VoiceManager.Instance.micSelectMenuOpen = !VoiceManager.Instance.micSelectMenuOpen;
        });

        // Seed console with text and move cursor to the front of the input.
        htmlPanel.RegisterNotificationCallback("Seed_Console", delegate(JSValue[] args)
        {
            if (args.Length <= 1)
            {
                GameGUI.Inst.consoleGui.SeedConsole("");
                return;
            }
            GameGUI.Inst.consoleGui.SeedConsole(args[1].ToString());
        });

        htmlPanel.RegisterNotificationCallback("Issue_ConsoleCmd", delegate(JSValue[] args)
        {
            if (args.Length <= 1)
            {
                Debug.LogError("attempting to issue console command, but didn't find an argument.");
                return;
            }
            ConsoleInterpreter.Inst.ProcCommand(args[1].ToString());
        });
        htmlPanel.RegisterNotificationCallback("Issue_FacConsoleCmd", delegate(JSValue[] args)
        {
            if (args.Length <= 1)
            {
                Debug.LogError("attempting to issue console command, but didn't find an argument.");
                return;
            }
            ConsoleInterpreter.Inst.ProcCommand(args[1].ToString(), PlayerType.LEADER);
        });
        htmlPanel.RegisterNotificationCallback("Write_ToConsole", delegate(JSValue[] args)
        {
            string msg = (args.Length <= 1) ? "" : args[1].ToString();
            GameGUI.Inst.WriteToConsoleLog(msg);
        });
        htmlPanel.RegisterNotificationCallback("Open_Browser", delegate(JSValue[] args)
        {
            if (args.Length <= 1)
                Debug.LogError("no url specified for Open_Browser command.");
            OpenInExternalBrowser(args[1].ToString());
        });
        htmlPanel.RegisterNotificationCallback("Click_AvatarChangeCharacter", delegate(JSValue[] args)
        {
            if (args.Length < 2)
            {
                Debug.LogError("no url specified for Open_Browser command.");
                return;
            }
            int modelIdx;
            if (!int.TryParse(args[1].ToString(), out modelIdx))
            {
                Debug.LogError(args[1] + " not a valid int");
                return;
            }
            GameGUI.Inst.customizeAvatarGui.ChangeCharacter(modelIdx);
        });
        htmlPanel.RegisterNotificationCallback("Click_AvatarOption", delegate(JSValue[] args)
        {
            if (args.Length < 3)
            {
                Debug.LogError("Click_AvatarOption - not enough arguments: elementName, newIndex " + args.Length);
                return;
            }

            int optionIdx;
            if(!int.TryParse(args[2].ToString(), out optionIdx))
            {
                Debug.LogError(args[2] + " not a valid int");
                return;
            }
            GameGUI.Inst.customizeAvatarGui.ChangeElement(args[1].ToString(), optionIdx);
        });
        htmlPanel.RegisterNotificationCallback("Click_AvatarRotate", delegate(JSValue[] args)
        {
            if (args.Length < 3)
            {
                Debug.LogError("Click_AvatarRotate - not enough arguments: is_left, is_down");
                return;
            }

            GameGUI.Inst.customizeAvatarGui.HandleRotateBtn(args[1].ToBoolean());
            GameGUI.Inst.customizeAvatarGui.HandleRotateDown(args[2].ToBoolean()); 
        });
        htmlPanel.RegisterNotificationCallback("Click_AvatarDone", delegate(JSValue[] args)
        {
            bool save = true;
            if (args.Length > 1)
                save = args[1].ToBoolean();
            GameGUI.Inst.customizeAvatarGui.HandleDoneBtn(save);
        });
        htmlPanel.RegisterNotificationCallback("Confirm_Action", delegate(JSValue[] args)
        {
            string action = (args.Length > 1) ? args[1].ToString() : "";
            if (GameManager.Inst.LevelLoaded == GameManager.Level.BIZSIM)
                BizSimManager.Inst.HandleAction(action);
        });
        htmlPanel.RegisterNotificationCallback("Send_SFSRequest", delegate(JSValue[] args)
        {
            if (args.Length < 2)
            {
                Debug.LogError("Not enough arguments for Send_SFSRequest");
                return;
            }

            string msg = (args.Length > 2) ? args[2].ToString() : "";
            ISFSObject msgObj = new SFSObject();
            msgObj.PutUtfString("msg", msg);
            ExtensionRequest request = new ExtensionRequest(args[1].ToString(), msgObj);
            CommunicationManager.SendMsg(request);
        });
        htmlPanel.RegisterNotificationCallback("Set_InputFocus", delegate(JSValue[] args)
        {
            if (args.Length < 2)
            {
                Debug.LogError("Set_InputFocus needs true/false argument");
                return;
            }
            hasInputFocus = args[1].ToString() == "true";
        });
        htmlPanel.RegisterNotificationCallback("Toggle_ClickOffElement", delegate(JSValue[] args)
        {
            if (args.Length < 2)
            {
                Debug.LogError("Toggle_ClickOffElement needs true/false argument");
                return;
            }

            clickOffElementOpen = args[1].ToString() == "true";
        });
        htmlPanel.RegisterNotificationCallback("Overlay_URL", delegate(JSValue[] args)
        {
            if (args.Length < 2)
            {
                Debug.LogError("Overlay_URL needs url");
                return;
            }
            string url = args[1].ToString();
            int width = (args.Length > 2) ? args[2].ToInteger() : (int)(0.75f * Screen.width);
            int height = (args.Length > 3) ? args[3].ToInteger() : (int)(0.75f * Screen.height);

            GameManager.Inst.OverlayMgr.SetURL(url, width, height);
        });
        htmlPanel.RegisterNotificationCallback("Scale_PresentTool", delegate(JSValue[] args)
        {
            if (args.Length <= 1)
            {
                Debug.LogError("attempting to scale present tool, but didn't find an argument.");
                return;
            }
            GameGUI.Inst.presentToolScale = (float)args[1].ToDouble();
        });
        htmlPanel.RegisterNotificationCallback("Set_GameGUIGutter", delegate(JSValue[] args)
        {
            if (args.Length <= 1)
            {
                Debug.LogError("attempting to set game gui gutter, but didn't find an argument.");
                return;
            }
            GameGUI.Inst.gutter = args[1].ToInteger();
        });
        htmlPanel.RegisterNotificationCallback("Hide_PresentUI", delegate(JSValue[] args)
        {
            GameGUI.Inst.showPresentToolButtons = false;
        });
        htmlPanel.RegisterNotificationCallback("Open_FileDialog", delegate(JSValue[] args)
        {
            string file = NativePanels.OpenFileDialog(null);
            string cmd = "handleOpenFile(\"" + file + "\");";
            ExecuteJavascript(cmd);
        });
        htmlPanel.RegisterNotificationCallback("Set_Clipboard", delegate(JSValue[] args)
        {
            if (args.Length <= 1)
            {
                Debug.LogError("attempting to set clipboard, but didn't find an argument.");
                return;
            }
            ClipboardHelper.Clipboard = args[1].ToString();
        });
        htmlPanel.RegisterNotificationCallback("Set_GUIVisibility", delegate(JSValue[] args)
        {
            if (args.Length <= 1)
            {
                Debug.LogError("attempting to set gui visibility, but didn't find an argument.");
                return;
            }
            GameGUI.Inst.VisibilityFlags = args[1].ToInteger();
        });
        htmlPanel.RegisterNotificationCallback("Update_TutorialCode", delegate(JSValue[] args)
        {
            if (args.Length <= 1)
            {
                Debug.LogError("attempting to update tutorial code, but didn't find an argument.");
                return;
            }
            CommunicationManager.CurrentUserProfile.UpdateTutorial(args[1].ToInteger());
        });

        
            
        htmlPanel.RegisterNotificationCallback("Request_AvatarOptions", SendGuiLayerAvatarOptions);
        htmlPanel.RegisterNotificationCallback("Request_GameIds", SendGuiLayerNumGames);
        htmlPanel.RegisterNotificationCallback("Request_TeamInfo", SendGuiLayerTeamInfo);
        htmlPanel.RegisterNotificationCallback("Request_MicInfo", SendGuiLayerMicInfo);
        htmlPanel.RegisterNotificationCallback("Request_TeacherClassInfo", SendGuiLayerTeamTeacherClass);
        htmlPanel.RegisterNotificationCallback("Request_PanelTitle", SendGuiLayerPanelTitle);
        htmlPanel.RegisterNotificationCallback("Request_PanelURL", SendGuiLayerPanelURL);
        htmlPanel.RegisterNotificationCallback("Request_PanelSelection", SendGuiLayerPanelSelection);
        htmlPanel.RegisterNotificationCallback("Request_GetClipboard", SendGuiLayerClipboard);
        htmlPanel.RegisterNotificationCallback("Request_GUIVisibility", SendGuiLayerGUIVisibility);
        htmlPanel.RegisterNotificationCallback("Request_PlayerInfo", SendGuiLayerPlayerInfo);
        htmlPanel.RegisterNotificationCallback("Request_WorkingDir", SendGuiLayerWorkingDir);
        htmlPanel.RegisterNotificationCallback("Request_ReplayUpdate", SendGuiLayerReplayPosInfo);
        htmlPanel.RegisterNotificationCallback("Request_TutorialCode", SendGuiLayerTutorialCode);
        htmlPanel.RegisterNotificationCallback("Request_HardwareInfo", SendGuiLayerHardwareInfo);
		
#if PATCHER_ENABLE
		//startingFile = GameManager.Inst.guipatcher.path + startingFile;
		Debug.LogError("HTMLGuiLayer - starting file: " + startingFile);
#endif
	}

    JSValue AttemptLogin()
    {
        VDebug.LogError("Time until attempt login: " + (DateTime.Now - startLoadHTMLTime).TotalSeconds);
        var cmd = SetGUIVersionJSCmd() + SetDebugLoginCmd() + SetBuildTypeCmd() + SetConfigCmd() + SetFullscreenCmd() + SetLastLoginNameCmd() + "showLoginDialog();";
        Debug.LogError(cmd);
        JSValue retValue = ExecuteJavascriptWithValue(cmd);
        if (retValue == null)
        {
            Debug.LogError("Got null trying to login");
            loginOnUpdate = true;
        }
        return retValue;
    }

    string SetGUIVersionJSCmd()
    {
        return "var guiVersion = " + guiVersion + ";";
    }

    string SetDebugLoginCmd()
    {
        return "var debugLogin = " + DEBUG_LOGIN;
    }

    string SetConfigCmd()
    {
        return "var origConfig = \"" + GameManager.Inst.ServerConfig + "\";";
    }

    string SetFullscreenCmd()
    {
        return "var fullscreen = " + (Screen.fullScreen ? "true" : "false") + ";";
    }

    string SetLastLoginNameCmd()
    {
        return "var lastLoginUsername = \"" + PlayerPrefs.GetString("vuser") + "\";";
    }

    string SetBuildTypeCmd()
    {
        return "var buildType = " + (int)GameManager.buildType + ";";
    }

    public void SetBuildType()
    {
        string cmd = SetBuildTypeCmd();
        if(!ExecuteJavascript(cmd))
            cmdOnUpdate += cmd;
    }

    void HandleClientVersion(string clientVer)
    {
        var cmd = "handleClientVersion(\""+clientVer +"\");";
        Debug.LogError(cmd);
        htmlPanel.browserTexture.ExecuteJavaScript(cmd);
    }

	void Update()
	{
        if (loginOnUpdate && !loading)
        {
            if( AttemptLogin() != null )
                loginOnUpdate = false;
        }
		if (htmlPanel.browserTexture != null && htmlPanel.browserTexture.webView != null && !shown)
		{
			Debug.Log(Show());
			shown = true;
		}

        if (loggingIn)
        {
            loggingInFade += Time.deltaTime;
            GameGUI.Inst.fadeAlpha = loggingInFade;

            Visible = false;

            if ((loggingInFade >= 1f) && !loginInitialized)
            {
                loginInitialized = true;
                loggingIn = false;

                DoAuthenticatedLogin();
                Visible = true;
            }
        }            

        if (cmdOnUpdate != "")
        {
            if( ExecuteJavascript(cmdOnUpdate) )
                cmdOnUpdate = "";
        }
	}

    void DoAuthenticatedLogin()
    {
        string json = loginArgs;
        Debug.Log("login form fields: " + json);
        JSONObject jsonObject = JSONObject.Parse(json);
        string sessionToken = jsonObject.GetString("sessionToken");
        string refreshRes = CommunicationManager.Inst.RefreshCurrentUserProfile(json);
        if (refreshRes == sessionToken)
        {
            string displayname = jsonObject.GetString("displayname");
            if (displayname == "")
                displayname = jsonObject.GetString("username");
            string username = displayname;
            Debug.Log(username + "logged in! sessionToken: " + sessionToken);

            string level = jsonObject.GetString("level");
            if (level == null)
                level = "";
            string teamID = jsonObject.GetString("teamid");
            if (teamID == null || teamID == "")
                teamID = "8";   //setting up a default teamid/room for ppl to join
#if DEBUG_LOGIN
                teamID = jsonObject.GetString("team");
#endif

            // If user has a server config specified use that, otherwise it will use the default.
            CommunicationManager.Instance.SetServerConfig(CommunicationManager.CurrentUserProfile.ServerConfig);

            // if fallback room has never been set, set to default level, useful for landing people in avatar selection room by default.
            if (string.IsNullOrEmpty(CommunicationManager.CurrentUserProfile.Room))
                CommunicationManager.CurrentUserProfile.UpdateProfile("room", ((int)ConsoleInterpreter.GetLevel(CommunicationManager.defaultLevel)).ToString());
            GameManager.Inst.lastLevel = ConsoleInterpreter.GetLevel(CommunicationManager.defaultLevel);

            CommunicationManager.Instance.ConnectToSmartFox(username, teamID, level);
            CommunicationManager.CurrentUserProfile.IncrementLoginCount();

            PlayerPrefs.SetString("vuser", CommunicationManager.CurrentUserProfile.Username);
        }
    }
	
	public string Show(int x=-1, int y=-1)
	{
		string ret = "Error: GUI data missing!";
		if (loading)
			return "GUI still loading...";
		if(!cached)
		{
			//htmlPanel.LoadFile(startingFile, x, y);
            htmlPanel.LoadURL(CommunicationManager.guihtmlUrl, x, y);
            htmlPanel.browserTexture.AddLoadCompleteEventListener(OnLoadCompleted);
            htmlPanel.browserTexture.webView.JSConsoleMessageAdded += OnJSConsoleMessageAdded;
            ret = "Showing new GUI layer...";
            startLoadHTMLTime = DateTime.Now;
		}
		else if (cached)
		{			
			htmlPanel.Place(x, y);
			htmlPanel.browserTexture.enableBgRendering = false;
			htmlPanel.browserTexture.RefreshWebView();
            ret = "Showing cached GUI layer...";
        }
		loading = true;
		return ret;
	}

	public string Hide()
	{
		string ret = "Error: Menu data missing!";
		if (loading)
			return "GUI still loading...";
		if (cached)
		{
			htmlPanel.browserTexture.enableBgRendering = true;
			htmlPanel.SetActive(false);
			ret = "Hide menu layer...";
		}
		return ret;
	}
	
	void OnLoadCompleted(System.Object sender, System.EventArgs args)
	{
		cached = true;
		loading = false;
		htmlPanel.SetActive(true);
	}

    public bool ExecuteJavascript(string cmd)
    {
        if (!loading && htmlPanel != null)
        {
            VDebug.Log("HTMLGuiLayer - ExecuteJavascript: " + cmd);
            htmlPanel.browserTexture.ExecuteJavaScript(cmd);
        }
        return !loading;
    }

    public JSValue ExecuteJavascriptWithValue(string cmd)
    {
        JSValue ret = null;
        if (!loading && htmlPanel != null)
        {
            VDebug.Log("HTMLGuiLayer - ExecuteJavascriptWithValue: " + cmd);
            ret = htmlPanel.browserTexture.ExecuteJavaScriptWithResult(cmd);
        }
        return ret;
    }

    private void OnJSConsoleMessageAdded(object sender, JSConsoleMessageEventArgs e)
    {
        string msg = "JSConsole: " + e.Source + "(" + e.LineNumber + "): " + e.Message;
        Debug.LogError(msg);
    }

    public void ReloadLayer(bool skipLogin_ = false)
    {
        // Update will load the starting file again.
        cached = false;
        shown = false;
        skipLogin = skipLogin_;
    }

    // Application specific helper functions -- should probably be moved out at some point
    public void UpdateTimer()
    {
        if (GameManager.Inst.LevelLoaded == GameManager.Level.BIZSIM)
        {
            string cmd = "setSecondsLeftInTimer(" + BizSimManager.Inst.QuarterTimeLeft.TotalSeconds + ");";
            ExecuteJavascript(cmd);
        }
    }

    public void InitVoiceToggle()
    {
        string enabled = VoiceManager.Inst.ToggleToTalk ? "true" : "false";
        string cmd = "initVoiceToggle(" + enabled + ");";
        ExecuteJavascript(cmd);
    }

    public void OpenInExternalBrowser(string url)
    {
        Application.OpenURL(url);
    }

    private void SendGuiLayerAvatarOptions(JSValue[] args)
    {
        InitAvatarLevel(GameManager.Inst.playerManager.GetLocalPlayer().GetUnisexAvatarOptionJSON());
    }

    private void SendGuiLayerNumGames(JSValue[] args)
    {
        //string cmd = "setGameIds(\"" + CmdRoomManager.GetGameIds() + "\");";
        //ExecuteJavascript(cmd);
    }

    public void SendGuiLayerTeamInfo(JSValue[] args)
    {
        string cmd = "setTeamInfo(" + TeamInfo.Inst.GetTeamClassJSON() + ");";
        ExecuteJavascript(cmd);
    }

    public void SendGuiLayerMicInfo(JSValue[] args)
    {
        string[] devs = VoiceChatRecorder.Instance.AvailableDevices;
        string devStr = (devs.Length > 0) ? devs[0] : "";
        foreach (string dev in devs)
            devStr += "," + dev;
        string cmd = "setMicInfo(" + devStr + ");";
        ExecuteJavascript(cmd);
    }

    public void SendGuiLayerTeamTeacherClass(JSValue[] args)
    {
        int teamID = CommunicationManager.Inst.roomNumToLoad;
        TeamInfo.Inst.GetTeacherID(teamID);
        TeamInfo.Inst.GetClassName(teamID);
        string cmd = "checkTeacherAndClass(\"" + TeamInfo.Inst.GetTeacherID(teamID) + "\", \"" + TeamInfo.Inst.GetClassName(teamID) + "\");";
        ExecuteJavascript(cmd);
    }

    public void SendGuiLayerPanelTitle(JSValue[] args)
    {
        int id = args.Length > 1 ? args[1].ToInteger() : -1;
        CollabBrowserTexture browserTexture = null;
        if (CollabBrowserTexture.GetAll().TryGetValue(id, out browserTexture))
        {
            string cmd = "webPanelTitle(" + id + ", \"" + browserTexture.webView.Title + "\");";
            ExecuteJavascript(cmd);
        }
        else
            Debug.LogError("Could not find browser texture " + id);
    }

    public void SendGuiLayerPanelURL(JSValue[] args)
    {
        int id = args.Length > 1 ? args[1].ToInteger() : -1;
        CollabBrowserTexture browserTexture = null;
        if (CollabBrowserTexture.GetAll().TryGetValue(id, out browserTexture))
        {
            string cmd = "webPanelURL(" + id + ", \"" + browserTexture.URL + "\");";
            ExecuteJavascript(cmd);
        }
        else
            Debug.LogError("Could not find browser texture " + id);
    }

    public void SendGuiLayerPanelSelection(JSValue[] args)
    {
        int id = args.Length > 1 ? args[1].ToInteger() : -1;
        CollabBrowserTexture browserTexture = null;
        if (CollabBrowserTexture.GetAll().TryGetValue(id, out browserTexture))
        {
            string cmd = "webPanelSelection(" + id + ", \"" + browserTexture.GetSelectedText() + "\");";
            ExecuteJavascript(cmd);
        }
        else
            Debug.LogError("Could not find browser texture " + id);
    }

    public void SendGuiLayerClipboard(JSValue[] args)
    {
        string callbackArg = args.Length > 1 ? args[1].ToString() : "";
        string clipboardStr = ClipboardHelper.Clipboard.Replace(@"\", @"\\");
        clipboardStr = clipboardStr.Replace(@"'", @"\'");
        clipboardStr = clipboardStr.Replace("\"", "\\\"");

        string cmd = "handleClipboard(\"" + clipboardStr + "\", \"" + callbackArg + "\");";
        ExecuteJavascript(cmd);
    }

    private void SendGuiLayerGUIVisibility(JSValue[] args)
    {
        string cmd = "handleGUIVisibilityFlags(" + GameGUI.Inst.VisibilityFlags + ");";
        ExecuteJavascript(cmd);
    }

    private void SendGuiLayerPlayerInfo(JSValue[] args)
    {
        if (args.Length < 3)
        {
            Debug.LogError("SendGuiLayerPlayerInfo - please specify a player id and the info you want");
            return;
        }

        // Get Player
        Player p = null;
        if( args[1].Type == JSValueType.Integer )
            p = GameManager.Inst.playerManager.GetPlayer(args[1].ToInteger());
        else
            p = GameManager.Inst.playerManager.GetPlayerByName(args[1].ToString());
        if (p == null)
        {
            Debug.LogError("Couldn\'t find player " + args[1].ToString());
            return;
        }

        // Get Info
        string info = "";
        if (args[2].ToString() == "avOp" || args[2].ToString() == "avatarOptions")
            info = p.GetAvatarOptionStr();

        string cmd = "handlePlayerInfo(\"" + args[1].ToString() + "\", \"" + args[2].ToString() + "\", \"" + info + "\");";
        ExecuteJavascript(cmd);
    }

    private void SendGuiLayerWorkingDir(JSValue[] args)
    {
        string cmd = "handleWorkingDir(\"" + System.IO.Directory.GetCurrentDirectory() + "\",\"" + ((args.Length > 1) ? args[1].ToString() : "\"") + ")";
        ExecuteJavascript(cmd);
    }

    public void SendGuiLayerSitStart()
    {
        string cmd = "handleSitStart();";
        ExecuteJavascript(cmd);
    }

    public void SendGuiLayerSitStop()
    {
        string cmd = "handleSitStop();";
        ExecuteJavascript(cmd);
    }

    public void SendGuiLayerRecordingStart(string filename)
    {
        string cmd = "handleRecordStart(\"" + filename + "\");";
        ExecuteJavascript(cmd);
    }

    public void SendGuiLayerRecordingStop()
    {
        string cmd = "handleRecordStop();";
        ExecuteJavascript(cmd);
    }

    public void SendGuiLayerReplayStartInfo(DateTime startTime, DateTime endTime, string replayResource, int replayLevel)
    {
        replayResource = replayResource.StartsWith("http") ? replayResource : replayResource.Replace('\\', '/');
        string cmd = "handleReplayStart(" + ConvertToUNIXTimestamp(startTime) + ", " + ConvertToUNIXTimestamp(endTime) + ", \"" + replayResource + "\", " + replayLevel + ", " + (int)GameManager.Inst.LevelLoaded + ");";
        ExecuteJavascript(cmd);
    }

    public void SendGuiLayerReplayPosInfo(JSValue[] args)
    {
        if (ReplayManager.Initialized)
        {
            string cmd = "handleReplayUpdate(" + ReplayManager.Inst.GetPlaybackPercent() + ");";
            ExecuteJavascript(cmd);
        }
    }

    public void SendGuiLayerTutorialCode(JSValue[] args)
    {
        string cmd = "handleTutorialCodeAction(" + CommunicationManager.CurrentUserProfile.TutorialCode + ", \""  + ((args.Length > 1) ? args[1].ToString() : "") + "\");";
        ExecuteJavascript(cmd);
    }

    public void SendGuiLayerHardwareInfo(JSValue[] args)
    {
        string cmd = "handleHardwareInfo(" + UserProfile.GetLocalHardwareInfoJSON() + "\");";
        ExecuteJavascript(cmd);
    }

    public void SendGuiLayerPresenterToolInfo(Rect presenterToolRect)
    {
        string cmd = "setPresenterToolInfo(" + presenterToolRect.x + "," + presenterToolRect.y + "," + presenterToolRect.width + "," + presenterToolRect.height + ");";
        ExecuteJavascript(cmd);
    }

    public void SendGuiLayerConsoleMsg(string msg, bool debug, bool chat)
    {
        if (debug)
            return;
        string cmd = "addConsoleMsg(\"" + msg + "\", \"" + (chat ? "true" : "false") + "\");";
        ExecuteJavascript(cmd);
    }

    public void SendGuiLayerChat(string msg, string fromName, int fromID, bool privateRoom = false, bool privateMsg = false, string privateRecipient = "")
    {
        string cmd = "addChatMsg(\"" + msg + "\", \"" + fromName + "\", " + fromID + ", \"" + (privateRoom ? "true" : "false") + "\", \"" + (privateMsg ? "true" : "false") + "\", \"" + privateRecipient + "\")";
        ExecuteJavascript(cmd);
    }

    public void SendGuiLayerNumUsersInRoom(string room, int numUsers)
    {
        string cmd = "handleNumUsersInRoom(\"" + room + "\"," + numUsers + ");";
        ExecuteJavascript(cmd);
    }

    public void SendGuiLayerTooltip(string tooltip, bool on)
    {
        string cmd = "handleTooltip(\"" + tooltip + "\", \"" + (on ? "true" : "false") + "\");";
        ExecuteJavascript(cmd);
    }

    public void SendGuiLayerProgress(string name, float progress)
    {
        string cmd = "handleProgress(\"" + name + "\", " + progress + ");";
        ExecuteJavascript(cmd);
    }

    public void SendGuiLayerUploadComplete(string uploadFileName, string error = "")
    {
        string cmd = "handleUploadComplete(\"" + uploadFileName + "\", \"" + error + "\");";
        ExecuteJavascript(cmd);
    }

    public void UpdatePrivateBrowserURL(string newURL)
    {
        string cmd = "updatePrivateBrowserURL(\"" + newURL + "\");";
        ExecuteJavascript(cmd);
    }

    public void InitAvatarLevel(string avatarOptionJSON)
    {
        string cmd = "if(typeof(initAvatarLevel) == typeof(Function)){initAvatarLevel(" + avatarOptionJSON + ");}";
        ExecuteJavascript(cmd);
    }

    public void HandleHelpMsg(string recipient, string msg)
    {
        string cmd = "if(typeof(handleHelpRequest) == typeof(Function)){handleHelpRequest(\"" + recipient + "\", \"" + msg + "\");}";
        ExecuteJavascript(cmd);
    }

    public void HandleDownloadRequest(string url)
    {
        string cmd = "handleDownloadRequest(\"" + url + "\");";
        ExecuteJavascript(cmd);
    }

    public void HandleDisplayUrlRequest(string url)
    {
        string cmd = "handleDisplayUrlRequest(\"" + url + "\");";
        ExecuteJavascript(cmd);
    }

    public void HandleClickOnUrlText(string url)
    {
        string cmd = "handleClickOnURLText(\"" + url + "\");";
        ExecuteJavascript(cmd);
    }

    public bool IsMouseOverGUIElement()
    {
        return htmlPanel != null && htmlPanel.browserTexture != null && htmlPanel.browserTexture.GetPixelColorAtMousePosition().a > 0.0f;
    }

    public string URL
    {
        get { return (htmlPanel == null || htmlPanel.browserTexture == null) ? "" : htmlPanel.browserTexture.URL; }
    }

    public double ConvertToUNIXTimestamp(DateTime time)
    {
        return time.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
    }

    public bool Visible { set { htmlPanel.browserTexture.GetComponent<GUITexture>().enabled = value; } }
    public bool HasInputFocus { get { return hasInputFocus; } }
    public bool ClickOffElementOpen { get { return clickOffElementOpen; } }
    public bool IsBlockingClickToMove { get { return clickOffElementOpen || hasInputFocus || IsMouseOverGUIElement(); } }
}
