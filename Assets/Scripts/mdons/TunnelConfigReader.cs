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
    }

    public void SetAvatar(string str)
    {
        avatarVisible = (str.ToLower() == "visible");
    }

    public void SetArrows(string str)
    {
        chooseArrow = (str.ToLower() == "two");
        Debug.LogError("arrow: " + chooseArrow.ToString());
    }

    public void SetNavigation(string str)
    {
        if (str == "one key")
            userControl = UserControl.PARTIAL;
        else if (str == "auto")
            userControl = UserControl.NONE;
        else // "full"
            userControl = UserControl.FULL;
    }

    public void SetChoiceMethod(string str)
    {
        mouseClickToChoose = (str.ToLower() == "click");
    }

    public float angle = 30f;
    public bool chooseArrow = false;
    public bool avatarVisible = true;
    public bool mouseClickToChoose = true;
    public UserControl userControl = UserControl.NONE;
}

public class TunnelConfigReader {

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
                for (int i = 0; i < arr.Length; ++i )
                {
                    Debug.LogError("json obj: " + arr[i].ToString());
                    if( arr[i].Obj.GetString("type") == "trial")
                        experiments.Add(new Experiment(arr[i].Obj));

                    if( arr[i].Obj.GetString("type") == "constants")
                    {
                        foreach(KeyValuePair<string,JSONValue> v in arr[i].Obj)
                        {
                            Debug.LogError("Setting constant:" + v.Key + " " + v.Value);
                            constants.Add(v.Key, v.Value);
                        }
                    }
                }
                if( constants.Count != 0)
                {
                    Debug.LogError("Hello - setting constants");
                    for (int i = 0; i < experiments.Count; ++i)
                        foreach (KeyValuePair<string, JSONValue> v in constants)
                            experiments[i].SetValue(v.Key, v.Value);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("The file could not be read: " + filePath);
        }
        return experiments;
    }
}
