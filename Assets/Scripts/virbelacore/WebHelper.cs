using UnityEngine;
using System;
using System.Collections;
using Awesomium.Mono;
using Awesomium.Unity;

public class WebHelper : MonoBehaviour {

    private bool error = false;
    private bool webCoreFailed = false;
    private bool checkForCacheInstructions = false;
    private string cacheInstructionURL = "http://game.virbela.com/cache/";
    private string customCacheStr = "";
    static private bool clearCache = false;
    static public bool ClearCache
    {
        set
        {
            if (WebCore.IsRunning && value == true)
            {
                Debug.LogError("Clearing WebCore cache");
                WebCore.ClearCache();
                clearCache = false;
            }
            else
                clearCache = value;

        }
    }
	private void WebCoreInit()
	{
        int attemptCount = 0;
        int maxAttempts = 10;
		while ( !WebCore.IsRunning && attemptCount < maxAttempts )
		{
            WebCoreConfig wcConfig = new WebCoreConfig() { SaveCacheAndCookies = true };
            if (attemptCount > 0)
                wcConfig.UserDataPath = "CSIDL_APPDATA/Awesomium/Default" + customCacheStr + attemptCount; 
            WebCore.Initialize(wcConfig);

            try
            {
                WebView wv = WebCore.CreateWebView(1, 1);
                wv.Close();
            }
            catch (Exception e)
            {
                Debug.LogError("Caught webcore init exception, trying new path for cache...");
                attemptCount++;
            }
		}
        error = attemptCount >= maxAttempts;
	}

    void OnGUI()
    {
        if( (error || webCoreFailed) && GameManager.Inst.ServerConfig != "MDONS" )
        {
            GUI.contentColor = Color.red;
            string msg = webCoreFailed ? "Error: The embedded web browser has crashed please quit and relaunch the program (/quit)" : "Error: There are too many instances of virbela running, please close them all and try again";
            GUILayout.TextField(msg);
            GUI.contentColor = Color.white;
        }
    }
	
	// Use this for initialization
	void Awake () {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (WebCore.IsRunning)
        {
            // Coming back to Connection screen, shouldn't create multiple WebHelpers.
            Destroy(gameObject);
            return;
        }
        if (checkForCacheInstructions)
            gameObject.AddComponent<DownloadHelper>().StartDownload(cacheInstructionURL + GameManager.Inst.ServerConfig.ToLower() + ".txt", InitCallback);
        else
            Init();

	}

    private void InitCallback(WWW downloadObj)
    {
        if (!string.IsNullOrEmpty(downloadObj.error))
        {
            VDebug.LogError("Error downloading config cache directive from web: " + downloadObj.error);
            Init();
        }
        else
        {
            customCacheStr = downloadObj.text.Substring(0, 1);
            VDebug.LogError("Setting custom cache string: " + customCacheStr);
            Init();
        }
    }

    private void Init()
    {
        if (GameManager.InBatchMode())
        {
            Debug.LogError("Disabling WebCore for recording players");
            return;
        }
        Debug.Log("Started WebCoreHelper!");
        WebCoreInit();
        InvokeRepeating("tick", 0, 0.020F);
        DontDestroyOnLoad(this);
        ClearCache = clearCache;
#endif
	}
	
	public void tick() {
        if (WebCore.IsRunning)
        {
            WebCore.Update();
            webCoreFailed = false;
        }
        else
        {
            if (!webCoreFailed)
                Debug.LogError("WebCore is not running!");
            webCoreFailed = true;
        }
	}

	void OnApplicationQuit() {
#if UNITY_EDITOR || UNITY_STANDALONE
		WebCore.Shutdown();
#endif
	}
}
