using UnityEngine;
using System;

public class StageEventHandler : MonoBehaviour {

	void Start () {

        TriggerArea triggerArea = GetComponent<TriggerArea>();
        if (triggerArea != null)
        {
            triggerArea.TriggerEnter += OnStageEnter;
            triggerArea.TriggerExit += OnStageExit;
        }
        else
            Debug.LogError("No Trigger Area found");
	}

    void OnStageEnter(System.Object sender, EventArgs args)
    {
        string cmd = "stageEnter();";
        GameGUI.Inst.ExecuteJavascriptOnGui(cmd);
    }

    void OnStageExit(System.Object sender, EventArgs args)
    {
        string cmd = "stageExit();";
        GameGUI.Inst.ExecuteJavascriptOnGui(cmd);
    }
}
