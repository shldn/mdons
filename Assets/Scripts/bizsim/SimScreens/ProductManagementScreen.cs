using UnityEngine;
using Awesomium.Mono;
using System.Collections.Generic;
using Sfs2X.Entities.Variables;
using Sfs2X.Requests;

public class ProductManagementScreen : GenericSimScreen
{
    private static Dictionary<int, ProductManagementScreen> allProdMgrs = new Dictionary<int, ProductManagementScreen>();
    new public static Dictionary<int, ProductManagementScreen> GetAll() { return allProdMgrs; }
    

    public int id = -1;
    private string tabOnLoad = "";
    private string urlOnLoad = "";
 
    private int ServerID { get { return id - 1; } }

    public static void HandleNewProductForAll()
    {
        foreach (KeyValuePair<int, ProductManagementScreen> s in ProductManagementScreen.GetAll())
            s.Value.HandleNewProduct();
    }

    protected override void Awake()
    {
        allProdMgrs[ServerID] = this;
        base.Awake();
        stageItem = 30;
        bssId = CollabBrowserId.PRODUCTMGR + id;
        if (id < 0)
            Debug.LogError("ProductManagementScreen: Please specify an id in the editor");
        url = BizSimScreen.GetStageItemURL(stageItem);
    }

    public override void Initialize()
    {
        base.Initialize();
        bTex.AllowURLChanges = true;
        bTex.AddLoadCompleteEventListener(OnLoadComplete);
        bTex.supportedDialogMessages.Add("Are you sure you want to upsize this business unit?", "bssid=" + bssId + ",div=butexpand");
        bTex.supportedDialogMessages.Add("Are you sure you want to liquidate this business unit?", "bssid=" + bssId + ",div=butliquidate");
        bTex.supportedDialogMessages.Add("Are you sure you want to downsize this business unit?", "bssid=" + bssId + ",div=butdownsize");
        bTex.supportedDialogMessages.Add("Are you sure you want to relaunch this business unit?", "bssid=" + bssId + ",div=butrelaunch");
    }

    public void AddToProductList(string displayStr, string productID)
    {
        if (bTex.isWebViewBusy())
            return;

        // add option to drop down for product choices 
        string cmd = "var elems = document.getElementsByName(\"prod_filter\"); if(elems != null && elems.length > 0){$(elems[0]).append('<option value=\"" + productID + "\">" + displayStr + "</option>')}"; 
        bTex.ExecuteJavaScript(cmd);

    }

    public bool UpdateProductList()
    {
        if (bTex.isWebViewBusy())
            return false;

        int splitIdx = bTex.URL.IndexOf("prod_filter=");
        string prodID = splitIdx != -1 ? bTex.URL.Substring(splitIdx + ("prod_filter=").Length) : "";

        // strip any more arguments
        splitIdx = prodID.IndexOf('&');
        if( splitIdx != -1 )
            prodID = prodID.Substring(0, splitIdx);

        // add to drop down list
        if( prodID != "" )
            AddToProductList("More Options...", prodID);

        return prodID != "";
    }

    public void SetupEventCallbacks()
    {

        string cmdToSync = "var cmdToSync = \"var elem = document.getElementById(\\\"\" + ev.data.s.attr(\"id\") + \"\\\"); if( elem != null && elem.style != null){elem.style.left=\\\"\" + ev.data.s.css(\"left\") + \"\\\";}\";";
        string cmd = "";
        cmd += "var sl = new Array();";
        cmd += "var slFunc = function(ev){" + cmdToSync + "UnityClient.Notify(\"SyncCmd\",cmdToSync);};";
        cmd += "for(var i=0; i < 100; ++i){ ";
        cmd +=    "sl[i] = $(\"#sl\" + i + \"slider\"); ";
        cmd +=    "if( sl[i].length ){ ";
        cmd +=        "sl[i].mouseup({s:sl[i]},slFunc);";
                        // hook up the plus + minus if they exist
        cmd +=        "var minus = $(\"#sl\" + i + \"minus\"); ";
        cmd +=        "if( minus.length ) ";
        cmd +=            "minus.mouseup({s:sl[i]},slFunc);";
        cmd +=        "var plus  = $(\"#sl\" + i + \"plus\"); ";
        cmd +=        "if( plus.length ) ";
        cmd +=            "plus.mouseup({s:sl[i]},slFunc);";
        cmd +=    "}else {";
        cmd +=        "break;";
        cmd +=    "}";
        cmd += "}";

        // bind the callback for changing tabs
        cmd += "$('.domtabs a').click(function(){UnityClient.Notify(\"ChangeTab\",this.href);});";
        bTex.ExecuteJavaScript(cmd);
    }

