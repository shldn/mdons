using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Boomlagoon.JSON;

public class Team
{
    public Team(JSONObject obj)
    {
        jsonObj = obj;
        id = Convert.ToInt32(obj.GetString("teamID"));
        name = obj.GetString("name");
        className = obj.GetString("class");
        notesURL = obj.GetString("notesUrl");
        gameID = obj.GetString("bizsimGameId");
        simType = obj.GetString("simType");

        JSONObject teacherPtr = obj.GetObject("facilitator");
        teacherID = (teacherPtr != null) ? teacherPtr.GetString("objectId") : "";
    }
    public int id;
    public string name;
    public string className;
    public string notesURL;
    public string gameID;
    public string simType;
    public string teacherID;
    public JSONObject jsonObj;
}

public class TeamInfo {

    Dictionary<int, Team> teams = new Dictionary<int, Team>(); // team id --> Team
    string teamClassJsonStr = "";

    private static TeamInfo mInstance;
    public static TeamInfo Inst
    {
        get
        {
            if (mInstance == null)
                mInstance = new TeamInfo();
            return mInstance;
        }
    }

    private TeamInfo()
    {
        Reload();
    }

    // pull the latest from parse
    public void Reload()
    {
        teams.Clear();
        string serverForTeams = (!string.IsNullOrEmpty(CommunicationManager.shareTeams)) ? CommunicationManager.shareTeams : GameManager.Inst.ServerConfig;
        teamClassJsonStr = ParseDAOFactory.GetAllByColumnValue("Team", "server", serverForTeams); // when we want unique notes across different servers, use this instead TODO (requires forcing new build for everyone)
        JSONArray teamArray = ParseDAOFactory.GetAllRowsFromJson(teamClassJsonStr);
        foreach (JSONValue value in teamArray)
        {
            Team newTeam = new Team(value.Obj);
            teams.Add(newTeam.id, newTeam);
        }
    }

    public string GetName(int teamID)
    {
        return teams[teamID].name;
    }

    public string GetBizSimGameId(int teamID)
    {
        return teams[teamID].gameID;
    }

    public string GetSimType(int teamID)
    {
        return teams[teamID].simType;
    }

    public string GetNotesURL(int teamID)
    {
        return teams[teamID].notesURL;
    }

    public string GetTeacherID(int teamID)
    {
        return teams[teamID].teacherID;
    }

    public string GetClassName(int teamID)
    {
        return teams[teamID].className;
    }

    public string GetVariable(int teamID, string varName)
    {
        return teams[teamID].jsonObj.GetString(varName);
    }

    public bool TeamExists(int teamID)
    {
        return teams.ContainsKey(teamID);
    }

    public int GetFirstTeamInGame(string gameID)
    {
        Team retVal = null;
        foreach (KeyValuePair<int, Team> tPair in teams)
        {
            if (tPair.Value.gameID == gameID && (retVal == null || tPair.Value.id < retVal.id))
                retVal = tPair.Value;
        }
        return retVal != null ? retVal.id : -1;
    }

    public string GetTeamClassJSON()
    {
        return teamClassJsonStr;
    }
}
