using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Sfs2X.Core;
using Sfs2X.Requests;
using Sfs2X.Entities;
using Sfs2X.Entities.Variables;
using Awesomium.Mono;

public enum SimPlayMode
{
    SINGLE_PLAYER,
    MULTI_PLAYER,
    DEMO,
    ADMIN_PLAYER
}

public class BizSimManager : MonoBehaviour {

    public static SimPlayMode playMode = SimPlayMode.SINGLE_PLAYER;   // SINGLE_PLAYER or MULTI_PLAYER -- can be set by database variable
    public static string gameID = "";
    public static string simType = "";
	public bool finalQuarter = false;
    private BizSimLoginAutomator loginAutomator = null;
    private BizSimAdminController adminController = null;
    public QuarterScheduler quarterScheduler = new QuarterScheduler();
    private ServerData serverData;
    private Bounds displayBounds = new Bounds();
    private Room room = null;
    public ProductManager productMgr = null;
	static public bool forceNewGame = false; // set false after one use.
    private bool polluting = false;
	private int quarterEndBufferSecs = 0;  // extra seconds at quarter end to allow industry master's server to update
    private double lastUpdateQuarterSecondsLeft = 0;

    private static BizSimManager mInstance;
    public static BizSimManager Instance
    {
        get
        {
            if (mInstance == null)
                mInstance = (new GameObject("BizSimManager")).AddComponent(typeof(BizSimManager)) as BizSimManager;
            return mInstance;
        }
    }
	
    public static BizSimManager Inst
    {
        get{return Instance;}
    }

    public static string LoginPage
    {
        get{ return (BizSimManager.playMode == SimPlayMode.MULTI_PLAYER) ? "unityaccess.php" : "instantaccess.php"; }
    }

    public static string LoginURL
    {
        get { return BizSimScreen.BaseURL + "/" + LoginPage; }
    }

    public static void SetPlayMode(string playModeStr)
    {
        if (GameManager.buildType == GameManager.BuildType.DEMO)
        {
            VDebug.LogError("Demo version, forcing sim to demo bizsimType");
            playMode = SimPlayMode.DEMO;
            return;
        }

        switch(playModeStr.ToLower()){
            case "singleplayer":
            case "single":
                playMode = SimPlayMode.SINGLE_PLAYER;
                break;

            case "demo":
                playMode = SimPlayMode.DEMO;
                break;

            case "multiplayer":
            case "multi":
                playMode = SimPlayMode.MULTI_PLAYER;
                break;

            default:
                Debug.LogError("Unknown playmode: " + playModeStr);
                break;
        }
    }

    public Bounds DisplayBounds
    {
        get { return displayBounds; }
    }

    public bool QuarterTimeLeftValid {
        get { return (serverData != null && !serverData.UsingManualQuarterAdvancement || (CurrentQuarter >= 0 && serverData != null && serverData.IsValid && quarterScheduler != null && quarterScheduler.HasDataForQuarter(CurrentQuarter))); }
    }
	
	public TimeSpan QuarterTimeLeft {
		get
		{
			TimeSpan timeLeft = TimeSpan.Zero;
            if (serverData != null && !serverData.UsingManualQuarterAdvancement)
			{
                timeLeft = serverData.QuarterEndTime.Subtract(DateTime.UtcNow) + TimeSpan.FromSeconds(quarterEndBufferSecs);
			}
			else if (QuarterTimeLeftValid)
			{
                timeLeft = quarterScheduler.GetTimeLeftInQuarter(serverData.CurrentQuarter) + TimeSpan.FromSeconds(quarterEndBufferSecs);
			}
			return (timeLeft.TotalSeconds > 0) ? timeLeft : TimeSpan.Zero;
		}
	}

    public int CurrentQuarter {
        get
        {
            if (serverData == null)
                return -1;
            return serverData.CurrentQuarter;
        }
    }

    public bool Polluting
    {
        get { return polluting; }
        set
        {
            polluting = value;
            UpdateAllFactoryPollutionVisuals(polluting);
        }
    }

    public void UpdateAllFactoryPollutionVisuals(bool polluting)
    {
        for (int i = 0; i < Factory.GetAll().Count; ++i)
            Factory.GetAll()[i].Polluting = polluting;
    }

    public List<BizSimScreen> GetAllScreens() {
        return BizSimScreen.GetAll();
    }

    public string DateTimeToMsgString(DateTime time)
    {
        return time.ToString("yyMMddHHmmss");
    }

