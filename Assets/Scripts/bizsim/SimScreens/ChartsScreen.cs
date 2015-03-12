using UnityEngine;
using System.Collections;

public class ChartsScreen : CompDataScreen {
    protected override void Awake()
    {
        base.Awake();
        stageItem = 200;
        bssId = CollabBrowserId.CHARTS;
        url = BizSimScreen.GetStageItemURL(stageItem);
    }
}
