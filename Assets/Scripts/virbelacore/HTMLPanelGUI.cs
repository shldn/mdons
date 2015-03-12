using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using Awesomium.Mono;
using Awesomium.Unity;

public class HTMLPanelGUI : MonoBehaviour {

	private GameObject overlayGO = null;
	public CollabBrowserTexture browserTexture;
    public int browserId = -1;
	private bool initialized = false;
	public delegate void WebCallbackDelegate(JSValue[] args);
	private Dictionary<string, WebCallbackDelegate> registeredCallbacks = new Dictionary<string, WebCallbackDelegate>();
	
	public int x, y;
	public int width = 320;
	public int height = 240;
	public string html = "";
    public string initUrl = "";
    public bool useTransparentMaterial = true;
    public bool Active { get { return overlayGO != null && overlayGO.activeInHierarchy; } }
	
	// Use this for initialization
	void Awake()
	{
		overlayGO = new GameObject("HtmlPanelGO");
		overlayGO.AddComponent("GUITexture");
		overlayGO.transform.localScale = Vector3.zero;
		DontDestroyOnLoad(overlayGO);
		x = (Screen.width-width) / 2;
		y = (Screen.height-height) / 2;
	}
	
	void Start()
	{
		browserTexture = overlayGO.AddComponent<CollabBrowserTexture>();
        browserTexture.useCollabFeatures = browserId != -1; // true let's us use the webpanel command
		browserTexture.Width = width;
		browserTexture.Height = height;
        browserTexture.useTransparentMaterial = useTransparentMaterial;
        browserTexture.id = browserId;
        if (initUrl != "")
            browserTexture.InitialURL = initUrl;
		browserTexture.AddLoadCompleteEventListener(OnWebViewLoaded);
	}
	
	void OnWebViewLoaded(System.Object sender, System.EventArgs args)
	{
		browserTexture.webView.CreateObject("AwesomiumClient");
		browserTexture.webView.SetObjectCallback("AwesomiumClient", "Notify", OnWebViewCallback);
	}
	
	void OnWebViewCallback(object sender, JSCallbackEventArgs e)
	{
		if (e.CallbackName == "Notify")
		{
			string id = e.Arguments[0].ToString();
			if(registeredCallbacks.ContainsKey(id))
			{
				WebCallbackDelegate cb = registeredCallbacks[id];
				cb(e.Arguments);
			}
		}
	}
	
	public void RegisterNotificationCallback(string id, WebCallbackDelegate callback) {
		if(!registeredCallbacks.ContainsKey(id))
			registeredCallbacks.Add(id, callback);
		else
			registeredCallbacks[id] = callback;
	}
	
	public bool SetURL(string url)
	{
        if (browserTexture == null)
        {
            initUrl = url;
            return false;
        }
        browserTexture.Height = height;
        browserTexture.Width = width;
		bool result = browserTexture.GoToURL(url);
        Center(browserTexture.Width, browserTexture.Height);
        return result;
	}
	
	public void LoadHTML(string html)
	{
		browserTexture.Height = height;
		browserTexture.Width = width;
		browserTexture.LoadHTML(html);
		Center(width, height);
	}

    public void LoadURL(string url, int _x = -1, int _y = -1)
    {
        Debug.Log("HTMLPanelGUII - loading URL: " + url);
        browserTexture.Height = height;
        browserTexture.Width = width;
        browserTexture.GoToURL(url);
        if (_x < 0 || _y < 0)
            Center(width, height);
        else
            Place(_x, _y);
    }
	
	public void LoadFile(string filename, int _x=-1, int _y=-1)
	{
        Debug.Log("HTMLPanelGUII - loading file: " + filename);
        browserTexture.Height = height;
		browserTexture.Width = width;
		browserTexture.LoadFile(filename);
		if (_x < 0 || _y <0)
			Center(width, height);
		else
			Place( _x, _y);
	}

    public void Center()
    {
        Center(width, height);
    }

	void Center(int w, int h)
	{
		if (overlayGO.guiTexture == null)
			return;
		x = (Screen.width-w) / 2;
		y = (Screen.height-h) / 2;
		overlayGO.guiTexture.pixelInset = new Rect(x, y, w, h);
	}
	
	public void Place(int _x=-1, int _y=-1)
	{
		if (overlayGO.guiTexture == null)
			return;
		if (_x<0 && _y<0)
		{
			Center(width, height);
			return;
		}
		x = _x;
		y = _y;
		overlayGO.guiTexture.pixelInset = new Rect(x, y-height, width, height);
	}
	
	public void SetActive(bool active)
	{
		overlayGO.SetActive(active);
	}

    private void OnDestroy()
    {
        Destroy(overlayGO);
    }
}
