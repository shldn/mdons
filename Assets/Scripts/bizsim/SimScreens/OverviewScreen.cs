using UnityEngine;
using Awesomium.Mono;
using System;

// This displays the overview page in the Industry Masters sim.
public class OverviewScreen : BizSimScreen {

    private bool removeAnnouncementTextFromPanel = true;
    private bool showAnnouncementOnRefresh = true;

    protected override void Awake() // called on AddComponent
	{
        base.Awake();
        url = BizSimScreen.BaseURL + "/holdinginfo.php";
        refreshOnInvestmentBudgetChange = true;
        bssId = CollabBrowserId.OVERVIEW;
	}
	
    public void Move(Vector3 delta)
    {
        gameObject.transform.position += delta;
    }

    public override void Initialize()
    {
 	    base.Initialize();
        bTex.LoadComplete += OnLoadCompleted;
        BizSimManager.Inst.AddOnQuarterChangeEventListener(OnQuarterChange);
    }

    private void OnLoadCompleted(System.Object sender, System.EventArgs args)
    {
        string javaCmd = "var elem = document.getElementById(\"scenario_body\"); if( elem != null ) elem.textContent;";
        string announcementStr = GetCmdStrResult(javaCmd);
        if (announcementStr != "")
        {
            if (showAnnouncementOnRefresh)
            {
                AnnouncementManager.Inst.Announce("New Quarter Announcement", announcementStr);
                showAnnouncementOnRefresh = false;
            }
            if (removeAnnouncementTextFromPanel)
            {
                javaCmd = "var elem = document.getElementById(\"scenario_body\"); if( elem != null ){elem.style.display=\"none\"}";
                bTex.ExecuteJavaScript(javaCmd);
            }
        }

        setCO2Values();
    }

    private void setCO2Values()
    {
        float co2Current = -1.0f;
        float co2Cap = -1.0f;
        JSValue result = bTex.ExecuteJavaScriptWithResult("var elem = document.getElementById('ajaxDiv_invbudget').getElementsByTagName('nobr'); if (elem != null) {elem.item(3).firstChild.nodeValue;}");
        if (IsValidJavaResult(result))
            co2Current = (float)result.ToDouble();
        else
            VDebug.LogError("Failed to set current CO2 value.");

        result = bTex.ExecuteJavaScriptWithResult("var elem = document.getElementById('ajaxDiv_invbudget').getElementsByTagName('nobr'); if (elem != null) {elem.item(2).firstChild.nodeValue;}");
        if (IsValidJavaResult(result))
            co2Cap = (float)result.ToDouble();
        else
            VDebug.LogError("Failed to set cap CO2 value.");
        if (co2Cap != -1.0f && co2Current != -1.0f)
            BizSimManager.Inst.Polluting = co2Current > co2Cap;
    }

    void OnQuarterChange(System.Object sender, EventArgs args)
    {
        showAnnouncementOnRefresh = true;
    }
}
