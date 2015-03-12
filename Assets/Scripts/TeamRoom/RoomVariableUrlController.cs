using UnityEngine;
using System.Collections;
using Sfs2X.Entities.Variables;
using System.Collections.Generic;
using Sfs2X.Requests;
using Awesomium.Mono;
using System;
using Sfs2X.Core;
using Sfs2X.Entities;

public class RoomVariableUrlController : MonoBehaviour {

    private static Dictionary<string, RoomVariableUrlController> allRmVarControllers = new Dictionary<string, RoomVariableUrlController>();
    public static Dictionary<string, RoomVariableUrlController> GetAll() { return allRmVarControllers; }

    CollabBrowserTexture browserTexture = null;
    public string roomVariableName = "";
    private string newUrl = "";
    private bool updateServerOnLoadComplete = true; // don't update if the server told us which url to go to.
    private bool nextUpdateServerOnLoadComplete = true; // extra variable if a url is loading, save next state when it completes
    private int crashedCount = 0;

    // Accessors
    public string RoomVarName { 
        get { return roomVariableName; } 
        set {
            if (roomVariableName != "")
                allRmVarControllers.Remove(roomVariableName);
            roomVariableName = value;
            allRmVarControllers.Add(roomVariableName, this);
        }
    }
    public CollabBrowserTexture BrowserTexture
    {
        get { return browserTexture; }
        set
        {
            browserTexture = value;
            if (browserTexture != null)
            {
                browserTexture.LoadBegin += OnBeginLoading;
                browserTexture.Crashed += OnCrashed;
                browserTexture.AddLoadCompleteEventListener(OnLoadComplete);
            }
        }
    }

	void Start () {
        if (BrowserTexture == null)
            BrowserTexture = GetComponent<CollabBrowserTexture>();
	}


	void Update () {
        if (newUrl != "")
            SetNewURL(newUrl, nextUpdateServerOnLoadComplete);
	}


    void OnDestroy()
    {
        allRmVarControllers.Remove(roomVariableName);
    }

    void OnBeginLoading(System.Object sender, UrlEventArgs urlArgs)
    {
        if (GameManager.Inst.LocalPlayer != null && browserTexture.WriteAccessAllowed && updateServerOnLoadComplete)
            UpdateServerWithURL(urlArgs.Url);
        updateServerOnLoadComplete = true; // if a click produces the url change, we want it to update the server by default.
    }


    void OnLoadComplete(System.Object sender, EventArgs args)
    {
        crashedCount = 0;
    }


    private void OnCrashed(System.Object sender, EventArgs args)
    {
        if (crashedCount++ > 1)
        {
            InfoMessageManager.Display("The web screen has crashed, please try a different url");
            newUrl = "";
            return;
        }
        browserTexture.ReviveWebView();
    }


    public void SetNewURL(string url, bool updateServer)
    {
        if (url == "" || browserTexture == null)
            return;

        newUrl = url;
        nextUpdateServerOnLoadComplete = updateServer;

        // if webView is null it will load this, if it crashes it will load this on revive
        browserTexture.InitialURL = newUrl;

        if (browserTexture.webView == null)
        {
            newUrl = "";
            return;
        }

        if (!browserTexture.isWebViewBusy())
        {
            if (browserTexture.URL != newUrl)
            {
                updateServerOnLoadComplete = nextUpdateServerOnLoadComplete;
                if (!browserTexture.GoToURL(newUrl))
                    Debug.LogError("Problem navigating to new url");
            }
            if (browserTexture.webView.IsCrashed)
            {
                browserTexture.InitialURL = newUrl;
                browserTexture.ReviveWebView();
            }
            newUrl = "";
        }
        else
        {
            if (browserTexture.webView.IsCrashed)
            {
                Debug.LogError(newUrl + " Stopping nav for crashed " + crashedCount);
                browserTexture.ReviveWebView();
            }
        }
    }

    private void UpdateServerWithURL(string url)
    {
        List<RoomVariable> roomVariables = new List<RoomVariable>();
        roomVariables.Add(new SFSRoomVariable(roomVariableName, url));
        CommunicationManager.SendMsg(new SetRoomVariablesRequest(roomVariables, CommunicationManager.LastValidRoom()));
    }

    public static void HandleRoomVariableUpdate(BaseEvent evt)
    {
        Room room = (Room)evt.Params["room"];
        RoomVariableUrlController controller = null;

        ArrayList changedVars = (ArrayList)evt.Params["changedVars"];
        foreach (string varName in changedVars)
        {
            if (allRmVarControllers.TryGetValue(varName, out controller))
                controller.SetNewURL(room.GetVariable(varName).GetStringValue(), false);
        }
    }

    public static void HandleRoomVariableUpdate(UserVariable userVar)
    {
        RoomVariableUrlController controller = null;
        if (allRmVarControllers.TryGetValue(userVar.Name, out controller))
            controller.SetNewURL(userVar.GetStringValue(), false);
    }

    public static void HandleRoomJoin(BaseEvent evt)
    {
        if (evt == null)
            return;
        Room room = (Room)evt.Params["room"];
        foreach (RoomVariable rmVar in room.GetVariables())
            HandleRoomVariableUpdate(rmVar);
    }

}
