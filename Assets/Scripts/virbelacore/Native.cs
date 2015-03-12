using UnityEngine;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class Native {

    public static void StartWindowsProcess(string filename, string arguments)
    {
        bool isWindows = Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor;
        if (isWindows)
        {
#if !UNITY_WEBPLAYER
            System.Diagnostics.ProcessStartInfo procStartInfo = new System.Diagnostics.ProcessStartInfo(filename, arguments);
            procStartInfo.UseShellExecute = false;
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo = procStartInfo;
            proc.Start();
#endif
        }
        else
            UnityEngine.Debug.LogError("StartWindowsProcess: unsupported platform: " + Application.platform);
    }

    public static void OpenMicSettings()
    {
        StartWindowsProcess("Control.exe", "-name Microsoft.Sound -page 1");
    }

    public static void OpenSpeakerSettings()
    {
        StartWindowsProcess("Control.exe", "-name Microsoft.Sound -page 0");
    }

}
