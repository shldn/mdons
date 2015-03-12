using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Boomlagoon.JSON;

public class UserProfile {

	public UsersDAO userDao = null;
	private string userID = null;
	private string username = null;
    private string displayName = null;
	private string password = null;
	private string email = null;
    private string teamID = null;
    private string model = null;
    private string room = null; // last room
	private string sessionToken = null;
    private string serverConfig = null;
	private string rawJson = null;
    private int tutorialCode = -1;

    private string hwi_device = null;
    private string hwi_processor = null;
    private string hwi_os = null;
    private string hwi_gfx = null;
    private string hwi_ram = null;

    public string hwi_mic = null;

    private int refreshErrorCount = 0;
	private bool initialized = false;
    private bool readOnly = false;
	
	public UserProfile()
	{
		userDao = new UsersDAO();
		Logout();
	}

    public void InitFromColumnValue(string jsonQueryStr)
    {
        readOnly = true;
        string jsonResult = userDao.GetUserByColumnValue(jsonQueryStr);
        JSONObject jsonArrayObject = JSONObject.Parse(jsonResult);
        JSONArray resArr = jsonArrayObject.GetArray("results");
        RefreshUserSession((resArr.Length > 0) ? resArr[0].ToString() : "");
    }
	
	public string UserID
	{
		get { return userID; }
	}
	public string Username
	{
		get { return username; }
	}
    public string DisplayName
    {
        get { return displayName; }
    }
	public string Email
	{
		get { return email; }
		//TODO: make setters
	}
    public string TeamID
    {
        get { return teamID; }
    }
    public string Model
    {
        get { return model; }
    }
    public string Room
    {
        get { return room; }
    }
    public string SessionToken
	{
		get { return sessionToken; }
	}
    public string ServerConfig
    {
        get { return serverConfig; }
    }
	public string RawJson
	{
		get { return rawJson; }
	}
    public int TutorialCode
    {
        get { return tutorialCode; }
    }
	public bool Initialized
	{
		get { return initialized; }
	}

    private void UpdateMemberVariables(JSONObject jsonObject)
    {
        userID = jsonObject.GetString("objectId");
        username = jsonObject.GetString("username");
        displayName = jsonObject.GetString("displayname");
        email = jsonObject.GetString("email");
        teamID = jsonObject.GetString("teamid");
        model = jsonObject.GetString("model");
        room = jsonObject.GetString("room");
        serverConfig = jsonObject.GetString("server");
        tutorialCode = (int)jsonObject.GetNumber("tut");

        hwi_device = jsonObject.GetString("hwi_device");
        hwi_processor = jsonObject.GetString("hwi_processor");
        hwi_mic = jsonObject.GetString("hwi_mic");
        hwi_os = jsonObject.GetString("hwi_os");
        hwi_gfx = jsonObject.GetString("hwi_gfx");
        hwi_ram = jsonObject.GetString("hwi_ram");
    }

	public string RefreshUserSession(string json)
	{
		if (json == "")
			return json;
		else
		{
			JSONObject jsonObject = JSONObject.Parse(json);
			sessionToken = jsonObject.GetString("sessionToken");
            if (sessionToken != "" || readOnly)
			{
                UpdateMemberVariables(jsonObject);
				rawJson = json;
				initialized = true;
			}
			return sessionToken;
		}
	}
	
	public string CreateProfile(string u, string p)
	{
		username = u;
		password = p;
		string retBody = userDao.Create(username, password);
		return RefreshUserSession(retBody);
	}
	
	public string Login(string username, string password)
	{
		string retBody = userDao.Login(username, password);
		string loginResult = RefreshUserSession(retBody);
		if (loginResult != "")
		{
			return loginResult;
		}
		else
			return retBody;
	}
	
	public void Logout()
	{
        UpdateMemberVariables(new JSONObject());
		initialized = false;
	}
	
	public string PasswordReset(string email)
	{
		return userDao.PasswordReset("{\"email\":\"" + email + "\"}");
	}

    public void IncrementLoginCount()
    {
        if (CheckLogin())
            userDao.IncrementColumn(userID, sessionToken, "logins");
    }

    public void IncrementColumn(string columnName, int amt = 1)
    {
        if (CheckLogin())
            userDao.IncrementColumn(userID, sessionToken, columnName, amt);
    }
	
	public string UpdateProfile(string k, string v)
	{
		return UpdateProfile("{\"" + k +"\":\"" + v + "\"}");
	}
	
