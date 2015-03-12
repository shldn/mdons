using UnityEngine;
using System.Collections;

// Info messages will display for a short time and then disappear
public class InfoMessageManager {

    public static void Display(string msg)
    {
        string cmd = "showInfoMessage('"  + msg + "');";
        if (!GameGUI.Inst.ExecuteJavascriptOnGui(cmd))
        {
            Debug.LogError("Info message failed, giving console message.");
            GameGUI.Inst.WriteToConsoleLog(msg);
        }
    }
}
