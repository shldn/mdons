using UnityEngine;
using System;
using Awesomium.Mono;
using System.Collections.Generic;
using Sfs2X.Requests;
using Sfs2X.Entities.Variables;

[RequireComponent(typeof(CollabBrowserTexture))]
[RequireComponent(typeof(MeshCollider))]
public class SlidePresenter : MonoBehaviour {

    public CollabBrowserTexture browserTexture;
    public bool enableScrollWheel = false; // can be changed in editor, when this was enabled we had issues with flickering slides
    public bool showMouseOnlyInTrigger = false;  // by default the mouse will always be shown, if this is set, it will only show users on stage.
    public GameObject goToActivateOnFocus = null;

    RoomVariableUrlController urlController;

    void Awake()
    {
        browserTexture = GetComponent<CollabBrowserTexture>();
        browserTexture.ShowMouseRepresentation = true;
        browserTexture.enableScrollWheel = enableScrollWheel;
        browserTexture.allowURLBroadcast = false;
        browserTexture.AddLoadCompleteEventListener(OnLoadComplete);
        browserTexture.ClickBlocked += OnClickBlocked;

        if (browserTexture.id == CollabBrowserId.NONE)
            browserTexture.id = CollabBrowserId.SLIDEPRESENT;

        if (CommunicationManager.redirectVideo)
        {
            browserTexture.redirectLoadingURL.Add("www.youtube.com", RedirectHelper.HandleYouTube);
            browserTexture.redirectLoadingURL.Add("vimeo.com", RedirectHelper.HandleVimeo);
        }

        WebPanel webPanel = gameObject.AddComponent<WebPanel>();
        webPanel.goToActivateOnFocus = goToActivateOnFocus;
        if (browserTexture.KeyInputEnabled)
            webPanel.allowKeyInputOnZoom = true;

        urlController = gameObject.AddComponent<RoomVariableUrlController>();
        urlController.RoomVarName = "purl";
        urlController.BrowserTexture = browserTexture;
    }
 

    void OnLoadComplete(System.Object sender, System.EventArgs args)
    {
        if (browserTexture.URL.IndexOf("docs.google.com") != -1)
            JSHelpers.HitDismissForGoogleDocOutOfDateWarning(browserTexture);
    }


    void OnClickBlocked(System.Object sender, System.EventArgs args)
    {
        if (GameManager.Inst.LevelLoaded == GameManager.Level.ORIENT)
        {
            if (browserTexture.AllowInputChanges && browserTexture.restrictedByTrigger)
            {
                if( !ReplayManager.Initialized || !ReplayManager.Inst.Playing )
                    InfoMessageManager.Display("Move onto the stage to click on screen");
            }
            else if (!browserTexture.AllowInputChanges)
                InfoMessageManager.Display("You do not have permission to click on this screen. :(");
        }
    }


    void Update()
    {
        if (showMouseOnlyInTrigger)
        {
            if (browserTexture.ShowMouseRepresentation && !GameGUI.Inst.allowPresenterTool)
                browserTexture.OnMouseExit();

            browserTexture.ShowMouseRepresentation = GameGUI.Inst.allowPresenterTool;
        }
    }

    public void SetNewURL(string url, bool updateServer)
    {
        urlController.SetNewURL(url, updateServer);
    }

}
