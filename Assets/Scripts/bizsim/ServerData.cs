using UnityEngine;
using System;
using System.Globalization;
using System.Collections;
using Awesomium.Mono;

public class ServerData : MonoBehaviour {
	
    private WebView webView;
	int currentTick = 0;
	bool firstLoad = true;
    int refreshAttemptSinceQuarterChangeCount = 0;
    int readDataFailCount = 0;
    bool errorReadingValues = false;
    bool manualQuarterAdvancement = true;
    bool quarterHasChanged = false;
	float userScore = 0f;
	int userRank = 0;
    DateTime quarterEndTime;
    DateTime serverTime;
    TimeSpan serverTimeOffset;
    DateTime lastRefreshTime = DateTime.Now;
    DateTime lastQuarterChangeTime = DateTime.Now;
    DateTime enableRefreshAttemptTime = DateTime.Now;
    private ServerDataDisplay serverDataDisplay;
    public DateTime QuarterEndTime { get { return quarterEndTime; } }
	public int CurrentQuarter { get { return currentTick/3; } }
	public float UserScore { get { return userScore; } }
	public int UserRank { get { return userRank; } }
    public bool UsingManualQuarterAdvancement { get { return manualQuarterAdvancement; } }
    public bool IsLoading { get { return webView != null && webView.IsLoadingPage; } }
    public bool IsValid { get { return !IsLoading && !firstLoad; } } // valid once it has been loaded.

    public delegate void QuarterChangeHandler(object sender, EventArgs e);
    public event QuarterChangeHandler QuarterChange;

	public void Awake()
    {
        webView = WebViewManager.Inst.CreateWebView(1, 1);
        webView.LoadURL(BizSimScreen.GetStageItemURL(1));
        webView.LoadCompleted += OnLoadCompleted;
        serverDataDisplay = gameObject.AddComponent<ServerDataDisplay>();
	}

    private bool CheckWebView()
    {
        return WebCore.IsRunning && (webView != null) && webView.IsEnabled;
    }

    private JSValue ExecuteJavaScriptWithResult(string cmds)
    {
        if (!CheckWebView())
        {
            Debug.LogError("ServerData: ExecuteJavaScriptWithResult failed CheckWebView");
            return null;
        }
        return webView.ExecuteJavascriptWithResult(cmds, 2000);
    }
	
	private bool IsValidJavaResult(JSValue result)
	{
        return (result != null && result.Type != JSValueType.Null);
	}
	
	private string RowElementJavaCommand(int row)
	{
		return "var elem = document.getElementById('simdata'); if (elem != null) {elem.rows[" + row + "].cells[1].firstChild.nodeValue;}";
	}

    private void setScreenVis()
    {
        if (errorReadingValues)
            return;

        bool error = errorReadingValues;
        bool showStrategy = true;
        bool showAdditionalInvestments = true;
        bool showKPI = true;
        bool showFinance = true;
        bool showWarehouse = true;
        bool showBanking = true;

        JSValue result = null;

        result = error ? null : ExecuteJavaScriptWithResult(RowElementJavaCommand(12));
        error = error || !IsValidJavaResult(result);
        if (!error)
            showStrategy = result.ToInteger() > 0;

        result = error ? null : ExecuteJavaScriptWithResult(RowElementJavaCommand(13));
        error = error || !IsValidJavaResult(result);
        if( !error )
            showAdditionalInvestments = result.ToInteger() > 0;

        result = error ? null : ExecuteJavaScriptWithResult(RowElementJavaCommand(14));
        error = error || !IsValidJavaResult(result);
        if (!error)
            showKPI = result.ToInteger() > 0;

        result = error ? null : ExecuteJavaScriptWithResult(RowElementJavaCommand(15));
        error = error || !IsValidJavaResult(result);
        if (!error)
            showFinance = result.ToInteger() > 0;

        result = error ? null : ExecuteJavaScriptWithResult(RowElementJavaCommand(16));
        error = error || !IsValidJavaResult(result);
        if (!error)
            showWarehouse = result.ToInteger() > 0;

        result = error ? null : ExecuteJavaScriptWithResult(RowElementJavaCommand(17));
        error = error || !IsValidJavaResult(result);
        if (!error)
            showBanking = result.ToInteger() > 0;

        if (error)
        {
            errorReadingValues = error;
            Debug.LogError("setScreenVis() failed.");
            return;
        }

        StrategyScreen.Inst.gameObject.SetActive(showStrategy);
        SustainScreen.Inst.gameObject.SetActive(showAdditionalInvestments);
        KPIScreen.Inst.gameObject.SetActive(showKPI);
        FinanceScreen.Inst.gameObject.SetActive(showFinance);
        WarehouseScreen.Inst.gameObject.SetActive(showWarehouse);
        BankingChartsScreen.Inst.gameObject.SetActive(showBanking);
        SectorInfoScreen.Inst.gameObject.SetActive(!showBanking);
    }

