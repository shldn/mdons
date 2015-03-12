using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Awesomium.Mono;
using Sfs2X.Entities.Data;
using Sfs2X.Requests;

[RequireComponent(typeof(MeshCollider))]
public class BizSimScreen : MonoBehaviour {
    private static List<BizSimScreen> screens = new List<BizSimScreen>();
    public static List<BizSimScreen> GetAll() { return screens; }
    public static float refreshDelaySeconds = 2.0f; // seconds to wait before refreshing after getting a System Message
    protected const int pixelWidthOfScrollBar = 24;

    protected string url = "http://google.com";
    protected int stageItem = -1;
    protected int pixelWidth = 706; // add 24 pixels for scroll bar
    protected int pixelHeight = 500; // overwritten when useScaleForPixelDimensions is turned on.
    protected int bssId = CollabBrowserId.NONE; // will assign to bTex id, when bTex is created
    protected List<string> blacklistRequestURLFragments = new List<string>() {"licenceinfo.php", "helpcontent.php", "view_chart.php"};
    protected Dictionary<string, string> requestReplacements = new Dictionary<string, string>();
    public CollabBrowserTexture bTex;
    public bool useScaleForPixelDimensions = true;
    public float metersPerPixel = -1; // if -1, will use the default PlaneMeshFactory.metersPerPixel
    public GameObject activeOnFocusObj = null; // when focused this object becomes active, when focus lost this object becomes inactive
    protected bool removeTitle = true;
    protected bool disableScrolling = true;
    protected bool refreshOnInvestmentBudgetChange = false;
    protected bool refreshingOnQuarterChange = false;
    static private Texture2D refreshTex = null;
    static private Texture2D quarterEndedTex = null;

    // Store previous cmds sent from other clients, to stop loops of cmds recieved and sent back out.
    LinkedList<KeyValuePair<string, DateTime>> prevRemoteCmds = new LinkedList<KeyValuePair<string, DateTime>>();


    public string URL { set { url = value; } }
    public int StageItem { get { return stageItem; } }
    public bool IsURLLoaded { get { return bTex != null && bTex.IsLoaded; } }
    public bool Initialized { get { return bTex != null; } }

    public static string BaseURL {
        get {
            switch(BizSimManager.playMode) {
                case SimPlayMode.DEMO:
                    return "http://uis-demo.industrymasters.net";
                case SimPlayMode.MULTI_PLAYER:
                    return "http://unity.industrymasters.net";
                default: // SINGLE_PLAYER
                    return "http://uis-sustain.industrymasters.net"; 
            }
        }
    }

    public static string GetStageItemURL(int stageItemNum){
        return (BaseURL + "/holdinginfo.php?stage_item=" + stageItemNum);
    }

    protected virtual void Awake() {
        screens.Add(this);
        if (refreshTex == null)
            refreshTex = (Texture2D)Resources.Load("Textures/loadingRefresh");
        if (quarterEndedTex == null)
            quarterEndedTex = (Texture2D)Resources.Load("Textures/loadingQuarterOver");
        if (activeOnFocusObj != null)
            activeOnFocusObj.SetActive(false); // initialized assuming screen doesn't have focus.
    }

    protected virtual void Start() {
        if (!disableScrolling)
            pixelWidth += pixelWidthOfScrollBar;
    }

    protected virtual void OnDestroy() {
        screens.Remove(this);
    }

	public virtual void Initialize() {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null)
        {
            MeshCollider mc = GetComponent<MeshCollider>();
            if (mc == null)
            {
                mc = gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.mesh;
            }
            if (useScaleForPixelDimensions)
                pixelHeight = PanelHelpers.GetPxHeightFromTransform(this.transform, pixelWidth); 
            bTex = gameObject.AddComponent<CollabBrowserTexture>();
            bTex.Width = pixelWidth;
            bTex.Height = pixelHeight;
            bTex.id = bssId;
            bTex.InitialURL = url;
            bTex.KeyInputEnabled = false;
            bTex.blacklistRequestURLFragments.AddRange(blacklistRequestURLFragments);
            bTex.requestReplacements = requestReplacements;
            bTex.LoadingTexture = refreshTex;
            bTex.FocusChange += OnFocusChange;
        }
        else
        {
            PlaneMesh bPlane = PlaneMeshFactory.GetPlane(pixelWidth, pixelHeight, gameObject.name + "-bplane", true, url);
            bTex = bPlane.go.GetComponent<CollabBrowserTexture>();
        }
        bTex.AllowURLChanges = false;
        bTex.AddLoadCompleteEventListener(OnLoadCompleted);

