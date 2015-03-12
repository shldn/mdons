using System;
using System.IO;
using UnityEngine;
using Awesomium.Mono;
using Awesomium.Unity;

public class PrivateBrowser : MonoBehaviour {
    HTMLPanelGUI guiPanel;

    bool browserTextureInitialized = false;
    int minPxBorderToLeave = 10;
    string urlOnRepaintComplete = "";

    public bool closeOnClickOff = true;
    public bool closeOnEsc = false;
    public static bool Active { get { return mInstance != null && mInstance.guiPanel != null && mInstance.guiPanel.Active; } }

    private static PrivateBrowser mInstance;
    public static PrivateBrowser Inst
    {
        get
        {
            if (mInstance == null)
                mInstance = (new GameObject("PrivateBrowser")).AddComponent(typeof(PrivateBrowser)) as PrivateBrowser;
            return mInstance;
        }
    }


    void Update()
    {
        if (!Active)
            return;
        if (!browserTextureInitialized)
            InitializeBrowserTexture();
        if (closeOnEsc && Input.GetKeyUp(KeyCode.Escape))
            SetActive(false);

        // Disable Click to move while overlays are active
        PlayerController.ClickToMoveInterrupt();
    }

    void InitializeBrowserTexture()
    {
        if (guiPanel.browserTexture == null)
            return;
        //guiPanel.Center();
        guiPanel.browserTexture.AddClickOutsideGUIBoundaryEventListener(OnClickOffOverlay);
        guiPanel.browserTexture.AddLoadCompleteEventListener(OnLoadComplete);

        browserTextureInitialized = true;
    }

    void CreateNewPanelGUI(int width, int height, string initUrl)
    {
        Hide();
        guiPanel = this.gameObject.AddComponent(typeof(HTMLPanelGUI)) as HTMLPanelGUI;
        guiPanel.width = width;
        guiPanel.height = height;
        guiPanel.initUrl = initUrl;
        guiPanel.useTransparentMaterial = false;
        guiPanel.browserId = CollabBrowserId.PRIVATEBROWSER;
        browserTextureInitialized = false;

        guiPanel.RegisterNotificationCallback("Send_Guilayer", delegate(JSValue[] args)
        {
            if(args.Length > 1)
                GameGUI.Inst.ExecuteJavascriptOnGui(args[1].ToString());
        });
    }

    public void SetURL(string url, int width, int height, int x = -1, int y = -1)
    {
        EnforceMaxDimensions(ref width, ref height);
        if (guiPanel == null || !guiPanel.Active || guiPanel.width != width || guiPanel.height != height)
            CreateNewPanelGUI(width, height, url);

        if (!browserTextureInitialized)
            InitializeBrowserTexture();

        guiPanel.SetURL(url);
        SetPosition(x, y);
        SetActive(true);
    }

    void EnforceMaxDimensions(ref int width, ref int height)
    {
        // enforce max size of overlay
        int maxHeight = Screen.height - minPxBorderToLeave;
        int maxWidth = Screen.width - minPxBorderToLeave;
        if (width > maxWidth || height > maxHeight)
        {
            int widthAmtOver = width - maxWidth;
            int heightAmtOver = height - maxHeight;
            if (widthAmtOver > heightAmtOver)
            {
                height = (int)((float)height / (float)width * (float)maxWidth); // preserves aspect ratio
                width = maxWidth;
            }
            else
            {
                width = (int)((float)width / (float)height * (float)maxHeight); // preserves aspect ratio
                height = maxHeight;
            }
        }
    }

    public void SetPosition(int x, int y)
    {
        if (guiPanel == null)
            return;
        guiPanel.Place(x, y);
    }

    public void SetRelativePosition(int x, int y)
    {
        if (guiPanel == null)
            return;
        guiPanel.Place(guiPanel.x + x, guiPanel.y + y);
    }

    private void SetActive(bool active)
    {
        if (guiPanel == null)
            return;
        guiPanel.SetActive(active);
    }

    public void Show()
    {
        SetActive(true);
    }

    public void Hide()
    {
        SetActive(false);
        Destroy(guiPanel);
        guiPanel = null;
    }

    void OnClickOffOverlay(System.Object sender, System.EventArgs args)
    {
        if(closeOnClickOff)
            Hide();
    }

    void OnLoadComplete(System.Object sender, System.EventArgs args)
    {
        if( guiPanel != null && guiPanel.browserTexture != null )
            GameGUI.Inst.guiLayer.UpdatePrivateBrowserURL(guiPanel.browserTexture.URL);
    }

    void OnDestroy()
    {
        mInstance = null;
        CollabBrowserTexture.GetAll().Remove(CollabBrowserId.PRIVATEBROWSER);
    }

    public bool IsMouseOverBrowser()
    {
        return guiPanel != null && guiPanel.browserTexture != null && guiPanel.browserTexture.GetPixelColorAtMousePosition().a > 0.0f;
    }
}


