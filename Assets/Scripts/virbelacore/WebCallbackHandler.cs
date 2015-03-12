using System.Collections.Generic;
using Awesomium.Mono;

public class WebCallbackHandler {

    CollabBrowserTexture bTex = null;
    public string cmdToInjectOnLoadComplete = "";

    // Setup callbacks from the webpage
    public delegate void WebCallbackDelegate(JSValue[] args);
    private Dictionary<string, WebCallbackDelegate> registeredCallbacks = new Dictionary<string, WebCallbackDelegate>();

    public WebCallbackHandler(CollabBrowserTexture bTex_)
    {
        bTex = bTex_;
        bTex.AddLoadCompleteEventListener(OnLoadComplete);
    }

    // Callbacks and message handling
    void SetupCallbacks()
    {
        if (registeredCallbacks.Count > 0)
        {
            bTex.webView.CreateObject("UnityClient");
            bTex.webView.SetObjectCallback("UnityClient", "Notify", OnWebViewCallback);
        }
    }

    public void RegisterNotificationCallback(string id, WebCallbackDelegate callback)
    {
        if (!registeredCallbacks.ContainsKey(id))
            registeredCallbacks.Add(id, callback);
        else
            registeredCallbacks[id] = callback;
    }

    void OnWebViewCallback(object sender, JSCallbackEventArgs e)
    {
        if (e.CallbackName == "Notify")
        {
            string id = e.Arguments[0].ToString();
            if (registeredCallbacks.ContainsKey(id))
            {
                WebCallbackDelegate cb = registeredCallbacks[id];
                cb(e.Arguments);
            }
        }
    }

    void OnLoadComplete(System.Object sender, System.EventArgs args)
    {
        SetupCallbacks();
        if (cmdToInjectOnLoadComplete != "")
            bTex.ExecuteJavaScript(cmdToInjectOnLoadComplete);
    }



    // List of callbacks that can be registered

    public void RegisterZoom(){
        RegisterNotificationCallback("Zoom", delegate(JSValue[] args)
        {
            if (MainCameraController.Inst.cameraType != CameraType.SNAPCAM && (!ReplayManager.Initialized || !ReplayManager.Inst.Playing))
                bTex.ToggleSnapCameraToObject();
        });
    }


}
