using UnityEngine;
using System.Collections;
using Awesomium.Mono;

public class SustainScreen : BizSimScreen {
    private int investCount = 0; // number of investments that have been made (OK buttons that have turned into checkmarks)
    float[] mouseUpCheckWaitTime = { 1.0f, 2.0f }; // seconds (time to wait after a click for the change to propogate, then check the new values)
    public static SustainScreen mInst = null;
    public static SustainScreen Inst { get { return mInst; } }

    protected override void Awake()
    {
        base.Awake();
        stageItem = 60;
        bssId = CollabBrowserId.SUSTAIN;
        url = BizSimScreen.GetStageItemURL(stageItem);
        refreshOnInvestmentBudgetChange = true;
        mInst = this;
    }

    public override void Initialize()
    {
        base.Initialize();
        bTex.AddLoadCompleteEventListener(OnLoadComplete);
    }

    private void OnLoadComplete(System.Object sender, System.EventArgs args)
    {
        SetSustainStage();
        UpdateInvestCount();
    }

    void OnGUI()
    {
        if (Event.current != null && Event.current.type == EventType.MouseUp && MouseHelpers.GetCurrentGameObjectHit() == gameObject)
        {
            StartCoroutine(HandleMouseUpDelayed(mouseUpCheckWaitTime[0]));
            StartCoroutine(HandleMouseUpDelayed(mouseUpCheckWaitTime[1]));
        }
    }

    public IEnumerator HandleMouseUpDelayed(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        if( UpdateInvestCount() )
            SendRefreshMeMessage();
    }

    private void SetSustainStage()
    {
        int stage = -1;

        JSValue result = bTex.ExecuteJavaScriptWithResult("var elem = document.getElementsByClassName('opcao'); if (elem != null) {elem[0].innerText;}");
        if (IsValidJavaResult(result))
        {
            if (result.ToString().ToLower().Contains("pollution prevention"))
                stage = 1;
            else if (result.ToString().ToLower().Contains("design for environment"))
                stage = 2;
            else if (result.ToString().ToLower().Contains("eco-effectiveness"))
                stage = 3;
            else if (result.ToString().ToLower().Contains("base of the pyramid"))
                stage = 4;
            else
                stage = 5;
        }
        else
            stage = 5;

        TreeDisplayManager.Inst.SetLevel(stage);
    }

    private bool UpdateInvestCount()
    {
        bool retVal = false;
        string jsCmd = "var imgs = document.getElementsByTagName(\"img\");var n = 0; for (var i = 0; i < imgs.length; ++i) { if (imgs[i].src.lastIndexOf(\"opcao_ok.gif\") != -1)++n; } n;";
        JSValue result = bTex.ExecuteJavaScriptWithResult(jsCmd);
        if (result != null && result.Type != JSValueType.Null)
        {
            int newInvestCount = result.ToInteger();
            retVal = newInvestCount != investCount;
            investCount = newInvestCount;
        }
        return retVal;
    }
}
