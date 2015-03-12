using UnityEngine;
using System.Collections;

public class VDebug {

    // Only prints when running in editor
    public static void Log(string msg)
    {
#if UNITY_EDITOR
        Debug.Log(msg);
#endif
    }

    // Only prints when running in development mode -- please use sparingly, this is still complied into the code so the if statement is executed not compiled out.
    public static void LogError(string msg)
    {
        if( Debug.isDebugBuild )
            Debug.LogError(msg);
    }

    public static void Console(string msg)
    {
        GameGUI.Inst.WriteToConsoleLog(msg);
    }
}
