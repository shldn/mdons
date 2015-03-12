using UnityEngine;
using System.Collections;

public class BankingChartsScreen : BizSimScreen {
    public static BankingChartsScreen mInst = null;
    public static BankingChartsScreen Inst { get { return mInst; } }

    protected override void Awake()
    {
        base.Awake();
        stageItem = 19;
        bssId = CollabBrowserId.BANKINGCHARTS;
        url = BizSimScreen.GetStageItemURL(stageItem);
        mInst = this;
    }
}
