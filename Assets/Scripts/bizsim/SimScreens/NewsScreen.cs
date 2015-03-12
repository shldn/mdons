using UnityEngine;
using System.Collections;

// newsticker
public class NewsScreen : BizSimScreen {
    private int numItemsToDisplay = 20;
    protected override void Awake()
    {
        base.Awake();
        stageItem = 5;
        bssId = CollabBrowserId.NEWS;
        url = BizSimScreen.GetStageItemURL(stageItem);
        url += "&num_items=" + numItemsToDisplay;
        blacklistRequestURLFragments.Add("holdinginfo.php?playerid");
    }
}