	private void setUserScore()
	{
        if (errorReadingValues)
            return;

		JSValue result = ExecuteJavaScriptWithResult(RowElementJavaCommand(10));

        if (IsValidJavaResult(result))
		{
			userScore = (float)result.ToDouble();
		}
		else
		{
			//Failed.
			Debug.LogError("setUserScore() failed.");
		}
	}
	
	private void setUserRank()
    {
        if (errorReadingValues)
            return;

        JSValue result = ExecuteJavaScriptWithResult(RowElementJavaCommand(11));

        if (IsValidJavaResult(result))
		{
			userRank = result.ToInteger();
            serverDataDisplay.SetRank(userRank);
		}
		else
		{
			//Failed.
			Debug.LogError("setUserRank() failed.");
            errorReadingValues = true;
		}
	}
	
	private void setCurrentTick()
	{
        if (errorReadingValues)
            return;

		JSValue result = ExecuteJavaScriptWithResult(RowElementJavaCommand(7));

        if (IsValidJavaResult(result))
		{
            int prevTick = currentTick;
			currentTick = result.ToInteger();
            quarterHasChanged = prevTick != currentTick && !firstLoad;
            if (currentTick == 60)
                BizSimManager.Inst.finalQuarter = true;

            serverDataDisplay.SetCurrentQuarter(CurrentQuarter);
		}
		else
		{
			Debug.LogError("setCurrentTick() failed");
            errorReadingValues = true;
		}
	}

