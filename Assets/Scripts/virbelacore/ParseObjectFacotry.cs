using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Boomlagoon.JSON;

public class ParseObject    //parse object methods
{
    public string parseClassName = "";
	public ParseDAO parseDao = null;

    private string objectID = null;
    private string rawJson = null;
	private bool initialized = false;

    public ParseObject(string name, bool createObjectIfDoesnotExist=true)
    {
        parseClassName = name;
        parseDao = ParseDAOFactory.CreateDAO(name, createObjectIfDoesnotExist);
    }

    public ParseObject(string name, string json, bool createObjectIfDoesnotExist=true)
    {
        parseClassName = name;
        parseDao = ParseDAOFactory.CreateDAO(name, createObjectIfDoesnotExist);
        RefreshObjectFromJson(json);

    }

    public string ObjectID
    {
        get { return objectID; }
    }
    public string RawJson
	{
		get { return rawJson; }
	}
	public bool Initialized
	{
		get { return initialized; }
	}

    public string RefreshObjectFromJson(string json)
    {
        JSONObject jsonObject = JSONObject.Parse(json);
        objectID = jsonObject.GetString("objectId");
        rawJson = parseDao.GetObject(objectID);
        initialized = true;
        return rawJson;
    }

    public string RetrieveObject(string id)
    {
        objectID = id;
        rawJson = parseDao.GetObject(objectID);
        initialized = true;
        return rawJson;
    }

    public string Update(string k, string v)
    {
        if (initialized)
            return parseDao.UpdateObject(objectID, "{\"" + k +"\":\"" + v + "\"}");
        else
            return "Error Update: Uninitialized Parse object!";
    }

    public string Remove()
    {
        if (initialized)
            return parseDao.DeleteObject(objectID);
        else
            return "Error Remove: Uninitialized Parse object!";
    }
}
public static class ParseObjectFactory  //parse class methods
{
    public static ParseObject CreateParseObject(string name, string json="")
    {
        ParseObject obj = new ParseObject(name, json);
        return obj;
    }

    public static ParseObject FindParseObjectById(string name, string id)
    {
        ParseDAO parseDao = ParseDAOFactory.CreateDAO(name, false);

        string retBody = parseDao.GetObject(id);
        if (retBody != "{\"results\":[]}")
        {
            ParseObject obj = new ParseObject(name, false);
            obj.RetrieveObject(id);
            return obj;
        }
        return null;
    }

    public static ParseObject FindByParseObjectByColumnValue(string name, string c, string v, bool createIfDoesntExist = false)
    {
        string jsonQuery = "{\"" + c + "\":\"" + v + "\"}";
        return FindByParseObjectByColumnValue(name, jsonQuery, createIfDoesntExist);
    }

    public static ParseObject FindByParseObjectByColumnValue(string className, string jsonQuery, bool createIfDoesntExist = false)
    {

        JSONArray res = GetParseObjectsByColumnValues(className, jsonQuery, createIfDoesntExist);
        if( res == null || res.Length == 0 )
        {
            Debug.LogError("FindByParseObjectByColumnValue returned no results");
            return null;
        }
        return new ParseObject(className, res[0].ToString(), false);
    }

    public static JSONArray GetParseObjectsByColumnValue(string className, string c, string v, bool createIfDoesntExist = false)
    {
        string jsonQuery = "{\"" + c + "\":\"" + v + "\"}";
        return GetParseObjectsByColumnValues(className, jsonQuery, createIfDoesntExist);
    }

    public static JSONArray GetParseObjectsByColumnValues(string className, string jsonColumnKVPairs, bool createIfDoesntExist = false) 
    {
        ParseDAO parseDao = ParseDAOFactory.CreateDAO(className, createIfDoesntExist);
        string retBody = parseDao.FindByColumnValue(jsonColumnKVPairs);
        if (retBody != "" && retBody.Substring(0, 12) == "{\"results\":[")
        {
            JSONObject jsonObject = JSONObject.Parse(retBody);
            JSONArray res = jsonObject.GetArray("results");
            return res;
        }
        Debug.LogError("GetParseObjectsByColumnValues Failed " + jsonColumnKVPairs + " Error: " + retBody);
        return null;
    }

    public static bool DoesClassExist(string name)
    {
        string retBody = ParseDAOFactory.GetAllObject(name);
        Debug.Log("DoesClassExist: " + retBody);
        return (retBody != "{\"results\":[]}");
    }
}
