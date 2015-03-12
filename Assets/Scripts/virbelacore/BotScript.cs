using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BotScript {

    private static List<string> cmds = new List<string>();
    public static void AddCmd(string cmd)
    {
        cmds.Add(cmd);
    }

    public static string ToString()
    {
        string str = "";
        foreach(string cmd in cmds)
            str += cmd + "\n";
        return str;
    }
}