    // Cut up industrymasters current server date and time.
    private void setServerTime()
    {
        if (errorReadingValues)
            return;

        JSValue result = ExecuteJavaScriptWithResult(RowElementJavaCommand(0));
        JSValue result2 = ExecuteJavaScriptWithResult(RowElementJavaCommand(1));

        if (IsValidJavaResult(result) && IsValidJavaResult(result2))
        {
            serverTime = DateTime.ParseExact(result.ToString() + result2.ToString(), "yyyyMMddHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
            serverTimeOffset = serverTime - DateTime.UtcNow;
        }
        else
        {
            Debug.LogError("setServerTime() failed");
            errorReadingValues = true;
        }
    }

    private void setQuarterEndTime()
	{
        if (errorReadingValues)
            return;
        
		JSValue js_minutes_tick = ExecuteJavaScriptWithResult(RowElementJavaCommand(5));

        if (IsValidJavaResult(js_minutes_tick))
        {
            JSValue js_last_tick = ExecuteJavaScriptWithResult(RowElementJavaCommand(6));
            if(IsValidJavaResult(js_last_tick))
            {
                float minutes_tick = (float)js_minutes_tick.ToDouble();
                manualQuarterAdvancement = Math.Abs(minutes_tick) <= 0.001f; // close to zero -- accounting for floating point error
                if (!manualQuarterAdvancement)
                    quarterEndTime = IMMsgStringToDateTime(js_last_tick.ToString()) + TimeSpan.FromMinutes(minutes_tick);
            }
        }
        else
            manualQuarterAdvancement = true;
	}
	
	private DateTime IMMsgStringToDateTime(string msgStr)
    {
        if (String.IsNullOrEmpty(msgStr))
		{
			Debug.Log ("Bad DateTime from msg");
            return new DateTime();
		}
        return DateTime.ParseExact(msgStr, "yyyy'-'MM'-'dd' 'HH':'mm':'ss", CultureInfo.InvariantCulture);
    }

    private void ReadServerData()
    {
        //setServerTime();
        setCurrentTick();
        setQuarterEndTime();
        setUserRank();
        setUserScore();
        setScreenVis();

        if (quarterHasChanged || firstLoad)
            GameGUI.Inst.guiLayer.UpdateTimer();

        if (quarterHasChanged)
        {
            RaiseQuarterChangeEvent();
            quarterHasChanged = false;
        }

        if (errorReadingValues)
        {
            errorReadingValues = false;
            HandleErrorReadingValues();
        }
        else
            readDataFailCount = 0;
    }

    private void ReadServerDataDelayed(float waitSeconds)
    {
        enableRefreshAttemptTime = DateTime.Now.AddSeconds(waitSeconds + 2.0f); // don't allow a refresh while we're attempting to read again.
        StartCoroutine(ReadServerDataDelayedImpl(waitSeconds));
    }

    IEnumerator ReadServerDataDelayedImpl(float waitSeconds)
    {
        yield return new WaitForSeconds(waitSeconds);
        ReadServerData();
    }

	private void OnLoadCompleted(System.Object sender, System.EventArgs args)
	{
        if (HandleSystemMessage())
            return;

        ReadServerData();
        firstLoad = false;
        lastRefreshTime = DateTime.Now;
	}

    private void HandleErrorReadingValues()
    {
        readDataFailCount++;
        int maxReadFailsBeforeRefresh = 3;
        if (readDataFailCount <= maxReadFailsBeforeRefresh)
        {
            // attempt to read the data again shortly
            float readServerDataDelay = (readDataFailCount == maxReadFailsBeforeRefresh) ? 1.0f : 0.1f * readDataFailCount;
            ReadServerDataDelayed(readServerDataDelay);
            Debug.LogError(readDataFailCount + " Failed to read ServerData, reading data again in " + readServerDataDelay);
        }
        else
        {
            // refresh and read again after refresh completes
            int maxRefreshFailBeforeLongerDelay = maxReadFailsBeforeRefresh + 5;
            float refreshDelay = (readDataFailCount > maxRefreshFailBeforeLongerDelay) ? 5.0f * BizSimScreen.refreshDelaySeconds : BizSimScreen.refreshDelaySeconds;
            RefreshDelayed(refreshDelay);
            Debug.LogError(readDataFailCount + " Failed to refresh ServerData, refresh in " + refreshDelay);
        }
    }

    private bool HandleSystemMessage()
    {
        // If a refresh occurs while industry masters is updating its servers a System Message appears and a refresh is needed.
        if (IsSystemMessageDisplayed())
        {
            Debug.LogError("* Caught system message *");
            RefreshDelayed(BizSimScreen.refreshDelaySeconds);
            return true;
        }
        return false;
    }

    private bool IsSystemMessageDisplayed()
    {
        string jsCmd = "var headers = document.getElementsByTagName(\"h1\"); if( headers != null && headers.length > 0){headers[0].innerText;}";
        JSValue result = ExecuteJavaScriptWithResult(jsCmd);
        if (IsValidJavaResult(result))
        {
            string header = result.ToString();
            return header.Contains("System Message");
        }
        return false;
    }

    public void AttemptRefresh()
    {
        if (enableRefreshAttemptTime > DateTime.Now)
            return;
        refreshAttemptSinceQuarterChangeCount++;
        int maxAttemptsToRefreshImmediately = 4;
        if (refreshAttemptSinceQuarterChangeCount < maxAttemptsToRefreshImmediately)
            RefreshWebView();
        else
        {
            float secondsSinceLastQuarterChange = (float)DateTime.Now.Subtract(lastQuarterChangeTime).TotalSeconds;
            float refreshDelay = (secondsSinceLastQuarterChange > 60) ? 3.0f * BizSimScreen.refreshDelaySeconds : BizSimScreen.refreshDelaySeconds;
            float secondsSinceLastRefresh = (float)DateTime.Now.Subtract(lastRefreshTime).TotalSeconds;
            if (secondsSinceLastRefresh > refreshDelay)
                RefreshWebView();
        }
    }

    public void RefreshWebView()
    {
        if (CheckWebView() && !webView.IsLoadingPage)
        {
            VDebug.Log("Refresh server data");
            webView.Reload();
        }
    }

    public void RefreshDelayed(float waitSeconds)
    {
        enableRefreshAttemptTime = DateTime.Now.AddSeconds(waitSeconds + 0.5f);
        StartCoroutine(RefreshDelayedImpl(waitSeconds));
    }

    IEnumerator RefreshDelayedImpl(float waitSeconds)
    {
        yield return new WaitForSeconds(waitSeconds);
        RefreshWebView();
    }

    private void RaiseQuarterChangeEvent()
    {
        refreshAttemptSinceQuarterChangeCount = 0;
        lastQuarterChangeTime = DateTime.Now;
        try
        {
            if (QuarterChange != null)
                QuarterChange(this, new System.EventArgs());
        }
        catch
        {
            Debug.Log("Exception raised throwing QuarterChange event");
        }
    }

    private void OnDestroy()
    {
        WebViewManager.Inst.CloseWebView(webView);
    }
}