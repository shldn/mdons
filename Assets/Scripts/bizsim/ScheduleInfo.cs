using System;
using System.Collections.Generic;
using Boomlagoon.JSON;


public class Quarter
{
    public Quarter(JSONObject obj)
    {
        number = Convert.ToInt32(obj.GetString("Quarter"));
        endTime = obj.GetString("EndTime");
        gameID = obj.GetString("bizsimGameId");
    }
    public int number;
    public string gameID;
    public string endTime;
}

public class ScheduleInfo
{

    private Dictionary<string, List<Quarter>> quarters = new Dictionary<string, List<Quarter>>(); // gameID --> List<Quarter>
    private static ScheduleInfo mInstance;
    public static ScheduleInfo Inst
    {
        get
        {
            if (mInstance == null)
                mInstance = new ScheduleInfo();
            return mInstance;
        }
    }

    private ScheduleInfo()
    {
        Init();
    }

    private void Init()
    {
        JSONArray scheduleArray = ParseDAOFactory.GetAllRows("Schedule");
        if (scheduleArray == null)
            return;
        foreach (JSONValue value in scheduleArray)
        {
            Quarter newQtr = new Quarter(value.Obj);
            if (!quarters.ContainsKey(newQtr.gameID))
                quarters.Add(newQtr.gameID, new List<Quarter>());
            quarters[newQtr.gameID].Add(newQtr);
        }
    }

    public List<Quarter> GetQuarters(string gameID)
    {
        if (quarters.Count == 0)
            Init();
        List<Quarter> quarterList = null;
        quarters.TryGetValue(gameID, out quarterList);
        return quarterList;
    }

    public static void Destroy()
    {
        mInstance = null;
    }
}

