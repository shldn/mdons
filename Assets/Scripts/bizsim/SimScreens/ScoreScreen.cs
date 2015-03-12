using UnityEngine;
using System.Collections;
using System.IO;

public class ScoreScreen : BizSimScreen {
    protected override void Awake()
    {
        base.Awake();
        stageItem = 6;
        bssId = CollabBrowserId.SCORE;
        url = BizSimScreen.GetStageItemURL(stageItem);
        blacklistRequestURLFragments.Add("holdinginfo.php?playerid");
        requestReplacements.Add(BaseURL + "/images/flags/us.gif", Directory.GetCurrentDirectory() + "/img/blank.gif");
    }

    public override void Initialize()
    {
        base.Initialize();
        bTex.AddLoadCompleteEventListener(OnLoadCompleted);
    }

    private void OnLoadCompleted(System.Object sender, System.EventArgs args)
    {
        StartCoroutine(RemoveWinnersDisplay(0.1f));
    }

    private IEnumerator RemoveWinnersDisplay(float waitSeconds)
    {
        yield return new WaitForSeconds(waitSeconds);
        if( !bTex.isWebViewBusy() )
        {
            string javaCmd = "var winners = document.getElementsByClassName(\"winners\"); if (winners != null && winners.length > 0) winners[0].style.display = \"none\"";
            bTex.ExecuteJavaScript(javaCmd);
        }
    }
}
