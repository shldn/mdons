using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Awesomium.Mono;
using Awesomium.Unity;
using Boomlagoon.JSON;

public class HTMLUserListManager : MonoBehaviour {
    void Start()
    {
        InvokeRepeating("CheckUserList", 0, 4.0f);
    }
	void CheckUserList () {
        if (GameManager.DoesLevelHaveSmartFoxRoom(GameManager.Inst.LevelLoaded) && CommunicationManager.InASmartFoxRoom && GameManager.Inst.playerManager != null && GameGUI.Inst.guiLayer != null)
            GameGUI.Inst.ExecuteJavascriptOnGui("checkUserCount(" + GameManager.Inst.playerManager.NumPlayers + ");");
	}

    public void RebuildHTMLUserList()
    {
        if (GameManager.buildType == GameManager.BuildType.REPLAY)
            return;
        string jsCmd = "hideUserList(true);";
        foreach (KeyValuePair<int, Player> playerPair in GameManager.Inst.playerManager)
            jsCmd += playerPair.Value.GetAddToGUIUserListJSCmd();
        jsCmd += "showUserList();";
        GameGUI.Inst.ExecuteJavascriptOnGui(jsCmd);
    }
}
