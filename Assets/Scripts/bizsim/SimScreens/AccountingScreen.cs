using UnityEngine;

public class AccountingScreen : BizSimScreen {
    protected override void Awake()
    {
        base.Awake();
        disableScrolling = false;
        stageItem = 300;
        bssId = CollabBrowserId.ACCOUNTING;
        url = BizSimScreen.GetStageItemURL(stageItem);
    }
}