	public string UpdateProfile(string jsonData, bool refreshAfterUpdate = true)
	{
		if (CheckLogin())
		{
            string ret = (!refreshAfterUpdate) ? "" : "UpdateProfile failed!";
			string updateRes = userDao.UpdateUser(userID, sessionToken, jsonData);
            if (refreshAfterUpdate && updateRes.IndexOf("updatedAt") > -1)
				ret = Refresh();
			return ret;
		}
		else
			return "Error UpdateProfile Failed!\nJSON: " + jsonData;
	}
	
	public string DeleteProfile()
	{
		if (CheckLogin())
		{
			string ret = userDao.DeleteUser(userID, sessionToken);
			Logout();
			return ret;
		}
		else
			return "Error DeleteProfile Failed!";
	}
	
	private string Refresh()
	{
		if (CheckLogin())
		{
			rawJson = userDao.GetUser(userID);
			if (rawJson != "")
			{
				JSONObject jsonObject = JSONObject.Parse(rawJson);
                UpdateMemberVariables(jsonObject);
                refreshErrorCount = 0;
            }
            else
            {
                Debug.LogError("Refresh Profile failed");
                if (refreshErrorCount++ < 1)
                    return Refresh();
                return "Refresh Failed!";
            }
		}
		return rawJson;
	}

    public string GetField(string fieldName)
    {
        if (initialized)
        {
            JSONObject jsonObject = JSONObject.Parse(rawJson); // shouldn't need to do this every time
            JSONValue jVal = jsonObject[fieldName];
            return (jVal == null) ? "" : jVal.Str;
        }
        return "";
    }
	
	public bool CheckLogin()
	{
		if ((sessionToken == "" || userID == "") && username != "")
		{
			Login(username, password);
			return true;
		}
		else if (initialized && sessionToken != "")
			return true;
		else
		{
			initialized = false;
			return false;
		}
	}

    public bool HasTutorialBeenDisplayed(int tutorialID)
    {
        return (tutorialCode & (1 << tutorialID)) != 0;
    }

    public void UpdateTutorial(int tutorialID)
    {
        // pack tutorial codes into one int.
        int thisTutorialCode = (1 << tutorialID);

        // Has this tutorial been displayed?
        if (!HasTutorialBeenDisplayed(tutorialID))
            IncrementColumn("tut", thisTutorialCode);

        // Make sure local copy is up to date.
        tutorialCode |= thisTutorialCode;
    }

    public void UpdateLocalHardwareInfo()
    {
        hwi_device = SystemInfo.deviceModel;
        hwi_processor = SystemInfo.processorType;
        hwi_os = SystemInfo.operatingSystem;
        hwi_gfx = SystemInfo.graphicsDeviceName;
        hwi_ram = (SystemInfo.systemMemorySize * 0.001) + "gb";
    }

    public string GetLocalHardwareInfo()
    {
        string userHardwareInfo = "";
        userHardwareInfo += "hwi_device: " + hwi_device + "\n";
        userHardwareInfo += "hwi_processor: " + hwi_processor + "\n";
        userHardwareInfo += "hwi_os: " + hwi_os + "\n";
        userHardwareInfo += "hwi_gfx: " + hwi_gfx + "\n";
        userHardwareInfo += "hwi_ram: " + hwi_ram;
        return userHardwareInfo;
    }

    public static string GetLocalHardwareInfoJSON()
    {
        string userJsonHardwareInfo = "{";
        userJsonHardwareInfo += "\"" + "hwi_device" + "\":\"" + SystemInfo.deviceModel + "\",";
        userJsonHardwareInfo += "\"" + "hwi_processor" + "\":\"" + SystemInfo.processorType + "\",";
        userJsonHardwareInfo += "\"" + "hwi_os" + "\":\"" + SystemInfo.operatingSystem + "\",";
        userJsonHardwareInfo += "\"" + "hwi_gfx" + "\":\"" + SystemInfo.graphicsDeviceName + "\",";
        userJsonHardwareInfo += "\"" + "hwi_ram" + "\":\"" + SystemInfo.systemMemorySize + "\"";
        userJsonHardwareInfo += "}";
        return userJsonHardwareInfo;
    }

    public void UpdateParseHardwareInfo()
    {
        UpdateProfile(GetLocalHardwareInfoJSON());
    }
}
