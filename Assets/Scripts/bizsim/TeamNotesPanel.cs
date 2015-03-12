using UnityEngine;
using Awesomium.Mono;
using System.Collections.Generic;

[RequireComponent(typeof(MeshCollider))]
public class TeamNotesPanel : MonoBehaviour {

    private static List<TeamNotesPanel> notes = new List<TeamNotesPanel>();
    public static List<TeamNotesPanel> GetAll() { return notes; }

    private CollabBrowserTexture browserTexture;
    private string defaultNotesURL = "https://docs.google.com/document/d/1KFgh4y3F6-VP2tMfJpghgtz47iMce1ED6mOJ3aAwiPM/edit";
    public int pixelWidth = 706; // add 24 pixels for scroll bar
    protected int pixelHeight = 500; // overwritten when useScaleForPixelDimensions is turned on.
    public bool useScaleForPixelDimensions = true;
    public GameObject activeOnFocusObj = null; // when focused this object becomes active, when focus lost this object becomes inactive
    private bool focused = false;
    private bool minimizeMenu = true;

    void Awake() {
        if (GameManager.Inst.LevelLoaded == GameManager.Level.TEAMROOM && !CommunicationManager.parseScreens.Contains(CollabBrowserId.TEAMNOTES))
        {
            gameObject.SetActive(false);
            return;
        }
        notes.Add(this);
    }

    void Start()
    {
        int teamID = CommunicationManager.CurrentTeamID;
        if (useScaleForPixelDimensions)
            pixelHeight = PanelHelpers.GetPxHeightFromTransform(this.transform, pixelWidth); 
        browserTexture = gameObject.AddComponent<CollabBrowserTexture>();
        browserTexture.useTransparentMaterial = false;
        browserTexture.Width = pixelWidth;
        browserTexture.Height = pixelHeight;
        browserTexture.id = CollabBrowserId.TEAMNOTES;
        browserTexture.InitialURL = GetNotesURL(teamID);
        browserTexture.ShowMouseRepresentation = (GetComponent<Billboard>() == null || !GetComponent<Billboard>().enabled); // sphere's don't currently move with the billboard controlled orientation
        browserTexture.FocusChange += OnFocusChange;
        browserTexture.AddLoadCompleteEventListener(OnLoadComplete);
        if (activeOnFocusObj != null)
            activeOnFocusObj.SetActive(false); // initialized assuming screen doesn't have focus.

        // shouldn't need these, but in case user puts video url in notes panel.
        if (CommunicationManager.redirectVideo)
        {
            browserTexture.redirectLoadingURL.Add("www.youtube.com", RedirectHelper.HandleYouTube);
            browserTexture.redirectLoadingURL.Add("vimeo.com", RedirectHelper.HandleVimeo);
        }

        WebPanel webPanel = gameObject.AddComponent<WebPanel>();
        webPanel.focusGainEngagesSnapCam = true;
        webPanel.focusCameraOnInputFieldClick = false; // already handled on focus

    }

    string GetNotesURL(int teamID)
    {
        return TeamInfo.Inst.TeamExists(teamID) ? WebStringHelpers.CreateValidURLOrSearch(TeamInfo.Inst.GetNotesURL(teamID)) : defaultNotesURL;
    }

    void OnFocusChange(System.Object sender, FocusEventArgs args)
    {
        focused = args.Focus;
        GameGUI.Inst.AllowConsoleToTakeEnterFocus = !focused;
        MainCameraController.Inst.playerMovementExitsSnapCam = !focused;
        if (activeOnFocusObj != null)
            activeOnFocusObj.SetActive(args.Focus);
    }

    void OnLoadComplete(System.Object sender, System.EventArgs args)
    {
        if (!minimizeMenu)
            return;

        JSHelpers.MinimizeGoogleDocMenus(browserTexture);

        JSHelpers.HitDismissForGoogleDocOutOfDateWarning(browserTexture);
    }

    void Update()
    {
        if (browserTexture != null && browserTexture.Focused)
        {
            // handle the case where the user hits esc to exit SnapCam mode
            if (MainCameraController.Inst.cameraType != CameraType.SNAPCAM)
                browserTexture.Unfocus();

            PlayerManager.playerController.lockMovement = true;
        }
    }

    public void Refresh()
    {
        if (browserTexture != null && !browserTexture.isWebViewBusy())
            browserTexture.GoToURL(GetNotesURL(CommunicationManager.CurrentTeamID));
    }

    void OnDestroy()
    {
        notes.Remove(this);
    }
}
