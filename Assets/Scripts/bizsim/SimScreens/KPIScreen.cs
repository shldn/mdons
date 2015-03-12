using UnityEngine;
using System.Collections;

public class KPIScreen : BizSimScreen {
    private int chartHeight = 35;
    public static KPIScreen mInst = null;
    public static KPIScreen Inst { get { return mInst; } }

    protected override void Awake()
    {
        base.Awake();
        stageItem = 7;
        bssId = CollabBrowserId.KPI;
        url = BizSimScreen.GetStageItemURL(stageItem);
        url += "&override_chartheight=" + chartHeight;
        mInst = this;
    }
}
