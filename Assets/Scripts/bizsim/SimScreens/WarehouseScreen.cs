using UnityEngine;
using System.Collections;

public class WarehouseScreen : BizSimScreen {
    public static WarehouseScreen mInst = null;
    public static WarehouseScreen Inst { get { return mInst; } }

    protected override void Awake()
    {
        base.Awake();
        stageItem = 80;
        bssId = CollabBrowserId.WAREHOUSE;
        url = BizSimScreen.GetStageItemURL(stageItem);
        mInst = this;
    }
}