    public DateTime MsgStringToDateTime(string msgStr)
    {
        if (String.IsNullOrEmpty(msgStr))
		{
			Debug.Log ("Bad DateTime from msg");
            return new DateTime();
		}
        return DateTime.ParseExact(msgStr, "yyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
    }

    public void SetNewCompanyName(string newCompanyName)
    {
        string newStartTime = DateTimeToMsgString(DateTime.UtcNow);
        List<RoomVariable> roomVariables = new List<RoomVariable>();
        roomVariables.Add(new SFSRoomVariable("company", newCompanyName));
        roomVariables.Add(new SFSRoomVariable("starttime", newStartTime));
        CommunicationManager.SendMsg(new SetRoomVariablesRequest(roomVariables, room));
    }
	
	void Awake () {
        // TODO -- refactor this, shouldn't have to find all of these, they should be children of the BizSimManager
        productMgr = gameObject.GetComponent<ProductManager>();
        if (productMgr == null)
            productMgr = gameObject.AddComponent<ProductManager>();
	}

    void Start()
    {
        if (GameManager.Inst.LevelLoaded == GameManager.Level.BIZSIM)
            HandleLogos();
    }

    private void HandleLogos()
    {
    }

    public void ReloadAll(bool reloadServerData = true)
    {
        Debug.Log("refreshing all biz sim screens");
        if (reloadServerData && serverData != null)
            serverData.RefreshWebView();
        foreach (BizSimScreen s in BizSimScreen.GetAll())
            s.Refresh();
    }

    private void SendQuarterEndedEventToScreens()
    {
        foreach (BizSimScreen s in BizSimScreen.GetAll())
            s.HandleQuarterEnded();
    }

    public List<JSValue> ExecuteJavaScriptOnAllBrowserPlanes(string JsCmd)
    {
    	List<JSValue> ret = new List<JSValue>();
        foreach (BizSimScreen s in BizSimScreen.GetAll())
		{
            ret.Add(s.bTex.ExecuteJavaScriptWithResult(JsCmd));
		}
		return ret;
    }

    // if affectMouseRepresentation, then the enable flag will affect whether mouse representations are on or off aswell
    public void EnableInputOnAllBrowserPlanes(bool enable, bool affectMouseRepresentation = true)
    {
        foreach (BizSimScreen s in BizSimScreen.GetAll())
            if (s.bTex != null)
            {
                s.bTex.AllowInputChanges = enable;
                if (affectMouseRepresentation)
                    s.bTex.ShowMouseRepresentation = enable;
            }
    }
	
	bool IsReadyToAdvanceQuarter()
	{
        return QuarterTimeLeftValid && QuarterTimeLeft.TotalSeconds <= 0 && serverData != null && !serverData.IsLoading && !finalQuarter;
	}
	
	void Update()
	{
        if (IsReadyToAdvanceQuarter())
            serverData.AttemptRefresh(); // when server data detects a quarter change it will force the screens to refresh.

        if (QuarterTimeLeftValid && QuarterTimeLeft.TotalSeconds <= 0 && lastUpdateQuarterSecondsLeft > 0)
            SendQuarterEndedEventToScreens();
        lastUpdateQuarterSecondsLeft = QuarterTimeLeft.TotalSeconds;

        if( QuarterTimeLeftValid )
            AudioAnnouncementManager.Inst.Update((int)QuarterTimeLeft.TotalSeconds);
	}
	
	public void AutoLogin(string companyName)
	{
        loginAutomator = new BizSimLoginAutomator(LoginURL, companyName, gameID, simType);
        loginAutomator.LoginComplete += OnLoginComplete;
	}

    public string GetUniqueCompanyName()
    {
        return "Team" + DateTime.UtcNow.ToString("yyMMddhhmmss");
    }

    public void OnRoomJoin(BaseEvent evt)
    {
        Debug.Log("Successfully joined room: " + (evt != null ? ((Sfs2X.Entities.Room)evt.Params["room"]).Name : "Unknown"));
        room = (evt == null) ? null : (Room)evt.Params["room"];
        InitSim();
    }

    public void InitSim(string overrideSimType = "")
    {
        string companyName = "";
        switch (playMode)
        {
            case SimPlayMode.SINGLE_PLAYER:

                // until Industry Masters releases single player for more types of sims
                simType = (overrideSimType == "") ? "uimx_sustain" : overrideSimType;

                if (room != null && room.ContainsVariable("company"))
                {
                    companyName = room.GetVariable("company").GetStringValue();
                    if (room.ContainsVariable("starttime"))
                    {
                        string startTimeStr = room.GetVariable("starttime").GetStringValue();

                        DateTime startTime = MsgStringToDateTime(startTimeStr);
                        Debug.Log("Sim was started: " + startTime.ToString());

                        if (forceNewGame || DateTime.UtcNow.Subtract(startTime).TotalHours >= 1.0)
                        {
                            Debug.Log("Last business simulation finished, start a new one");
                            // start a new game
                            companyName = GetUniqueCompanyName();
                            SetNewCompanyName(companyName);
                            forceNewGame = false;
                        }
                    }
                    else
                        Debug.LogError("room needs to have a starttime variable setup on the server");
                }
                else
                {
                    Debug.LogError("Shouldn't get here -- Need to create a company variable via the server interface!!!");
                    // create a company name -- this won't be persistent though, it dies when this user disconnects
                    companyName = GetUniqueCompanyName();
                    SetNewCompanyName(companyName);
                }
                AutoLogin(companyName);
                break;
            case SimPlayMode.MULTI_PLAYER:
                gameID = TeamInfo.Inst.GetBizSimGameId(CommunicationManager.CurrentTeamID);
                simType = (overrideSimType == "") ? TeamInfo.Inst.GetSimType(CommunicationManager.CurrentTeamID) : overrideSimType;
                simType = string.IsNullOrEmpty(simType) ? "uimx_sustain" : simType;
                companyName = GetMultiplayerTeamName();
                AutoLogin(companyName);
                break;
            case SimPlayMode.DEMO:
                simType = (overrideSimType == "") ? "uimx_sustain" : overrideSimType;
                AutoLogin("Demo");
                break;
            case SimPlayMode.ADMIN_PLAYER:
                Debug.LogError("Admin Player is not currently implemented");
                break;
        }

        SetupSimType(simType);

        // update current tabs
        for (int i = 0; room != null && i < productMgr.NumProducts; ++i)
        {
            if (room.ContainsVariable("cat" + i))
                HandleTabMsg(room, "cat" + i);
        }

        for (int i = 0; i < ProductManagementScreen.GetAll().Count; ++i)
        {
            string varName = "prdurl" + i;
            if (room != null && room.ContainsVariable(varName))
            {
                ProductManagementScreen screen = ProductManagementScreen.GetAll()[i];
                string url = (screen.Initialized) ? BizSimScreen.GetStageItemURL(screen.StageItem) : room.GetVariable(varName).GetStringValue();
                screen.HandleNewURL(url);
                if (overrideSimType != "")
                    ProductManagementScreen.GetAll()[i].UpdateServerWithNewURL(url);
            }
        }
        MainCameraController.Inst.snapCamMakesPlayersInvisible = true;
    }

    private void SetupSimType(string type)
    {
        bool generic_sim = (simType != "uimx_sustain");

        for (int i = ProductScreen.GetAll().Count - 1; i >= 0; --i)
            ProductScreen.GetAll()[i].gameObject.SetActive(!generic_sim);
        for (int i = GenericSimScreen.GetAll().Count - 1; i >= 0; --i)
            GenericSimScreen.GetAll()[i].gameObject.SetActive(generic_sim);

        if (!generic_sim)
        {
            TreeDisplayManager.Inst.SetLevel(1);
            if (productMgr != null && productMgr.productDisplay != null)
                productMgr.productDisplay.DisplayScreenIcons();
        }
        else
        {
            // replace car specific materials:
            GameObject snowGlobe = GameObject.Find("globe_base_tiered");
            Material replacementMat = (Material)Resources.Load("Materials/Asphalt");
            if (snowGlobe && replacementMat)
            {
                Renderer[] r = snowGlobe.GetComponentsInChildren<Renderer>() as Renderer[];
                for (int i = 0; i < r.Length; ++i)
                    MaterialHelpers.ReplaceAllMaterials(r[i], replacementMat);
            }
        }
        TreeDisplayManager.Inst.Enable(!generic_sim);
    }

    private string GetMultiplayerTeamName()
    {
        string name = TeamInfo.Inst.GetName(CommunicationManager.CurrentTeamID);
        return (name != "") ? name : ("Team" + (CommunicationManager.CurrentTeamID + 1));
    }

    private void HandleTabMsg(Room room, string varName)
    {
        int productID;
        if (int.TryParse(varName.Substring(3), out productID)) // 3 == "cat".Length
        {
            int tabIdx = room.GetVariable(varName).GetIntValue();
            productMgr.SetNewTab(productID, tabIdx);
        }
        else
            Debug.LogError("Room Variable \"" + varName + "\" not proper format for current active tab");
    }

    public void OnRoomVariablesUpdate(BaseEvent evt)
    {
        Room evtRoom = (Room)evt.Params["room"];
        if (room != null && room != evtRoom)
            Debug.LogError("Got OnRoomVariablesUpdate for wrong room?");

        room = evtRoom; 
        
        ArrayList changedVars = (ArrayList)evt.Params["changedVars"];
        foreach (string varName in changedVars)
        {
            if (varName.StartsWith("cat"))
                HandleTabMsg(room, varName);
            if (varName.StartsWith("prdurl"))
                ProductManagementScreen.HandleRoomVariable(varName, room.GetVariable(varName).GetStringValue());
        }

        if (room.ContainsVariable("product"))
            productMgr.HandleMessageObject(room.GetVariable("product").GetSFSObjectValue());
    }

    public void AddOnQuarterChangeEventListener(ServerData.QuarterChangeHandler handler)
    {
        Inst.serverData.QuarterChange += handler;
    }

    void OnLoginComplete(object sender, EventArgs e)
    {
        Inst.serverData = Inst.gameObject.AddComponent<ServerData>();
        Inst.serverData.QuarterChange += Inst.OnQuarterChange;
        DisplaySim();
    }

    private void DisplaySim()
    {
        foreach (BizSimScreen screen in BizSimScreen.GetAll())
        {
            if (!screen.Initialized)
            {
                screen.Initialize();

                // setup load complete callbacks
                screen.bTex.AddLoadCompleteEventListener(OnBrowserLoadComplete);
            }
            else
                screen.Refresh();
        }
    }

    public void SnapCameraToSeeAllBizSim()
    {
		if(displayBounds.size.sqrMagnitude <= 0.001f)
			return;
		
        float worldWidth = displayBounds.size.x;
        float worldHeight = displayBounds.size.y;

        float camPosOffset = CameraHelpers.GetCamDistToFitPlane(worldWidth, worldHeight, Camera.main.fieldOfView);
        Camera.main.transform.position = 0.5f * worldHeight * Vector3.up + camPosOffset * Vector3.forward;
        Camera.main.transform.forward = -Vector3.forward;
    }

    public void HandleAction(string action)
    {
        string[] tok = action.Split(new char[]{','}, 3);
        if( tok.Length < 3 )
        {
            Debug.LogError("Number of expected tokens not found");
            return;
        }

        int bssId = -1; 
        int.TryParse(tok[0].Substring(tok[0].IndexOf("=")+1), out bssId);

        string div = tok[1].Substring(tok[1].IndexOf("=")+1);
        string url = tok[2].Substring(tok[2].IndexOf("=")+1);;


        // This can become more efficient
        for (int i = 0; i < BizSimScreen.GetAll().Count; ++i)
        {
            if (BizSimScreen.GetAll()[i].bTex.id == bssId)
            {
                string error = null;
                if(BizSimScreen.GetAll()[i].bTex.isWebViewBusy())
                    error = "The panel appears to be busy, please try again";
                if(BizSimScreen.GetAll()[i].bTex.URL != url)
                    error = "The url appears to have changed, please try again";
                if( error != null )
                    InfoMessageManager.Display(error);
                else
                    BizSimScreen.GetAll()[i].bTex.ForceConfirmClickOnDiv(":submit");

                return;
            }
        }
    }

    private void OnBrowserLoadComplete(System.Object sender, System.EventArgs args)
    {
        CollabBrowserTexture bTexture = sender as CollabBrowserTexture;
        if (bTexture != null)
        {
            bTexture.ReplaceClass("lightbox", "");
            bTexture.AddURLChangeRequestEventListener(OnURLChangeRequest);
        }
    }

    private void OnURLChangeRequest(System.Object sender, UrlEventArgs args)
    {
        GameManager.Inst.OverlayMgr.SetURL(args.Url, 760, 480);
    }

    private void OnQuarterChange(System.Object sender, System.EventArgs args)
    {
        ReloadAll(false);
        AudioAnnouncementManager.Inst.Reset();
    }

    private void OnDestroy()
    {
        AudioAnnouncementManager.Inst.Destroy();
        TreeDisplayManager.Inst.Destroy();
    }
}
