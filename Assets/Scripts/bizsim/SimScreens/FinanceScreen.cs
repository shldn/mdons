using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Awesomium.Mono;

public enum FinanceValueType
{
    NONE = 0,
    LT_DEBT = 1,
    NUM_SHARES = 2
}

public class FinanceDynamicVals
{
    public string repayLTDebt;
    public string numberOfShares;
    public bool initialized = false;
}

public class FinanceScreen : CompDataScreen {
    public static FinanceScreen mInst = null;
    public static FinanceScreen Inst { get { return mInst; } }

    float[] mouseUpCheckWaitTime = { 1.0f, 2.0f }; // seconds (time to wait after a click for the change to propogate, then check the new values)
    FinanceDynamicVals dynamicVals = new FinanceDynamicVals();
    int dirtyFlags = 0;
    bool disableRefreshMeMessageOnLoad = false;

    protected override void Awake()
    {
        base.Awake();
        stageItem = 20;
        bssId = CollabBrowserId.FINANCE;
        url = BizSimScreen.GetStageItemURL(stageItem);
        refreshOnInvestmentBudgetChange = true;
        mInst = this;
    }

    public override void Initialize()
    {
        base.Initialize();
        bTex.AddLoadCompleteEventListener(OnLoadComplete);
    }

    protected override void HandleRefreshMeMessage()
    {
        disableRefreshMeMessageOnLoad = true;
        Refresh();
    }


    private void OnLoadComplete(System.Object sender, System.EventArgs args)
    {
        UpdateDynamicVals();
        if (dirtyFlags == 0)
            disableRefreshMeMessageOnLoad = false; // otherwise set to false after handling the dirtyFlags
    }

    void OnGUI()
    {
        if (Event.current != null && Event.current.type == EventType.MouseUp && MouseHelpers.GetCurrentGameObjectHit() == gameObject)
        {
            StartCoroutine(HandleMouseUpDelayed(mouseUpCheckWaitTime[0]));
            StartCoroutine(HandleMouseUpDelayed(mouseUpCheckWaitTime[1]));
        }
    }

    void Update()
    {
        if (dirtyFlags != 0)
        {
            if (dirtyFlags == (int)FinanceValueType.LT_DEBT)
            {
                if(!disableRefreshMeMessageOnLoad)
                    SendRefreshMeMessage();
            }
            else // num shares or both changed
                HandleInvestmentBudgetChange(false, !disableRefreshMeMessageOnLoad);
            dirtyFlags = 0;
            disableRefreshMeMessageOnLoad = false;
        }
    }

    // returns true if dynamic vals have changed.
    bool UpdateDynamicVals()
    {
        dirtyFlags = (int)FinanceValueType.NONE;
        string jsCmd = "var elem = document.getElementById(\"ajaxDiv_C\"); if(elem != null && elem.firstChild != null) elem.firstChild.textContent";
        string newRepayLTDebt = GetCmdStrResult(jsCmd);
        jsCmd = "var elem = document.getElementById(\"ajaxDiv_finplanning\"); if(elem != null && elem.firstChild != null && elem.firstChild.firstChild != null && elem.firstChild.firstChild.rows[2] != null && elem.firstChild.firstChild.rows[2].firstChild != null && elem.firstChild.firstChild.rows[2].firstChild.firstChild != null && elem.firstChild.firstChild.rows[2].firstChild.firstChild.rows[0] != null && elem.firstChild.firstChild.rows[2].firstChild.firstChild.rows[0].cells[1] != null ) elem.firstChild.firstChild.rows[2].firstChild.firstChild.rows[0].cells[1].textContent";
        string newNumShares = GetCmdStrResult(jsCmd);

        if (dynamicVals.repayLTDebt != newRepayLTDebt && dynamicVals.initialized)
            dirtyFlags |= (int)FinanceValueType.LT_DEBT;
        if (dynamicVals.numberOfShares != newNumShares && dynamicVals.initialized)
            dirtyFlags |= (int)FinanceValueType.NUM_SHARES;

        dynamicVals.repayLTDebt = newRepayLTDebt;
        dynamicVals.numberOfShares = newNumShares;
        dynamicVals.initialized = true;
        return dirtyFlags != (int)FinanceValueType.NONE;
    }

    public IEnumerator HandleMouseUpDelayed(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        UpdateDynamicVals();
    }
	
}
