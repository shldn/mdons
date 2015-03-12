/********************************************************************************
 *    Project   : VirBELA
 *    File      : CollabBrowserTexture.cs
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

#region Using
using System;
using UnityEngine;
using Awesomium.Mono;
using Awesomium.Unity;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Core;
using Sfs2X.Requests;
#endregion

public class FocusEventArgs : EventArgs
{
    public FocusEventArgs(bool focus_) { focus = focus_; }
    private bool focus;
    public bool Focus { get { return focus; } }
}
public class KeyEventArgs : EventArgs
{
    public KeyEventArgs(KeyCode key_) { key = key_; }
    private KeyCode key;
    public KeyCode Key { get { return key; } }
}

public class CollabBrowserTexture : MonoBehaviour
{
    #region Fields
    // Public Variables
    [SerializeField()]
    public int width = 512;
    [SerializeField()]
    public int height = 512;
    [SerializeField()]
    private string initialURL = "about:blank";
    private string initialHTML = "hello world!";
    [SerializeField()]
    public bool allowURLBroadcast = true;
    [SerializeField()]
    private bool disableKeyInput = false;
    private bool dynamicAllowKeyInput = true;

    // Internal Variables
    private bool isFocused = false;
    private bool isScrollable = false;
    private bool forceNextConfirm = false;
    private bool goingBack = false;
    private bool goingForward = false;
    private bool snapCamOnMe = false;
    public WebView webView;
    private Texture2D texture;
    private Color32[] Pixels;
    private GCHandle PixelsHandle;
    private GUITexture gui = null; // if this CollabBrowserTexture is a gui texture, gui will be non null
    private string lastURL = "";
    public int id = CollabBrowserId.NONE;                  // unique id, used to identify for messages across clients
    private bool displayWebViewErrors = true;

    private static float minDistanceToReportPosChange = 0.0f; // controls how often mouse updates are sent over the network, Erik added
    private static Dictionary<int, CollabBrowserTexture> allBrowserTextures = new Dictionary<int, CollabBrowserTexture>();
    public static bool disableAll3DMouseClicks = false; // global way to turn off clicking on all non-gui collabBrowserTextures.

    public List<string> blacklistRequestURLFragments = new List<string>(); // add the fragments of urls that should be blacklisted. Eg: "holdinginfo.php?playerid" will allow http://baseurl/holdinginfo.php thru, but any holdinginfo.php?playerid=somenumber will be blocked.
    public Dictionary<string, Func<string,string>> redirectLoadingURL = new Dictionary<string, Func<string,string>>(); // if the url is requested, it will instead go to Func(url)
    public Dictionary<string, string> requestReplacements = new Dictionary<string, string>(); // replacements for http requests. Eg. A webpage wants to load image.jpg, we redirect it to C:/myimage.jpg or http://virbela.com/myimage.jpg
    public Dictionary<string, string> supportedDialogMessages = new Dictionary<string, string>(); // dialog message and cooresponding action on confirmation.
    public string jsOnLoadComplete = "";  // Execute this javascript when the page finishes loading.
    private Vector3 lastMousePosition = Vector3.zero;
    private bool lastMouseDown = false;
    private GameObject toggleSnapCamHelperGO = null;
    public bool loadInitialHtmlInsteadOfURL = false;
    public bool enableBgRendering = false;
    public bool useTransparentMaterial = true;
    public bool enableScrollWheel = true;
    public bool useCollabFeatures = true; // work around bool before we restructure this class, don't give warnings for pages that don't need collab features.
    private bool isLoaded = false;
    private bool allowURLChanges = true;
    private bool allowInputChanges = true;
    public bool restrictedByTrigger = false;
    private bool showMouse = true;
    private bool showingLoadingTexture = false;

    private int lastMouseButtonDown = -1;
    private bool wasLastMouseDownObj = false;
    private Texture2D loadingTexture = null;
    public bool forceShowLoadingTexture = false;
    public PlayerType minWriteAccessType = PlayerType.NORMAL; // players must have at least these rights to inject input into the page.
    public WebCallbackHandler webCallbackHandler = null;

    private Shader defaultShader = null; 
    public Material refreshMat = null;

    #endregion

    #region Accessors
    public string InitialURL { get { return initialURL; } set { initialURL = value; } }
    public int Width { get { return width; } set { width = value; } }
    public int Height { get { return height; } set { height = value; } }
    public bool Enabled { get { return CheckWebView(); } }
    public bool IsLoaded { get { return isLoaded; } }
    public bool AllowURLChanges { get { return allowURLChanges; } set { allowURLChanges = value; } }
    public bool AllowSnapToScreen { get; set; }
    public bool AllowInputChanges { get { return allowInputChanges && WriteAccessAllowed; } set { allowInputChanges = value; } }
    public bool WriteAccessAllowed { get { return (GameManager.Inst.LocalPlayer == null || (int)minWriteAccessType <= (int)GameManager.Inst.LocalPlayer.Type); } }
    public bool ShowMouseRepresentation { get { return gui == null && showMouse; } set { showMouse = value; } }
    public bool KeyInputEnabled { get { return !disableKeyInput; } set { disableKeyInput = !value; } }
    public bool DynamicKeyInputAllowed { get { return dynamicAllowKeyInput; } set { dynamicAllowKeyInput = value; } }
    public string URL { get { if (CheckWebView()) { return webView.Source.AbsoluteUri; } else { return ""; } } }
    public bool Focused { get { return isFocused; } }
    public bool SnapCamOnMe { get { return snapCamOnMe; } }
    public Texture2D LoadingTexture
    {
        set
        {
            loadingTexture = value;
            if (refreshMat == null)
                InitRefreshMaterial();
            refreshMat.SetTexture("_MainTex", loadingTexture);
        }
    }
    #endregion

    public static Dictionary<int, CollabBrowserTexture> GetAll() { return allBrowserTextures; }

    // event and delegate to propogate webview's LoadComplete event.
    public delegate void LoadCompleteEventHandler(object sender, EventArgs e);
    public event LoadCompleteEventHandler LoadComplete;

    // event and delegate to propogate webview's BeginLoading event.
    public delegate void LoadBeginEventHandler(object sender, UrlEventArgs e);
    public event LoadBeginEventHandler LoadBegin;

    // event and delegate to propogate webview's Crashed event.
    public delegate void CrashedEventHandler(object sender, EventArgs e);
    public event CrashedEventHandler Crashed;

    // event that fires when the user is attempting to navigate to a new page
    public delegate void URLRequestEventHandler(object sender, UrlEventArgs e);
    public event URLRequestEventHandler URLChangeRequest;

    // event that fires for gui browser textures when the user clicks outside off the texture boundary displayed
    public delegate void ClickOutsideGUIBoundaryEventHandler(object sender, EventArgs e);
    public event ClickOutsideGUIBoundaryEventHandler ClickOutsideGUIBoundary;

    // event that fires for gui browser textures when the user clicks outside off the texture boundary displayed
    public delegate void FocusEventHandler(object sender, FocusEventArgs e);
    public event FocusEventHandler FocusChange;

    // event that fires if key input is dynamically blocked and the user hits an input key
    public delegate void KeyEntryBlockedHandler(object sender, KeyEventArgs e);
    public event KeyEntryBlockedHandler KeyEntryBlocked;

    // event that fires if mouse click is blocked
    public delegate void MouseClickBlockedHandler(object sender, EventArgs e);
    public event MouseClickBlockedHandler ClickBlocked;

    public void AddLoadCompleteEventListener(LoadCompleteEventHandler handler)
    {
        LoadComplete += handler;
    }

    public void AddURLChangeRequestEventListener(URLRequestEventHandler handler)
    {
        URLChangeRequest += handler;
    }

    public void AddClickOutsideGUIBoundaryEventListener(ClickOutsideGUIBoundaryEventHandler handler)
    {
        ClickOutsideGUIBoundary += handler;
    }

    #region Methods
    private bool CheckWebView()
    {
        bool ret = WebCore.IsRunning && (webView != null) && webView.IsEnabled;
        if (!ret && displayWebViewErrors)
        {
            VDebug.Log("Webview Failed!!\nWebCore.IsRunning: " + WebCore.IsRunning);
            if (webView != null)
            {
                VDebug.Log("webView: " + webView.ToString());
                VDebug.Log("webView.IsEnabled: " + webView.IsEnabled.ToString());
            }
            displayWebViewErrors = WebCore.IsRunning;
        }
        return ret;
    }
    #endregion

    #region Overrides
    public void Awake()
    {
        webCallbackHandler = new WebCallbackHandler(this);
    }

    public void Start()
    {
        if( useCollabFeatures )
        {
            if (id == CollabBrowserId.NONE || allBrowserTextures.ContainsKey(id))
                Debug.LogError("For syncing to work, CollabBrowserTexture id's must be unique, please assign a unique id != " + id);
            else
                allBrowserTextures.Add(id, this);
        }

        AllowSnapToScreen = true;

        // WebCoreInitializer.Awake() initializes the WebCore
        // before any Start() function on any script is called.
        // We create a web-view here.
        InitWebView();

        // Prepare and a assign a texture to the component.
        // The texture will display the pixel buffer of the WebView.
        bool mipmap = false;
        texture = new Texture2D(width, height, useTransparentMaterial ? TextureFormat.RGBA32 : TextureFormat.RGB24, mipmap);
        Pixels = texture.GetPixels32(0);
        PixelsHandle = GCHandle.Alloc(Pixels, GCHandleType.Pinned);

        if (renderer)
        {
            renderer.material.mainTexture = texture;
            string matName = useTransparentMaterial ? "Unlit/Transparent" : "Unlit/Texture";
            defaultShader = Shader.Find(matName);
            renderer.material.shader = defaultShader;
        }
        else if (GetComponent(typeof(GUITexture)))
        {
            gui = GetComponent(typeof(GUITexture)) as GUITexture;
            gui.texture = texture;
        }
        else
            Debug.LogError("Game Object has no Material or GUI Texture, we cannot render a web-page to this object!");

        // Grab materials
        InitRefreshMaterial();
    }

    private void InitWebView()
    {
        webView = WebViewManager.Inst.CreateWebView(width, height);
        webView.FlushAlpha = false;
        webView.IsTransparent = useTransparentMaterial;

        if (!loadInitialHtmlInsteadOfURL)
            webView.LoadURL(initialURL);
        else
            webView.LoadHTML(initialHTML);

        // Handle some important events.
        webView.OpenExternalLink += OnWebViewOpenExternalLink;
        webView.ShowJavascriptDialog += OnJavascriptDialog;
        webView.LoginRequest += OnLoginRequest;
        webView.BeginLoading += OnBeginLoading;
        webView.LoadCompleted += OnLoadCompleted;
        webView.ResourceRequest += OnResourceRequest;
        webView.ScrollDataReceived += OnScrollDataReceived;
        webView.Download += OnDownloadRequest;
        webView.Crashed += OnCrashed;
        //webView.JSConsoleMessageAdded += OnConsoleMessageAdded;
    }

    // Destroy and Re-create the webView
    public void ReviveWebView()
    {
        WebViewManager.Inst.CloseWebView(webView);
        InitWebView();
    }

    private void InitRefreshMaterial()
    {
        refreshMat = Resources.Load("Materials/Browser_Refreshing", typeof(Material)) as Material;
    }

    private void OnEnable()
    {
        if (!CheckWebView())
            return;
        webView.IsRendering = true;
    }

    private void OnDisable()
    {
        if (!CheckWebView())
            return;

        if (!enableBgRendering)
            webView.IsRendering = false;
    }

    public void Focus()
    {
        if (!CheckWebView())
            return;

        webView.Focus();
        bool focusChanged = !isFocused;
        isFocused = true;
        if (focusChanged)
            RaiseFocusChangeEvent(isFocused);

        // Set gui presenter tool browser to this one.
        if (GameGUI.Inst.guiLayer != null && this != GameGUI.Inst.guiLayer.htmlPanel.browserTexture && this.gui == null)
        {
            GameGUI.Inst.presenterToolCollabBrowser = this;
            GameGUI.Inst.presenterURL = URL;
        }
    }

    public void Unfocus()
    {
        if (!CheckWebView())
            return;

        webView.Unfocus();
        bool focusChanged = isFocused;
        isFocused = false;
        if (focusChanged)
            RaiseFocusChangeEvent(isFocused);
    }

    private void SetToLoadingTexture()
    {
        if (loadingTexture == null)
            return;
        renderer.material = refreshMat;
        showingLoadingTexture = true;
    }

    private void SetToWebViewRender()
    {
        // Set material to the webview texture.
        if (renderer){
            renderer.material.mainTexture = texture;
            renderer.material.shader = defaultShader;
        }
        else if (GetComponent(typeof(GUITexture))){
            gui = GetComponent(typeof(GUITexture)) as GUITexture;
            gui.texture = texture;
        }

        Awesomium.Mono.RenderBuffer rBuffer = webView.Render();

        if (rBuffer != null){
            Utilities.DrawBuffer(rBuffer, ref texture, ref Pixels, ref PixelsHandle);
            showingLoadingTexture = false;
        }
    }

    private void Update()
    {
        if (!CheckWebView())
        {
            if (!showingLoadingTexture && loadingTexture != null)
                SetToLoadingTexture();
            return;
        }

        if (loadingTexture != null && (webView.IsLoadingPage || forceShowLoadingTexture))
        {
            if (!showingLoadingTexture)
                SetToLoadingTexture();
        }
        else if (webView.IsDirty)
        {
            SetToWebViewRender();
        }


        if (Input.GetMouseButtonDown(0))
            //Debug.Log("Pressed left click.");
            lastMouseButtonDown = 0;
        if (Input.GetMouseButtonDown(1))
            //Debug.Log("Pressed right click.");
            lastMouseButtonDown = 2;
        if (Input.GetMouseButtonDown(2))
            //ebug.Log("Pressed middle click.");
            lastMouseButtonDown = 1;
    }

    private void OnDestroy()
    {
        allBrowserTextures.Remove(id);
        if (CheckWebView())
        {
            // Free the pinned array handle.
            PixelsHandle.Free();

            if (WebCore.IsRunning)
            {
                WebViewManager.Inst.CloseWebView(webView);
                webView = null;

                VDebug.Log("Destroyed View");
            }
        }
    }

    private void OnApplicationQuit()
    {
        if (CheckWebView())
            Destroy(this);
    }

    public bool GoToURL(string newURL)
    {
        if (!CheckWebView())
            return false;
        if (!webView.LoadURL(newURL))
            Debug.LogError("Setting url failed: " + newURL);
        return true;
    }

    public void LoadHTML(string html)
    {
        if (!CheckWebView())
            return;
        if (!webView.LoadHTML(html))
            Debug.LogError("LoadHTML failed: " + html);
    }

    public void LoadFile(string filename)
    {
        if (!CheckWebView())
            return;
        bool res = webView.LoadFile(filename);
        if (!res)
            Debug.LogError("LoadFile failed: " + filename);
    }

    public bool isWebViewBusy()
    {
        return webView.IsLoadingPage || webView.IsNavigating;
    }

    public void RefreshWebView()
    {
        if (!CheckWebView())
            return;
        webView.Reload();
        isLoaded = false;
    }

    // executes asyncronously
    public void ExecuteJavaScript(string cmds)
    {
        if (CheckWebView())
            webView.ExecuteJavascript(cmds);
    }

    // executes syncronously
    public JSValue ExecuteJavaScriptWithResult(string cmds, int timeoutMs = 2000)
    {
        if (!CheckWebView())
            return null;
        return webView.ExecuteJavascriptWithResult(cmds, timeoutMs);
    }

    public bool IsNetworkConnectionEstablished()
    {
        return CommunicationManager.IsConnected && CommunicationManager.InASmartFoxRoom;
    }

    private void OnBeginLoading(System.Object sender, Awesomium.Mono.BeginLoadingEventArgs args)
    {
        if (args.Url.Length < 1)
            return;

        if (IsBlackListedLoad(args.Url))
        {
            webView.GoBack();
            return;
        }

        if (HandleRedirectLoad(args.Url))
            return;

        SetToLoadingTexture();
        if (args.Url != lastURL)
        {
            lastURL = args.Url;
            RaiseLoadBeginEvent(args.Url);
        }
        else
            VDebug.Log("skipping url: " + args.Url);
        goingBack = false;
        goingForward = false;
    }

    private Awesomium.Mono.ResourceResponse OnResourceRequest(System.Object sender, ResourceRequestEventArgs args)
    {
        if (IsBlackListedRequest(args.Request.Url))
        {
            RaiseURLChangeRequestEvent(args.Request.Url);
            args.Request.Cancel();
        }

        if (requestReplacements.ContainsKey(args.Request.Url))
            return new Awesomium.Mono.ResourceResponse(requestReplacements[args.Request.Url]);

        return new Awesomium.Mono.ResourceResponse(args.Request.Url);
    }

    // note: Blacklisted request is different than blacklisted load, this is any resource that is requested to load a particular url
    protected bool IsBlackListedRequest(string url)
    {
        return (lastURL != "" && lastURL != url && blacklistRequestURLFragments.Count > 0 && blacklistRequestURLFragments.FindLastIndex(delegate(string str) { return url.LastIndexOf(str) != -1; }) != -1);
    }

    // note: Blacklisted load is different than blacklisted request, this is the actual url trying to be loaded
    protected bool IsBlackListedLoad(string url)
    {
        return (!AllowURLChanges && lastURL != "" && lastURL != url);
    }

    // returns true if it was redirected
    protected bool HandleRedirectLoad(string url)
    {
        foreach (KeyValuePair<string, Func<string, string>> redirectPair in redirectLoadingURL)
        {
            if (url.IndexOf(redirectPair.Key) != -1)
            {
                string newURL = redirectPair.Value(url);
                if (newURL != url)
                {
                    // If we got here by going back, go back one more.
                    if (goingBack)
                        GoBack();
                    else if (goingForward)
                        GoForward();
                    else
                        GoToURL(newURL);
                    return true;
                }
            }
        }
        return false;
    }

    public void GoBack()
    {
        goingBack = true;
        webView.GoBack();
    }

    public void GoForward()
    {
        goingForward = true;
        webView.GoForward();
    }

    private void OnScrollDataReceived(System.Object sender, ScrollDataEventArgs e)
    {
        Debug.LogError("sx: " + e.ScrollData.ScrollX + " sy: " + e.ScrollData.ScrollY);
    }

    private void OnCrashed(System.Object sender, EventArgs e)
    {
        Debug.LogError("Collab Browser has crashed: " + e.ToString());
        isFocused = false;
        RaiseCrashedEvent();
    }

    private void OnDownloadRequest(System.Object sender, UrlEventArgs args)
    {
        Debug.Log("Download request: " + args.Url);
        GameGUI.Inst.HandleDownloadRequest(args.Url);
    }

    private void OnLoadCompleted(System.Object sender, System.EventArgs args)
    {
        isLoaded = true;
        forceNextConfirm = false;
        RaiseLoadCompleteEvent(args);
        SetToWebViewRender();
        if (URL.EndsWith(".pdf"))
            GameGUI.Inst.HandleDownloadRequest(URL);
        if (jsOnLoadComplete != "")
        {
            ExecuteJavaScript(jsOnLoadComplete);
            jsOnLoadComplete = "";
        }
    }

    private void RaiseClickOutsideGUIBoundary()
    {
        try
        {
            if (ClickOutsideGUIBoundary != null)
                ClickOutsideGUIBoundary(this, new System.EventArgs());
        }
        catch
        {
            Debug.Log("Exception raised throwing ClickOutsideGUIBoundary event");
        }
    }

    private void RaiseLoadCompleteEvent(System.EventArgs args)
    {
        try
        {
            if (LoadComplete != null)
                LoadComplete(this, args);
        }
        catch
        {
            Debug.Log("Exception raised throwing LoadCompleted event");
        }
    }

    private void RaiseCrashedEvent()
    {
        try
        {
            if (Crashed != null)
                Crashed(this, new System.EventArgs());
        }
        catch
        {
            Debug.Log("Exception raised throwing Crashed event");
        }
    }

    private void RaiseFocusChangeEvent(bool focus)
    {
        try
        {
            if (FocusChange != null)
                FocusChange(this, new FocusEventArgs(focus));
        }
        catch
        {
            Debug.Log("Exception raised throwing LoadBegin event");
        }
    }

    private void RaiseLoadBeginEvent(string url)
    {
        try
        {
            if (LoadBegin != null)
                LoadBegin(this, new UrlEventArgs(url));
        }
        catch
        {
            Debug.Log("Exception raised throwing LoadBegin event");
        }
    }

    private void RaiseURLChangeRequestEvent(string url)
    {
        try
        {
            if (URLChangeRequest != null)
                URLChangeRequest(this, new UrlEventArgs(url));
        }
        catch
        {
            Debug.Log("Exception raised throwing URLChangeRequest event");
        }
    }

    private void RaiseKeyEntryBlocked(KeyCode keyBlocked)
    {
        try
        {
            if (KeyEntryBlocked != null)
                KeyEntryBlocked(this, new KeyEventArgs(keyBlocked));
        }
        catch
        {
            Debug.Log("Exception raised throwing KeyEntryBlocked event");
        }
    }

    private void RaiseClickBlocked()
    {
        try
        {
            if (ClickBlocked != null)
                ClickBlocked(this, new EventArgs());
        }
        catch
        {
            Debug.Log("Exception raised throwing ClickBlocked event");
        }
    }

    private void OnConsoleMessageAdded(System.Object sender, JSConsoleMessageEventArgs args)
    {
        Debug.Log("ConsoleMsg: " + args.Message + "\n" + args.LineNumber + ": " + args.Source);
    }
    // end: Erik added
    #endregion

    #region Input Processing

    #region OnGUI
    private bool HandleEvent(Event e, out RaycastHit hit)
    {
        if (e.isMouse)
        {
            // We only inject mouse input that occurred in this GameObject.
            // If mouse is outside this object and the presenter tool, unfocus this object.
            if (MouseHelpers.GetCurrentGameObjectHit(out hit) != this.gameObject)
            {
                if (e.type == EventType.MouseDown)
                    wasLastMouseDownObj = false;
                if (e.type == EventType.MouseUp)
                    webView.InjectMouseUp(Utilities.GetMouseButton());

                bool isBeingControlledByPresenterTool = GameGUI.Inst.mouseInPresenterTool && GameGUI.Inst.allowPresenterTool && GameGUI.Inst.presenterToolCollabBrowser == this;
                if (e.type != EventType.mouseDown && e.type != EventType.mouseDrag && !isBeingControlledByPresenterTool && !wasLastMouseDownObj)
                    Unfocus();

                return false;
            }
            else
            {
                if (e.type == EventType.MouseDown)
                    wasLastMouseDownObj = true;
                return true;
            }
        }
        else
        {
            hit = new RaycastHit();
            return (e.isKey && isFocused) || ((e.type == EventType.ScrollWheel) && isScrollable && enableScrollWheel && !GameGUI.Inst.mouseInPresenterTool);
        }
    }

    private bool SnapScreenInputDetected()
    {
#if UNITY_STANDALONE_OSX
        return (Input.GetKey(KeyCode.LeftApple) || Input.GetKey(KeyCode.RightApple)) && (Utilities.GetMouseButton() == Awesomium.Mono.MouseButton.Left);
#else
        return (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && (Utilities.GetMouseButton() == Awesomium.Mono.MouseButton.Left);
#endif
    }

    private void OnGUI()
    {

        if (!CheckWebView())
            return;

        Event e = Event.current;
        RaycastHit hit;

        if (gui == null && !HandleEvent(e, out hit))
            return;

        // allow HandleEvent to unfocus panels, so this check is after that call.
        bool snapScreenDetected = Event.current.type == EventType.MouseUp && AllowSnapToScreen && SnapScreenInputDetected();
        bool mouseDownEvt = Event.current.type == EventType.MouseDown;
        if ((restrictedByTrigger && !snapScreenDetected && !mouseDownEvt) || (!AllowInputChanges && !mouseDownEvt && !snapScreenDetected))
            return;

        int x;
        int y;

        x = (int)(hit.textureCoord.x * width);
        y = (int)(hit.textureCoord.y * height);

        if (!GameGUI.Inst.mouseInPresenterTool)
            HandleBrowserEvent(e, x, y);

        if (Input.GetMouseButton(0))
        {
            if ((Input.mousePosition.x <= 0) ||
                (Input.mousePosition.x >= Screen.width) ||
                (Input.mousePosition.y <= 0) ||
                (Input.mousePosition.y >= Screen.height))
            {
                webView.InjectMouseUp(0);
            }
        }
    }

    void OnApplicationFocus(bool focusStatus)
    {
        if(!focusStatus && webView != null)
            webView.InjectMouseUp(0);
    }


    bool HandleMacKeyCommands(Event e)
    {
        if( e.command )
        {
            if( e.keyCode == KeyCode.C || e.keyCode == KeyCode.X )
            {
                string selectedText = GetSelectedText();
                if( !string.IsNullOrEmpty(selectedText) )
                    ClipboardHelper.Clipboard = selectedText;
                if( e.keyCode == KeyCode.X )
                {
                    Event ke = Event.KeyboardEvent("delete");
                    HandleKeyboardEvent(ke, false);
                }        
                return true;
            }
            if( e.keyCode == KeyCode.V )
            {
                string clipboard = ClipboardHelper.Clipboard;
                foreach(char c in clipboard)
                {
                    bool addShift = false;
                    string keyStr = c.ToString();
                    //Event.KeyboardEvent could not recognize these characters, since they are reserved to indicate modifiers
                    if( keyStr == "&" || keyStr == "^" || keyStr == "%" || keyStr == "#")
                    {
                        keyStr = "3"; // 3 doesn't seem to matter (can't be empty), as long as the final character is overwritten
                        addShift = true;
                    }                                      
                    Event ke = Event.KeyboardEvent(keyStr);
                    if( Char.IsUpper(c) || addShift )
                        ke.modifiers |= EventModifiers.Shift;
                        
                    HandleKeyboardEvent(ke, false, true, c);
                }
                return true;
            }  
        }
        return false;
    }


    void HandleKeyboardEvent(Event e, bool addModifier = true, bool overrideChar = false, char c = '\0')
    {
        if (!disableKeyInput)
        {
            if (!DynamicKeyInputAllowed || (GameGUI.Inst.GuiLayerHasInputFocus &&  GameGUI.Inst.guiLayer.htmlPanel.browserTexture != this))
                RaiseKeyEntryBlocked(e.keyCode);
            else
            {
                WebKeyboardEvent webEvent = e.GetKeyboardEvent();
                if( addModifier && e.command )
                    webEvent.Modifiers |= WebKeyModifiers.MetaKey;

                if( overrideChar )
                {
                    if( webEvent.Text == null )
                        webEvent.Text = new ushort[1];
                    webEvent.Text[0] = (ushort)c;                    
                }
                webView.InjectKeyboardEvent(webEvent);
            }
        }        
    }

    public void HandleBrowserEvent(Event e, int x, int y)
    {
        // Don't handle non-gui events if the mouse is over the private browser
        if (gui == null && PrivateBrowser.Active && PrivateBrowser.Inst.IsMouseOverBrowser())
            return;

        switch (e.type)
        {
            case EventType.KeyDown:
            case EventType.KeyUp:

#if UNITY_STANDALONE_OSX
                if(HandleMacKeyCommands(e))
                    break;
#endif            
                HandleKeyboardEvent(e);
                break;

            case EventType.MouseDown:

                if (gui != null)
                    OnMouseDown();
                else if (GameManager.Inst.LocalPlayerType == PlayerType.STEALTH)
                    break;
                else
                {
                    if (!disableAll3DMouseClicks)
                    {
                        Focus();
                        if (!restrictedByTrigger && AllowInputChanges && (!AllowSnapToScreen || !SnapScreenInputDetected()))
                        {
                            webView.InjectMouseMove(x, height - y);
                            webView.InjectMouseDown(Utilities.GetMouseButton());
                        }
                        else
                            RaiseClickBlocked();
                    }
                }
                break;

            case EventType.MouseUp:
                if (gui != null)
                    OnMouseUp();
                else
                {
                    if (AllowSnapToScreen && SnapScreenInputDetected())
                        ToggleSnapCameraToObject();
                    else
                    {
                        if (!disableAll3DMouseClicks)
                        {
                            webView.InjectMouseMove(x, height - y);
                            webView.InjectMouseUp(Utilities.GetMouseButton());
                        }
                    }
                }
                break;

            case EventType.MouseDrag:
                if (gui != null)
                    OnMouseDrag();
                else
                {
                    webView.InjectMouseMove(x, height - y);
                }
                break;

            case EventType.ScrollWheel:
                webView.InjectMouseWheel((int)e.delta.y * -10, 0);
                break;

        }
    }

    public void ToggleSnapCameraToObject()
    {
        if (MainCameraController.Inst.cameraType != CameraType.SNAPCAM)
        {
            float offsetBuffer = 4;
            float camPosOffset = CameraHelpers.GetCameraDistFromPlaneHeight(transform.lossyScale.y, Camera.main.fieldOfView) + offsetBuffer;
            Vector3 pos = transform.position + camPosOffset * transform.forward;
            // only way I know to get a unique copy of a transform (create another game object.
            /*
            if (toggleSnapCamHelperGO == null)
                toggleSnapCamHelperGO = new GameObject("ToggleSnapCamHelper");
            toggleSnapCamHelperGO.transform.rotation = Camera.main.transform.rotation;
            toggleSnapCamHelperGO.transform.forward = -transform.forward;
            */
            MainCameraController.Inst.SnapCamera(pos, Quaternion.LookRotation(-transform.forward), 0.12f);
            snapCamOnMe = true;
        }
        else
        {
            MainCameraController.Inst.cameraType = MainCameraController.Inst.LastCameraType;
            snapCamOnMe = false;
        }
    }

    #endregion

    #region Mouse
    private void OnMouseOver()
    {
        if (!CheckWebView())
            return;

        RaycastHit hit;

        if (gui != null) // Used for injecting a MouseMove event on a GUITexture.
        {
            int x = (int)((Input.mousePosition.x) - (gui.pixelInset.x + Screen.width * transform.position.x));
            int y = (int)((Input.mousePosition.y) - (gui.pixelInset.y + Screen.height * transform.position.y));
            webView.InjectMouseMove(x, height - y);
        }
        else if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
        {
            int x = (int)(hit.textureCoord.x * width);
            int y = (int)(hit.textureCoord.y * height);
            webView.InjectMouseMove(x, height - y);

            if (ShowMouseRepresentation)
            {
                Vector3 diffVector = lastMousePosition - hit.point;
                if (!GameGUI.Inst.mouseInPresenterTool && ((Vector3.Dot(diffVector, diffVector) > minDistanceToReportPosChange) || lastMouseDown != Input.GetMouseButton(0)) && IsNetworkConnectionEstablished()) // Dot product == distance squared, avoiding sqrt.
                {
                    BroadcastMousePosition(hit.point, Input.GetMouseButton(0));

                    lastMousePosition = hit.point;
                    lastMouseDown = Input.GetMouseButton(0);
                }
            }
        }
    }

    private void OnMouseDown()
    {
        if (!CheckWebView() || !AllowInputChanges)
            return;

        Focus();

        if (gui != null)
        {
            int x = (int)((Input.mousePosition.x) - (gui.pixelInset.x + Screen.width * transform.position.x));
            int y = (int)((Input.mousePosition.y) - (gui.pixelInset.y + Screen.height * transform.position.y));
            if (x < 0 || y < 0 || x > (width) || (y > height))
                RaiseClickOutsideGUIBoundary();
            else
            {
                webView.InjectMouseMove(x, height - y);
                webView.InjectMouseDown(Utilities.GetMouseButton());
                if (Event.current != null && texture.GetPixel(x, y).a > 0.0f)
                    Event.current.Use();
            }
        }
    }

    private void OnMouseUp()
    {
        if (!CheckWebView() || !AllowInputChanges)
            return;

        if (gui != null)
        {
            int x = (int)((Input.mousePosition.x) - (gui.pixelInset.x + Screen.width * transform.position.x));
            int y = (int)((Input.mousePosition.y) - (gui.pixelInset.y + Screen.height * transform.position.y));
            webView.InjectMouseMove(x, height - y);
            webView.InjectMouseUp((MouseButton)lastMouseButtonDown);
            if (Event.current != null && texture.GetPixel(x, y).a > 0.0f)
                Event.current.Use();
        }
    }

    public Color GetPixelColorAtMousePosition()
    {
        if (gui != null && texture != null)
        {
            int x = (int)((Input.mousePosition.x) - (gui.pixelInset.x + Screen.width * transform.position.x));
            int y = (int)((Input.mousePosition.y) - (gui.pixelInset.y + Screen.height * transform.position.y));
            return texture.GetPixel(x, y);
        }
        return Color.black;
    }

    private void OnMouseDrag()
    {
        if (gui != null && CheckWebView())
        {
            int x = (int)((Input.mousePosition.x) - (gui.pixelInset.x + Screen.width * transform.position.x));
            int y = (int)((Input.mousePosition.y) - (gui.pixelInset.y + Screen.height * transform.position.y));
            webView.InjectMouseMove(x, height - y);
        }
    }

    private void OnMouseEnter()
    {
        isScrollable = true;
    }

    public void OnMouseExit()
    {
        isScrollable = false;

        if (!CheckWebView())
            return;

        if (webView.IsEnabled && !Input.GetMouseButtonDown(0))
            webView.InjectMouseMove(-1, -1);

        if (ShowMouseRepresentation && IsNetworkConnectionEstablished())
        {
            ISFSObject mouseExitObj = new SFSObject();
            mouseExitObj.PutBool("me", true);
            CommunicationManager.SendObjectMsg(mouseExitObj);
            if (gui == null)
                RemoteMouseManager.Inst.GetMyVisual().SetVisibility(false);
        }
    }
    #endregion

    #endregion

    #region Event Handlers
    private void OnLoginRequest(object sender, LoginRequestEventArgs e)
    {
        if (!CheckWebView())
            return;

        // Ask user for credentials or provide them yourself.
        // Do not forget to set Cancel to false.
        e.Cancel = true;

        // Prevent further processing by the WebView.
        e.Handled = true;
    }

    private bool CustomDialogHandler(JavascriptDialogEventArgs e)
    {
        if ((e.DialogFlags & JSDialogFlags.HasPromptField) > 0)
            return false;

        string action = "";
        if (supportedDialogMessages.TryGetValue(e.Message, out action))
        {
            GameGUI.Inst.ExecuteJavascriptOnGui("showConfirmDialog(\"" + e.Message + "\", \"" + action + ",url=" + URL + "\")");
            e.Cancel = true;
            e.Handled = true;
            return true;
        }

        // Google Doc crash message
        if ((e.Message.IndexOf("error has been reported to Google") != -1 && (e.DialogFlags & JSDialogFlags.HasCancelButton) != 0))
        {
            GameGUI.Inst.ExecuteJavascriptOnGui("handleGoogleCrash(\"" + e.Message + "\", \"" + URL + "\")");
            e.Cancel = true;
            e.Handled = true;
            return true;
        }

        // just a message, no options, display a dialog
        if ((e.DialogFlags & JSDialogFlags.HasCancelButton) == 0)
        {
            GameGUI.Inst.ExecuteJavascriptOnGui("showAlertDialog(\"" + e.Message + "\", \"From: " + webView.Title + "\")");
            e.Cancel = true;
            e.Handled = true;
            return true;
        }

        return false;
    }

    private void OnJavascriptDialog(object sender, JavascriptDialogEventArgs e)
    {
        if (!CheckWebView())
            return;

        if( forceNextConfirm )
        {
            e.Cancel = false;
            e.Handled = true;
            forceNextConfirm = false;
            return;
        }

        if (CustomDialogHandler(e))
            return;

        if (Screen.fullScreen || ((e.DialogFlags & JSDialogFlags.HasPromptField) > 0)) // native panels don't work in full screen so don't show them.
        {
            // It's a 'window.prompt'. You need to design your own dialog for this.
            if ((e.DialogFlags & JSDialogFlags.HasPromptField) > 0)
                Debug.LogError("We do not support prompt fields at the moment");
            e.Cancel = false;
            e.Handled = true;
        }
        else // Everything else can be presented with a MessageBox that can easily be designed using the available extensions.
        {
            NativePanels.MessageButtons options = ((e.DialogFlags & JSDialogFlags.HasCancelButton) > 0) ? NativePanels.MessageButtons.OKCancel : NativePanels.MessageButtons.OK;
            string msg = "Game Paused:\n" + e.Message;
            int choice = NativePanels.SetMessageBox(msg, "", options);
            e.Handled = true;
            e.Cancel = choice != 1;
        }
    }

    public void ForceConfirmClickOnDiv(string divToClick)
    {
        forceNextConfirm = true;
        ExecuteJavaScript("var divToClick = $(\"#" + divToClick + "\"); if(divToClick != null){divToClick.click();}");
    }

    private void OnWebViewOpenExternalLink(object sender, OpenExternalLinkEventArgs e)
    {
        if (!CheckWebView())
            return;

        // For this sample, we load the URL
        // in the same WebView.
        webView.LoadURL(e.Url);
    }
    #endregion


    public void BroadcastMousePosition(Vector3 worldSpacePos, bool mouseDown)
    {
        if (!IsNetworkConnectionEstablished())
            return;

        // display your mouse object first
        RemoteMouseManager.Inst.GetMyVisual().SetPosition(worldSpacePos);
        RemoteMouseManager.Inst.GetMyVisual().mouseDown = mouseDown;
        RemoteMouseManager.Inst.GetMyVisual().browserId = id;

        RemoteMouseManager.Inst.GetMyVisual().textureScaleMult = Mathf.Min(transform.lossyScale.x, transform.lossyScale.y) * 0.1f;


        Vector3 mouseLocalPos = transform.InverseTransformPoint(worldSpacePos);

        ISFSObject mousePosObj = new SFSObject();

        Sfs2X.Util.ByteArray mouseBytes = new Sfs2X.Util.ByteArray();

        if (GameManager.buildType == GameManager.BuildType.REPLAY)
        {
            mouseBytes.WriteByte((byte)id);
            mouseBytes.WriteFloat((((mouseLocalPos.x))));
            mouseBytes.WriteFloat((((mouseLocalPos.y))));
            mouseBytes.WriteBool(mouseDown);
            mousePosObj.PutByteArray("mp", mouseBytes);
        }
        else
        {
            mouseBytes.WriteByte((byte)id);
            mouseBytes.WriteByte((byte)(Mathf.Round((mouseLocalPos.x + 0.5f) * 256)));
            mouseBytes.WriteByte((byte)(Mathf.Round((mouseLocalPos.y + 0.5f) * 256)));
            mouseBytes.WriteBool(mouseDown);
            mousePosObj.PutByteArray("mpx", mouseBytes);
        }
        CommunicationManager.SendObjectMsg(mousePosObj);

    }

    public void InjectMousePosition(Vector2 textureCoords, bool broadcast, bool mouseDown)
    {
        webView.InjectMouseMove((int)((1.0f - (textureCoords.x + 0.5f)) * width), (int)((1.0f - (textureCoords.y + 0.5f)) * height));
        if( broadcast)
            BroadcastMousePosition(textureCoords, mouseDown);
    }
    public void BroadcastMousePosition(Vector2 textureCoords, bool mouseDown)
    {
        Vector3 mouseLocalPos = new Vector3(textureCoords.x, textureCoords.y, 0f);
        Vector3 mouseWorldPos = transform.TransformPoint(mouseLocalPos.x, mouseLocalPos.y, 0f);

        BroadcastMousePosition(mouseWorldPos, mouseDown);
    }

    // gets all the elements in the html with a specified class and replaces them with the new class.
    public void ReplaceClass(string className, string newClassName)
    {
        string cmd = "var elems = document.getElementsByClassName(\"" + className + "\");  if(elems != null){for(var i=elems.length-1; i>=0; i--){elems[i].className = \"" + newClassName + "\";}}";
        ExecuteJavaScript(cmd);
    }

    public void SaveToPNG(string filename)
    {
        webView.SaveToPNG(filename);
    }

    public void SetBodyBGColor(string bgColor)
    {
        JSValue result = ExecuteJavaScriptWithResult("var elems = document.getElementsByTagName(\"body\"); if(elems.length > 0){elems[0].style.backgroundColor=\"" + bgColor + "\";}");
        Debug.Log("SetBodyBGColor ExecuteJavaScriptWithResult: " + result.ToString());
    }

    public void ClearTexture()
    {
        Color clearColor = new Color(1, 1, 1, 0);
        for (int y = 0; y < texture.height; ++y)
            for (int x = 0; x < texture.width; ++x)
                texture.SetPixel(x, y, clearColor);
        // Apply all SetPixel calls
        texture.Apply();
    }

    /*
    // Legacy SetTexture function, used to be used for the "refreshing..." graphic. (Now handled by a material swap.)
    public void SetTexture(Texture2D newTexture)
    {
        if (newTexture == null)
            return;
        Color clearColor = new Color(1, 1, 1, 0);
        Vector2 center = new Vector2(width / 2, height / 2);
        Vector2 startNewTexture = new Vector2(center.x - newTexture.width / 2, center.y - newTexture.height / 2);
        Vector2 endNewTexture = new Vector2(startNewTexture.x + newTexture.width, startNewTexture.y + newTexture.height);
        int tWidth = texture.width;
        int tHeight = texture.height;
        for (int y = 0; y < tHeight; ++y)
            for (int x = 0; x < tWidth; ++x)
            {
                if (y < startNewTexture.y || y >= endNewTexture.y || x < startNewTexture.x || x >= endNewTexture.x)
                    texture.SetPixel(x, y, clearColor);
                else
                    texture.SetPixel(x, y, newTexture.GetPixel(x - (int)startNewTexture.x, y - (int)startNewTexture.y));
            }
        // Apply all SetPixel calls
        texture.Apply();
    }
    */

    public Texture2D GetTexture()
    {
        return texture;
    }

    public string GetSelectedText()
    {
        if (!CheckWebView())
            return "";
        return webView.Selection.Text;
    }

    public void ClickOnTopCenterOfObject(JSObject rectObj)
    {
        if (webView == null || rectObj == null || !rectObj.HasProperty("left") || !rectObj.HasProperty("top") || !rectObj.HasProperty("width"))
            return;
        int ltPos = rectObj["left"].ToInteger();
        int topPos = rectObj["top"].ToInteger();
        int width = rectObj["width"].ToInteger();
        webView.InjectMouseMove(ltPos + width / 2, topPos + 1);
        webView.InjectMouseDown(MouseButton.Left);
        webView.InjectMouseUp(MouseButton.Left);
    }
}

