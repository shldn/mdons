using UnityEngine;
using System;

// WebPanel
// currently this handles focus options.
// Would be nice for this to handle the toolbar options and placement at some point.
public class WebPanel : MonoBehaviour {

    public CollabBrowserTexture browserTexture;
    public bool focusLossReleasesSnapCam = true;
    public bool focusGainEngagesSnapCam = false;
    public bool allowKeyInputOnZoom = false;
    public bool focusCameraOnInputFieldClick = true;
    public GameObject goToActivateOnFocus = null;
    private bool allowZoomInMsg = true;

    public CollabBrowserTexture BrowserTexture
    {
        set
        {
            browserTexture = value;
            if (browserTexture != null)
            {
                browserTexture.FocusChange += OnFocusChange;
                browserTexture.KeyEntryBlocked += OnKeyEntryBlocked;
                browserTexture.LoadComplete += OnLoadComplete;
                if( allowKeyInputOnZoom )
                    browserTexture.DynamicKeyInputAllowed = false;
                if (focusCameraOnInputFieldClick)
                {
                    browserTexture.webCallbackHandler.cmdToInjectOnLoadComplete = "var elemList = document.querySelectorAll('input[type=\"text\"],input[type=\"search\"]'); if(elemList != null){for(var i=0; i < elemList.length; ++i){elemList[i].onclick = function(){UnityClient.Notify(\"Zoom\");}}}";
                    browserTexture.webCallbackHandler.RegisterZoom();
                }
            }
        }
    }
	void Start () {
        if (browserTexture == null)
            BrowserTexture = gameObject.GetComponent<CollabBrowserTexture>();
        MainCameraController.Inst.ChangeCameraType += OnChangeCameraType;
        if (goToActivateOnFocus != null)
            goToActivateOnFocus.SetActive(false);
	}
	
    void OnFocusChange(System.Object sender, FocusEventArgs args)
    {
        bool focused = args.Focus;
        FocusManager.Inst.UpdateFocusID(browserTexture.id, focused, browserTexture.URL, GameManager.Inst.LevelLoaded != GameManager.Level.ORIENT && (!focused || GameManager.Inst.LevelLoaded != GameManager.Level.OFFICE || !GameGUI.Inst.allowPresenterTool), browserTexture.minWriteAccessType);

        if (!focused)
        {
            GameObject hitObj = MouseHelpers.GetCurrentGameObjectHit();
            if (hitObj != null && hitObj.GetComponent<WebToolBarItem>() != null && hitObj.GetComponent<SnapToScreenOnClick>() == null)
            {
                browserTexture.Focus();
                return;
            }
        }
        if (goToActivateOnFocus != null)
        {
            goToActivateOnFocus.SetActive(focused);
            if( focused && goToActivateOnFocus.name == "screenglow" )
            {
                Color c = new Color(0.286f, 0.32f, 1.0f, 0.5f);
                TeamWebScreen tws = GetComponent<TeamWebScreen>();
                if (tws != null && tws.IsParseControlled)
                    c = new Color(0.98823529411f, 1.0f, 0.16862745098f);
                if (browserTexture.minWriteAccessType > PlayerType.NORMAL && browserTexture.minWriteAccessType > GameManager.Inst.LocalPlayerType)
                    c = new Color(1.0f, 0.286f, 0.286f, 0.5f);
                    
                goToActivateOnFocus.GetComponent<Renderer>().material.SetColor ("_TintColor", c);
            }
        }

        bool snapCamInUse = MainCameraController.Inst.cameraType == CameraType.SNAPCAM;
        if (focusLossReleasesSnapCam && snapCamInUse && browserTexture.SnapCamOnMe && !focused)
            browserTexture.ToggleSnapCameraToObject();
        if ( focusGainEngagesSnapCam && !snapCamInUse && focused )
            browserTexture.ToggleSnapCameraToObject();
        allowZoomInMsg = true;
    }

    void OnKeyEntryBlocked(System.Object sender, KeyEventArgs args)
    {
        if (MainCameraController.Inst.cameraType != CameraType.SNAPCAM && !GameGUI.Inst.GuiLayerHasInputFocus && RemoteMouseManager.Inst.GetMyVisual().browserId == browserTexture.id)
        {
            bool isArrowKey = args.Key == KeyCode.UpArrow || args.Key == KeyCode.DownArrow || args.Key == KeyCode.RightArrow || args.Key == KeyCode.LeftArrow;
            if (allowZoomInMsg && !isArrowKey)
            {
                InfoMessageManager.Display("You must zoom in to enter text on this panel  <span id=zoom-link onclick=\"handleZoomRequest();\">Zoom Now</span>");
                allowZoomInMsg = false;
            }
        }
    }

    void OnChangeCameraType(System.Object sender, CameraChangeEventArgs args)
    {
        bool switchedToSnapCam = (args.newCameraType == CameraType.SNAPCAM);
        browserTexture.DynamicKeyInputAllowed = switchedToSnapCam;
        MainCameraController.Inst.playerMovementExitsSnapCam = !switchedToSnapCam;
        GameManager.Inst.playerManager.playerInputMgr.disableKeyPressMovement = switchedToSnapCam;
    }

    void OnLoadComplete(System.Object sender, EventArgs args)
    {
        if( browserTexture.Focused )
            FocusManager.Inst.UpdateFocusID(browserTexture.id, true, browserTexture.URL, GameManager.Inst.LevelLoaded != GameManager.Level.ORIENT && (!GameGUI.Inst.allowPresenterTool || GameManager.Inst.LevelLoaded != GameManager.Level.OFFICE), browserTexture.minWriteAccessType);
    }
}