        // web panel for focus control
        WebPanel webPanel = gameObject.AddComponent<WebPanel>();
        webPanel.focusCameraOnInputFieldClick = false; // not currently needed for bizsim panels
	}

    public virtual void Refresh()
    {
        bTex.RefreshWebView();
    }

    public void HandleQuarterEnded()
    {
        bTex.forceShowLoadingTexture = true;
        bTex.LoadingTexture = quarterEndedTex;
        refreshingOnQuarterChange = true;
    }

    protected string GetCmdStrResult(string cmd)
    {
        JSValue result = bTex.ExecuteJavaScriptWithResult(cmd);
        if (result != null && result.Type != JSValueType.Null)
        {
            return result.ToString();
        }
        return "";
    }

    void OnLoadCompleted(System.Object sender, System.EventArgs args)
    {
        if (refreshingOnQuarterChange)
        {
            bTex.LoadingTexture = refreshTex;
            refreshingOnQuarterChange = false;
        }
        bTex.forceShowLoadingTexture = false;
        if (IsSystemMessageDisplayed())
            RefreshDelayed(BizSimScreen.refreshDelaySeconds);
        else 
        {
            SetupCallbacks();
            if (removeTitle || disableScrolling)
            {
                string jsCmd = "";
                if (removeTitle)
                    jsCmd += "var elems = document.getElementsByTagName('h1'); if(elems != null && elems.length > 0){elems[0].style.display = 'none'}";
                if (disableScrolling)
                    jsCmd += "document.body.style.overflow = \"hidden\";";
                bTex.ExecuteJavaScript(jsCmd);
            }
        }
    }

    private bool IsSystemMessageDisplayed()
    {
        string jsCmd = "var headers = document.getElementsByTagName(\"h1\"); if( headers != null && headers.length > 0){headers[0].innerText;}";
        JSValue result = bTex.ExecuteJavaScriptWithResult(jsCmd);
        if (result != null && result.Type != JSValueType.Null)
        {
            string header = result.ToString();
            return header.Contains("System Message");
        }
        return false;
    }

    public bool IsValidJavaResult(JSValue result)
    {
        return (result != null && result.Type != JSValueType.Null);
    }

    public void RefreshDelayed(float waitSeconds)
    {
        StartCoroutine(RefreshDelayedImpl(waitSeconds));
    }

    IEnumerator RefreshDelayedImpl(float waitSeconds)
    {
        yield return new WaitForSeconds(waitSeconds);
        Refresh();
    }

    public void SendRefreshMeMessage()
    {
        ISFSObject screenRefreshObj = new SFSObject();
        screenRefreshObj.PutUtfString("type", "screen");
		screenRefreshObj.PutInt("rs", stageItem); // refresh screen
        CommunicationManager.SendObjectMsg(screenRefreshObj);
    }

    public void HandleInvestmentBudgetChange(bool refreshLocalCaller = false, bool refreshRemoteCaller = true)
    {
        foreach (BizSimScreen s in GetAll())
        {
            if (s.refreshOnInvestmentBudgetChange && (s != this || refreshLocalCaller))
                s.Refresh();
        }
        if(refreshRemoteCaller)
            SendRefreshMeMessage(); // assumes that a refresh on the remote client will detect the local investment budget change and refresh its local screens
    }

    protected virtual void HandleRefreshMeMessage()
    {
        Refresh();
    }

    public static void RefreshScreen(int stageItem)
    {
        foreach (BizSimScreen screen in screens)
            if (screen.stageItem == stageItem)
            {
                screen.HandleRefreshMeMessage();
                return;
            }
        Debug.LogError("BizSimScreen::RefreshScreen: Didn't find stage item: " + stageItem + " to refresh");

    }

    void OnFocusChange(System.Object sender, FocusEventArgs args)
    {
        if (activeOnFocusObj != null)
            activeOnFocusObj.SetActive(args.Focus);
    }


    // Callbacks and message handling
    void SetupCallbacks()
    {
        // callbacks available for all bizsim screens
        bTex.webCallbackHandler.RegisterNotificationCallback("SyncCmd", delegate(JSValue[] args)
        {
             if (args.Length < 2)
             {
                 Debug.LogError("SyncCmd cmd requires an argument that specifies the cmd: \"SyncCmd\" \"action\"");
                 return;
             }
             string cmd = args[1].ToString();
             UpdateServerWithCmd(cmd);
         });
        bTex.webCallbackHandler.RegisterNotificationCallback("SyncCmdForId", delegate(JSValue[] args)
        {
             if (args.Length < 3)
             {
                 Debug.LogError("SyncCmdForId cmd requires at least 2 arguments: \"SyncCmdForId\" \"element\" \"action\"");
                 return;
             }
             string elem = args[1].ToString();
             string appendCmd = args[2].ToString();
             string cmd = "var elem = document.getElementById(\"" + elem + "\"); if(elem != null){ elem" + appendCmd + "}";
             UpdateServerWithCmd(cmd);

         });
        bTex.webCallbackHandler.RegisterNotificationCallback("Announce", delegate(JSValue[] args)
        {
             if (args.Length < 3)
             {
                 Debug.LogError("Announce cmd requires at least 3 arguments: \"Announce\" \"title\" \"message\"");
                 return;
             }
             string title = args[1].ToString();
             string msg = args[2].ToString();
             AnnouncementManager.Inst.Announce(title, msg);
         });
    }

    bool IsPrevRemoteCmd(string cmd)
    {
        float minTimeToRestrictCmd = 0.5f; // seconds
        while (prevRemoteCmds.Count > 0 && (DateTime.Now.Subtract(prevRemoteCmds.First.Value.Value)).TotalSeconds > minTimeToRestrictCmd)
        {
            prevRemoteCmds.RemoveFirst();
        }
        foreach (KeyValuePair<string, DateTime> remCmd in prevRemoteCmds)
        {
            if (remCmd.Key == cmd)
                return true;
        }
        return false;
    }

    void UpdateServerWithCmd(string cmd)
    {
        // if the cmd was just executed and is trying to be resent to others, stop the loop
        if (IsPrevRemoteCmd(cmd))
            return;

        // build msg object
        ISFSObject screenUpdateObj = new SFSObject();
        screenUpdateObj.PutInt("id", bTex != null ? bTex.id : bssId); // type
        screenUpdateObj.PutUtfString("js", cmd); // value
        CommunicationManager.SendObjectMsg(screenUpdateObj);
    }

    void ExecuteRemoteJavaScript(string cmd)
    {
        prevRemoteCmds.AddLast(new KeyValuePair<string, DateTime>(cmd, DateTime.Now));
        bTex.ExecuteJavaScript(cmd);
    }

    public static void HandleMessage(SFSObject msgObj)
    {
        if (msgObj.ContainsKey("js"))
        {
            int id = msgObj.GetInt("id");
            for (int i = 0; i < screens.Count; ++i)
            {
                if (screens[i].bssId == id && !screens[i].bTex.isWebViewBusy())
                {
                    screens[i].ExecuteRemoteJavaScript(msgObj.GetUtfString("js"));
                    return;
                }
            }

            Debug.LogError("Didn't find page: " + msgObj.GetInt("id") + " or webView was busy, did not execute js");
        }
        if (msgObj.ContainsKey("pl"))
        {
            foreach (KeyValuePair<int, NewProductInvestmentsScreen> s in NewProductInvestmentsScreen.GetAll())
                s.Value.Refresh();
            ProductManagementScreen.HandleNewProductForAll();
        }
    }
}
