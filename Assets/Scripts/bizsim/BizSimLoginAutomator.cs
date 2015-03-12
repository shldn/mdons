using UnityEngine;
using System;
using Awesomium.Mono;

//-----------------------------------
// 
// class to automate the process of logging into Industry Master's business simulation
// Once the login process is complete, their game pages will be accessible 
// Old instant login: http://uis-sustain.industrymasters.net/instantaccess.php?instantusername=Player1
// New instant login: http://unity.industrymasters.net/unityaccess.php?account=1667&instance=12285&instantusername=Team1
//-----------------------------------
public class BizSimLoginAutomator {

	enum LoginStage
	{
		START,
		LOGGED_IN,
		//COURSE_CHOICE_PAGE,
		IN_GAME
	}

	private bool startNewSim = true;
    private string account = "1667"; 
    private WebView webView;
    private LoginStage stage;

    public delegate void LoginCompleteEventHandler(object sender, EventArgs e);
    public event LoginCompleteEventHandler LoginComplete;

    public BizSimLoginAutomator(string loginPageUrl, string username, string gameID, string simType)
    {
    	if( !startNewSim )
    		return;
    	stage = LoginStage.START;
        webView = WebViewManager.Inst.CreateWebView(1, 1);
        webView.BeginLoading += OnBeginLoading;
        if (!Login(loginPageUrl, username, gameID, simType))
            Debug.Log(" Login failed ");
    }

    private bool CheckWebView()
    {
        return WebCore.IsRunning && (webView != null) && webView.IsEnabled;
    }

    private bool Login(string loginPageUrl, string username, string gameID, string simType)
    {
        if (!CheckWebView())
            return false;
        string url = loginPageUrl + "?instantusername=" + username;
        if( BizSimManager.playMode == SimPlayMode.MULTI_PLAYER)
            url += "&account=" + account + "&instance=" + gameID + "&simid=" + simType;
        if (BizSimManager.playMode == SimPlayMode.DEMO)
            url += "&simid=uis_demo";
#if UNITY_EDITOR
		Debug.Log ("login url: " + url);
#endif
    	webView.LoadURL( url );
    	return true;
    }

    private void OnBeginLoading(System.Object sender, Awesomium.Mono.BeginLoadingEventArgs args)
    {
        if (!CheckWebView())
            return;

        if (!args.Url.Contains("holdinginfo.php"))
        {
            // the url is expected to redirect to holdinginfo.php, so if it hasn't, assume there was an error
            InfoMessageManager.Display("* We're sorry, the business simulation is unavailable at this time *");
            Debug.LogError("Biz Sim Login Error: url didn't redirect: " + args.Url);
            return;
        }
        switch (stage)
        {
            case LoginStage.START:
                stage = LoginStage.IN_GAME;
                RaiseLoginCompleteEvent(args);
                break;
        }
    }

    void RaiseLoginCompleteEvent(System.EventArgs args)
    {
        try
        {
            if (LoginComplete != null)
                LoginComplete(this, new System.EventArgs());
        }
        catch(Exception e)
        {
            Debug.LogError("Exception raised throwing LoginCompleteEventHandler event: " + e.ToString());
        }
    }
}


