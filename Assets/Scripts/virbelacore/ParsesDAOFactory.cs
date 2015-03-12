using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Boomlagoon.JSON;

public class ParseDAO
{
    private RESThelper restHelper = null;
    public string parseClassName = "";

    public ParseDAO(string name, bool createObjectIfDoesnotExist=true)
    {
        parseClassName = name;

        //init RESThelper
        restHelper = new RESThelper("https://api.parse.com/1/", new Dictionary<string, string>());
        restHelper.AddHeader("X-Parse-Application-Id", "zs5W1kr9RMFlrHaTLrkY8Rxz8W44TXbhZ8TPvdzq");
        restHelper.AddHeader("X-Parse-REST-API-Key", "ZVekLieNrfFwb9H6dCiYZaOr5dlMPw01VY1Zlvgr");

        if (createObjectIfDoesnotExist)
            Create();
    }

    public string Create(string jsonData="")
    {
        return restHelper.sendRequest("classes/" + parseClassName, "POST", jsonData);
    }

    public string GetObject(string objectId)
    {
        return restHelper.sendRequest("classes/" + parseClassName + "/" + objectId, "GET");
    }

    public string GetAllObject()
    {
        return restHelper.sendRequest("classes/" + parseClassName, "GET");
    }

    public string UpdateObject(string objectId, string jsonData)
    {
        return restHelper.sendRequest("classes/" + parseClassName + "/" + objectId, "PUT", jsonData);
    }

    public string DeleteObject(string objectId)
    {
        return restHelper.sendRequest("classes/" + parseClassName + "/" + objectId, "DELETE");
    }

    public string FindByColumnValue(string c, string v)
    {
        string json = "{\"" + c + "\":\"" + v + "\"}";
        VDebug.Log("FindByColumnValue json: " + json);
        return FindByColumnValue(json);
    }

    public string FindByColumnValue(string jsonQuery)
    {
        return restHelper.sendRequest("classes/" + parseClassName, "GET", "where=" +  System.Uri.EscapeDataString(jsonQuery));
    }
}

public static class ParseDAOFactory
{
    public static ParseDAO CreateDAO(string classname= "", bool createObjectIfDoesnotExist = true)
    {
        ParseDAO obj = new ParseDAO(classname, createObjectIfDoesnotExist);
        return obj;
    }

    public static string GetAllObject(string classname)
    {
        ParseDAO obj = new ParseDAO(classname, false);
        return obj.GetAllObject();
    }

    public static string GetAllByColumnValue(string classname, string c, string v)
    {
        ParseDAO obj = new ParseDAO(classname, false);
        return obj.FindByColumnValue(c, v);
    }

    public static string GetAllByColumnValue(string classname, string jsonQuery)
    {
        ParseDAO obj = new ParseDAO(classname, false);
        return obj.FindByColumnValue(jsonQuery);
    }

    public static JSONArray GetAllRows(string classname)
    {
        return GetAllRowsFromJson(GetAllObject(classname));
    }

    public static JSONArray GetAllRowsFromJson(string jsonStr)
    {
        JSONObject jsonObject = JSONObject.Parse(jsonStr);
        if (jsonObject == null)
            return null;
        return jsonObject.GetArray("results");
    }
}
