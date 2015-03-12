using System.Collections;
using System.Collections.Generic;
using Awesomium.Mono;

public class WebViewManager {


    private static List<WebView> webViews = new List<WebView>();
    private static WebViewManager mInstance;
    public static WebViewManager Inst
    {
        get
        {
            if (mInstance == null)
                mInstance = new WebViewManager();
            return mInstance;
        }
    }

    public WebView CreateWebView(int width, int height)
    {
        WebView webView = WebCore.CreateWebView(width, height);
        webViews.Add(webView);
        return webView;
    }

    public void CloseWebView(WebView webView) {
        webViews.Remove(webView);
        if( webView != null )
            webView.Close();
    }

    public bool AreAnyBusy() {
        foreach (WebView wv in webViews)
        {
            if (wv == null || wv.IsDisposed)
                webViews.Remove(wv);
            if (wv != null && (wv.IsLoadingPage || wv.IsNavigating))
                return true;
        }
        return false;
    }

    public void StopAll()
    {
        foreach (WebView wv in webViews)
        {
            if (wv == null || wv.IsDisposed)
                webViews.Remove(wv);
            wv.Stop();
        }
    }
}
