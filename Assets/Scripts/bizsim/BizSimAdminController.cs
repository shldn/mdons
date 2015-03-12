using UnityEngine;
using System.Collections;
using Awesomium.Mono;

public class BizSimAdminController {

    enum LoginStage
    {
        START,
        LOGIN_ATTEMPTED,
        LOGGED_IN,
        LOGIN_FAILED
    }

    private WebView webView;
    private LoginStage stage;

    public BizSimAdminController(string loginPageUrl, string username, string password)
    {
    	stage = LoginStage.START;
        webView = WebViewManager.Inst.CreateWebView(1, 1);
        webView.LoadCompleted += OnLoadCompleted;
        if (!Login(loginPageUrl, username, password))
            Debug.Log(" Login failed ");
    }

    private bool CheckWebView()
    {
        return WebCore.IsRunning && (webView != null) && webView.IsEnabled;
    }

    //http://uim-sustain.industrymasters.net/login.php?user=virbela&password=****
    private bool Login(string loginPageUrl, string username, string password)
    {
        if (!CheckWebView())
            return false;

        string url = loginPageUrl + "?user=" + username + "&password=" + password;
        Debug.LogError("login url: " + url);
        webView.LoadURL(url);
        return true;
    }

    private string GetBtnClickJavaScriptCmd(string btnName)
    {
        return "var btns = (document.getElementsByName(\"" + btnName + "\")); if(btns != null && btns.length > 0){btns[0].click();}";
    }

    private void OnLoadCompleted(System.Object sender, System.EventArgs args)
    {
        Debug.LogError("BizSimAdminController: OnLoadCompleted");
        if (!CheckWebView())
            return;

        switch (stage)
        {
            case LoginStage.START:
                Debug.LogError("LoginStage.START: " + webView.Source.AbsolutePath);
                webView.ExecuteJavascript(GetBtnClickJavaScriptCmd("Login"));
                stage = LoginStage.LOGIN_ATTEMPTED;
                break;
            case LoginStage.LOGIN_ATTEMPTED:
                Debug.LogError("LoginStage.LOGIN_ATTEMPTED");
                string expectedPage = "adminpanel.php";
                if (webView.Source.AbsolutePath.Contains(expectedPage))
                    stage = LoginStage.LOGGED_IN;
                else
                {
                    Debug.LogError("Admin Login Failed, redirected to " + webView.Source.PathAndQuery);
                    stage = LoginStage.LOGIN_FAILED;
                }

                break;
            case LoginStage.LOGGED_IN:
                Debug.LogError("OnDomReady: LoginStage.LOGGED_IN");
                break;
            case LoginStage.LOGIN_FAILED:
                Debug.LogError("OnDomReady: LoginStage.LOGIN_FAILED");
                break;

        }
    }

    public void AdvanceQuarter()
    {
        if (stage == LoginStage.LOGGED_IN)
        {
            Debug.LogError("Advance Quarter not implemented yet!");
            // make sure container.lastChild.firstChild is a form (could check container[0].lastChild.firstChild.method for null)
            //var container = document.getElementsByClassName("displayeco")
            //container[0].lastChild.firstChild.submit() // pushes the "Next Quarter >>>" button
        }
    }

    public void Shutdown()
    {
        WebViewManager.Inst.CloseWebView(webView);
    }
}
