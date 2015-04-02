/********************************************************************************
 *    Project   : VirBELA
 *    File      : OverlayBrowserTexture.cs
 *    Version   : 
 *    Date      : 11/5/2012
 *    Author    : Erik Hill
 *    Copyright : UCSD
 *-------------------------------------------------------------------------------
 *
 *    Notes     :
 *
 *    Collaborative Browser based on WebViewTexture
 *    Navigations to new URLs are synced across clients
 *    
 ********************************************************************************/

using System;
using System.IO;
using UnityEngine;
using Awesomium.Mono;
using Awesomium.Unity;

public class OverlayBrowserManager : MonoBehaviour
{
    HTMLPanelGUI guiPanel;

    bool browserTextureInitialized = false;
    int minPxBorderToLeave = 10;
    string urlOnRepaintComplete = "";

    public bool Active { get { return guiPanel != null && guiPanel.Active; } }

    void Update()
    {
        if (!Active)
            return;
        if (!browserTextureInitialized)
            InitializeBrowserTexture();
        if (Input.GetKeyUp(KeyCode.Escape))
            SetActive(false);

        // Disable Click to move while overlays are active
        PlayerController.ClickToMoveInterrupt();
    }

    void InitializeBrowserTexture()
    {
        if (guiPanel.browserTexture == null)
            return;
        guiPanel.Center();
        guiPanel.browserTexture.AddClickOutsideGUIBoundaryEventListener(OnClickOffOverlay);

        // hacky, but if we see their car images, let's replace them with ours.
        SetupImageReplacements();
        browserTextureInitialized = true;
    }

    void CreateNewPanelGUI(int width, int height, string initUrl)
    {
        Hide();
        Destroy(guiPanel);
        guiPanel = null;
        guiPanel = this.gameObject.AddComponent(typeof(HTMLPanelGUI)) as HTMLPanelGUI;
        guiPanel.width = width;
        guiPanel.height = height;
        guiPanel.initUrl = initUrl;
        guiPanel.useTransparentMaterial = false;
        guiPanel.browserId = CollabBrowserId.OVERLAYBROWSER;
        browserTextureInitialized = false;
    }

    public void SetURL(string url, int width, int height, int x = -1, int y = -1)
    {
        EnforceMaxDimensions(ref width, ref height);
        if (guiPanel == null || guiPanel.width != width || guiPanel.height != height)
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

    void Center(int width, int height)
    {
        if (guiPanel.GetComponent<GUITexture>() == null)
            return;
        guiPanel.GetComponent<GUITexture>().pixelInset = new Rect(-width / 2, -height / 2, width, height);
    }

    void SetPosition(int x, int y)
    {
        guiPanel.Place(x, y);
    }

    private void SetActive(bool active)
    {
        if (guiPanel == null)
            return;
        guiPanel.SetActive(active);
        if (!active && guiPanel.browserTexture != null)
        {
            guiPanel.browserTexture.ClearTexture(); // clears the texture, but when set active it will render the last url until the new one loads
            guiPanel.SetURL(""); // make the "last" url blank
        }
        if( GameManager.Inst.LevelLoaded == GameManager.Level.BIZSIM )
            BizSimManager.Inst.EnableInputOnAllBrowserPlanes(!active);
        CollabBrowserTexture.disableAll3DMouseClicks = active;
    }

    public void Hide()
    {
        SetActive(false);
    }

    void OnClickOffOverlay(System.Object sender, System.EventArgs args)
    {
        Hide();
    }

    void OnResizeComplete(System.Object sender, ResizeEventArgs e)
    {
        Debug.LogError("OnResizeComplete");
        if (urlOnRepaintComplete != "")
        {
            Debug.LogError("OnResizeComplete: " + urlOnRepaintComplete);
            guiPanel.width = e.Width;
            guiPanel.height = e.Height;
            guiPanel.browserTexture.Width = e.Width;
            guiPanel.browserTexture.Height = e.Height;
            guiPanel.SetURL(urlOnRepaintComplete);
            SetActive(true);
        }
    }

    void SetupImageReplacements()
    {
        if (guiPanel.browserTexture == null)
            return;
        guiPanel.browserTexture.requestReplacements.Add(BizSimScreen.BaseURL + "/images/upload/custom/greencar/automotive-mini.jpg", Directory.GetCurrentDirectory() + "/img/automotive-mini.png");
        guiPanel.browserTexture.requestReplacements.Add(BizSimScreen.BaseURL + "/images/upload/custom/greencar/automotive-small.jpg", Directory.GetCurrentDirectory() + "/img/automotive-small.png");
        guiPanel.browserTexture.requestReplacements.Add(BizSimScreen.BaseURL + "/images/upload/custom/greencar/automotive-compact.jpg", Directory.GetCurrentDirectory() + "/img/automotive-compact.png");
        guiPanel.browserTexture.requestReplacements.Add(BizSimScreen.BaseURL + "/images/upload/custom/greencar/automotive-mid-size.jpg", Directory.GetCurrentDirectory() + "/img/automotive-midsize.png");
        guiPanel.browserTexture.requestReplacements.Add(BizSimScreen.BaseURL + "/images/upload/custom/greencar/automotive-luxury.jpg", Directory.GetCurrentDirectory() + "/img/automotive-luxury.png");
    }
}