    private void OnLoadComplete(System.Object sender, System.EventArgs args)
    {
        if (urlOnLoad != "" && urlOnLoad != bTex.URL)
        {
            HandleNewURL(urlOnLoad);
            urlOnLoad = "";
            return;
        }
        
        if (tabOnLoad != "")
        {
            UpdateTab(tabOnLoad);
            tabOnLoad = "";
        }

        if (bTex.URL != url)
            UpdateServerWithNewURL(bTex.URL);
        url = bTex.URL;

        bTex.webCallbackHandler.RegisterNotificationCallback("ChangeTab", HandleChangeTab);
        SetupEventCallbacks();
    }

    public void HandleNewProduct()
    {
        if (!UpdateProductList())
            Refresh();
    }

    // argument is expected to be the current url with the tab specified as a hash
    private void HandleChangeTab(JSValue[] args)
    {
        if (args.Length < 2)
        {
            Debug.LogError("ChangeTab cmd requires at least 3 arguments: \"ChangeTab\" \"url\"");
            return;
        }
        UpdateServerWithNewURL(args[1].ToString());
    }

    public void UpdateTab(string newTab)
    {
        string cmd = "var currActive = $('.domtabs > .active a').attr(\"href\").substr(1); if( currActive == \"" + newTab + "\") return; var lis = document.getElementsByClassName(\"domtabs\")[0].children; for(var i=0; i<lis.length; i++){ if (lis[i].firstChild.href.search(\"" + newTab + "\") > 0){ lis[i].className=\"active\"; lis[i].firstChild.parentNode.parentNode.currentLink = lis[i].firstChild; lis[i].firstChild.parentNode.parentNode.currentSection = lis[i].firstChild.href.match(/#(\\w.+)/)[1];} else lis[i].className=\"\";} document.getElementById(currActive).parentNode.style.display = \"none\"; document.getElementById(\"" + newTab + "\").parentNode.style.display = \"block\";";
        bTex.ExecuteJavaScript(cmd);
    }

    public static void HandleRoomVariable(string varName, string url)
    {
        int id = -1;
        if (int.TryParse(varName.Substring(("prdurl").Length), out id))
        {
            ProductManagementScreen prodScreen = GetAll()[id];
            prodScreen.HandleNewURL(url);
        }
        else
            Debug.LogError("Bad id from " + varName);
    }

    // remove the hash character(#) to the end of the string
    private string StripHash(string urlStr)
    {
        int hashIdx = urlStr.IndexOf('#');
        return (hashIdx != -1) ? urlStr.Substring(0, hashIdx) : urlStr;
    }

    public void HandleNewURL(string newUrl)
    {
        if (newUrl == "")
            return;

        // if there is a hash change, change the tab
        int hashIdx = newUrl.IndexOf('#');
        string hash = (hashIdx != -1) ? newUrl.Substring(hashIdx + 1) : "";

        // did the non-hash url change 
        string nonhashURL = (hashIdx != -1) ? newUrl.Substring(0, hashIdx) : newUrl;

        // set initial url, also used to check if the server variable should be updated
        url = nonhashURL;
        if (bTex == null)
            tabOnLoad = hash;
        else if (bTex.isWebViewBusy())
        {
            // on load complete the url/tab changes will be applied
            urlOnLoad = nonhashURL;
            tabOnLoad = hash;
        }
        else
        {
            if (bTex.URL != nonhashURL)
                bTex.GoToURL(nonhashURL);
            else
            {
                if (hash != "")
                    UpdateTab(hash);
            }
        }  
    }

    public void UpdateServerWithNewURL(string url)
    {
        string rmVarName = "prdurl" + ServerID;
        List<RoomVariable> roomVariables = new List<RoomVariable>();
        roomVariables.Add(new SFSRoomVariable(rmVarName, url));
        CommunicationManager.SendMsg(new SetRoomVariablesRequest(roomVariables, CommunicationManager.LastValidRoom()));
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        allProdMgrs.Remove(ServerID);
    }
}
