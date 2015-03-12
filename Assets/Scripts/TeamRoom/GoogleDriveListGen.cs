using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Boomlagoon.JSON;

public class GoogleDriveListGen {

    

    public static string errorStr = null;
    public static bool Generate(string googleDriveFolderPath)
    {
        Dictionary<string, List<string>> typeToListMap = new Dictionary<string, List<string>>();

        errorStr = null;
        try
        {
            string[] filePaths = Directory.GetFiles(googleDriveFolderPath);
            foreach (string fileP in filePaths)
            {
                if (fileP.IndexOf(".g") == -1)
                    continue;
                using (StreamReader sr = new StreamReader(fileP))
                {
                    String line = sr.ReadLine();
                    JSONObject obj = JSONObject.Parse(line);
                    string resourceId = obj.GetString("resource_id");
                    if( !string.IsNullOrEmpty(resourceId) )
                    {
                        string url = obj.GetString("url");
                        string type = resourceId.Substring(0,resourceId.IndexOf(':'));
                        List<string> typeList;
                        if (!typeToListMap.TryGetValue(type, out typeList))
                        {
                            typeList = new List<string>();
                            typeToListMap.Add(type, typeList);
                        }
                        typeToListMap[type].Add(url);
                        Debug.LogError("adding " + url + " to " + type);
                    }
                }
            }

            string fileText = "";
            foreach (KeyValuePair<string, List<string>> kvp in typeToListMap)
            {
                if (kvp.Value.Count > 0)
                {
                    fileText += kvp.Key + "\n";
                    fileText += "[";
                }
                for(int i=0; i < kvp.Value.Count; ++i)
                {
                    fileText += "\"" + kvp.Value[i] + "\"";
                    fileText += (i < kvp.Value.Count - 1) ? "," : "]\n";
                }
            }

            if (fileText != "")
            {
                string pathToSaveFile = googleDriveFolderPath + "/doclists.txt";
                GameGUI.Inst.WriteToConsoleLog("Writing list to " + pathToSaveFile);
#if !UNITY_WEBPLAYER
                File.WriteAllText(pathToSaveFile, fileText);
#endif
            }
            else
                errorStr = "no google drive files found - .gdoc, .gsheet, etc.";
        }
        catch (Exception e)
        {
            errorStr = e.ToString();
            return false;
        }
        return true;
    }
}
