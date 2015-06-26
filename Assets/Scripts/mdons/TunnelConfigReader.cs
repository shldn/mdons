using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using Boomlagoon.JSON;

public class Experiment
{
    public Experiment(JSONObject obj)
    {
        foreach (KeyValuePair<string, JSONValue> v in obj)
            SetValue(v.Key, v.Value);
    }

    public void SetValue(string name, JSONValue v)
    {
        name = name.ToLower();
        if (name == "angle")
            angle = (float)v.Number;
        else if (name == "avatar")
            SetAvatar(v.Str);
        else if (name == "arrows")
            SetArrows(v.Str);
        else if (name == "navigation")
            SetNavigation(v.Str);
        else if (name == "choicemethod")
            SetChoiceMethod(v.Str);
        else if(name == "autostartdelay")
            autoStartDelay = (float)v.Number;
    }

    public void SetAvatar(string str)
    {
        avatarVisible = (str.ToLower().Trim() == "visible");
        avatarPixelated = (str.ToLower().Trim() == "abstract");
    }

    public void SetArrows(string str)
    {
        chooseArrow = (str.ToLower().Trim() == "two");
    }

    public void SetNavigation(string str)
    {
        str = str.ToLower().Trim();
        if (str == "one key")
            userControl = UserControl.PARTIAL;
        else if (str == "auto")
            userControl = UserControl.NONE;
        else // "full"
            userControl = UserControl.FULL;
    }

    public void SetChoiceMethod(string str)
    {
        mouseClickToChoose = (str.ToLower().Trim() == "click");
    }

    public float angle = 30f;
    public float autoStartDelay = -1f;
    public bool chooseArrow = false;
    public bool avatarVisible = true;
    public bool avatarPixelated = false;
    public bool mouseClickToChoose = true;
    public UserControl userControl = UserControl.NONE;
}

public class TunnelConfigReader {

    public static string instructions = "";
    public static int breakAfter = -1;
    public static int skipCount = 0;

    public static List<Experiment> Read(string filePath)
    {
        Dictionary<string, JSONValue> constants = new Dictionary<string, JSONValue>();
        List<Experiment> experiments = new List<Experiment>();
        try
        {
            using (StreamReader sr = new StreamReader(filePath))
            {
                String fileStr = sr.ReadToEnd();
                JSONArray arr = JSONArray.Parse(fileStr);
                if( arr == null )
                {
                    TunnelGameManager.Inst.ErrorMessage = "Problem parsing " + filePath + " probably not valid JSON";
                    return experiments;
                }
                for (int i = 0; i < arr.Length; ++i )
                {
                    Debug.Log("json obj: " + arr[i].ToString());
                    if( arr[i].Obj.GetString("type").Trim() == "trial")
                        experiments.Add(new Experiment(arr[i].Obj));

                    if (arr[i].Obj.GetString("instructions") != "")
                        instructions = arr[i].Obj.GetString("instructions");

                    if (arr[i].Obj.GetValue("breakAfter") != null)
                        breakAfter = (int)arr[i].Obj.GetNumber("breakAfter");

                    if (arr[i].Obj.GetValue("skip") != null)
                        skipCount = (int)arr[i].Obj.GetNumber("skip");

                    if (arr[i].Obj.GetString("type").Trim() == "constants")
                    {
                        foreach(KeyValuePair<string,JSONValue> v in arr[i].Obj)
                            constants.Add(v.Key, v.Value);
                    }
                }
                if( constants.Count != 0)
                {
                    for (int i = 0; i < experiments.Count; ++i)
                        foreach (KeyValuePair<string, JSONValue> v in constants)
                            experiments[i].SetValue(v.Key, v.Value);
                }
            }
        }
        catch (Exception e)
        {
            string errorMsg = "The file could not be read: " + filePath;
            TunnelGameManager.Inst.ErrorMessage = errorMsg;
            Debug.LogError(errorMsg);
        }
        return experiments;
    }
}
