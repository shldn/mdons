using UnityEngine;
using Boomlagoon.JSON;
using System.Collections.Generic;

[RequireComponent(typeof(CollabBrowserTexture))]
public class TeamWebScreen : MonoBehaviour {

    private static List<TeamWebScreen> screens = new List<TeamWebScreen>();
    public static List<TeamWebScreen> GetAll() { return screens; }

    public int id = 1;
    public GameObject goToActivateOnFocus = null;
    private bool parseDBControlled = false;
    public string rmVarOverride = "";
    CollabBrowserTexture bTex = null;
    public bool IsParseControlled { get { return parseDBControlled; } }
    void Awake () {

        // hack to deal with notes screen replacement -- fix me please
        if (GameManager.Inst.LevelLoaded == GameManager.Level.TEAMROOM)
        {
            if (id == 5 && CommunicationManager.parseScreens.Contains(CollabBrowserId.TEAMNOTES))
            {
                gameObject.SetActive(false);
                return;
            }
        }

        if (GameManager.Inst.LevelLoaded == GameManager.Level.TEAMROOM && CommunicationManager.parseScreens.Contains(id))
            parseDBControlled = true;


        if (parseDBControlled)
        {
            string url = "";
            if( GetURLFromParse(ref url) )
            {
                InitBrowserTexture();
                SetToURL(url);
            }
            else
                gameObject.SetActive(false);
        }
        else
        {
            InitBrowserTexture();

            RoomVariableUrlController controller = gameObject.AddComponent<RoomVariableUrlController>();
            controller.RoomVarName = (rmVarOverride == "") ? ("url" + id) : rmVarOverride;
            controller.BrowserTexture = bTex;
        }

        WebPanel wp = gameObject.AddComponent<WebPanel>();
        wp.allowKeyInputOnZoom = true;
        wp.goToActivateOnFocus = goToActivateOnFocus;

        screens.Add(this);
	}

    void SetToURL(string url)
    {
        if (bTex.webView == null)
            bTex.InitialURL = url;
        else
            bTex.GoToURL(url);
    }

    bool GetURLFromParse(ref string url)
    {
        string panelUrlField = "panelUrl" + id;
        string newURL = ParseRoomVarManager.Inst.GetRoomVal(Application.loadedLevelName, panelUrlField);

        bool validStr = !string.IsNullOrEmpty(newURL);
        if (validStr && !newURL.StartsWith("http"))
        {
            // grab variable from Team definition.
            // Assume newURL is the variable name to grab from the team class
            newURL = TeamInfo.Inst.TeamExists(CommunicationManager.CurrentTeamID) ? TeamInfo.Inst.GetVariable(CommunicationManager.CurrentTeamID, newURL) : "";
            validStr = !string.IsNullOrEmpty(newURL) && newURL.StartsWith("http");
            if (!validStr && newURL != null && newURL.Trim() != "")
            {
                newURL = WebStringHelpers.CreateValidURLOrSearch(newURL);
                validStr = true;
            }
        }
        if (validStr)
            url = newURL;
        return validStr;
    }

    void InitBrowserTexture()
    {
        bTex = GetComponent<CollabBrowserTexture>();
        bTex.AddLoadCompleteEventListener(OnLoadComplete);
        if (bTex.id == CollabBrowserId.NONE)
            bTex.id = CollabBrowserId.TEAMWEB + id;

        if (GameManager.Inst.ServerConfig == "LocalTest" && id == 6)
        {
            VDebug.LogError("UCI Hack for id 6 -- making id slide presenter for backwards compatiblity");
            bTex.id = CollabBrowserId.SLIDEPRESENT;
            return;
        }

        if (CommunicationManager.redirectVideo)
        {
            bTex.redirectLoadingURL.Add("www.youtube.com", RedirectHelper.HandleYouTube);
            bTex.redirectLoadingURL.Add("vimeo.com", RedirectHelper.HandleVimeo);
        }

        // lock down team room screens, open them up on the guilayer side.
        if (GameManager.Inst.ServerConfig != "UCI")
        {
            bTex.minWriteAccessType = PlayerType.LEADER;
        }
    }
    
    void OnLoadComplete(System.Object sender, System.EventArgs args)
    {
        if (bTex.URL.IndexOf("docs.google.com") != -1)
            JSHelpers.HitDismissForGoogleDocOutOfDateWarning(bTex);
        if (bTex.URL.IndexOf("twiddla.com") != -1)
        {
            JSHelpers.EnterTwiddlaUserName(bTex, GameManager.Inst.LocalPlayer.Name);
            JSHelpers.CloseTwiddlaSideNav(bTex);
        }

        // auto zoom in
        WebPanel wp = gameObject.GetComponent<WebPanel>();
        wp.focusGainEngagesSnapCam = (bTex.URL.IndexOf("docs.google.com/document") != -1 || bTex.URL.IndexOf("docs.google.com/spreadsheet") != -1);

    }

    void OnDestroy()
    {
        screens.Remove(this);
    }

    public void ReloadParseURL()
    {
        if (parseDBControlled)
        {
            string url = "";
            if (GetURLFromParse(ref url))
                SetToURL(url);
        }
    }

}
