using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Boomlagoon.JSON;

public class ParseRoomVarManager {

    Dictionary<string, JSONObject> roomEntries = new Dictionary<string, JSONObject>(); // room name --> room entry
    private static ParseRoomVarManager mInstance;
    public static ParseRoomVarManager Inst {
        get {
            if (mInstance == null)
                mInstance = new ParseRoomVarManager();
            return mInstance;
        }
    }

    private ParseRoomVarManager() {
        JSONArray roomEntryArr = ParseObjectFactory.GetParseObjectsByColumnValue("Room", "serverConfig", GameManager.Inst.ServerConfig);
        foreach(JSONValue val in roomEntryArr)
        {
            if (val.Obj != null && val.Obj.ContainsKey("name"))
                roomEntries[val.Obj.GetValue("name").Str] = val.Obj;
            else
                Debug.LogError("Entry does not contain name key: " + val.ToString());
        }
    }

    public JSONObject GetRoomInfo(string rmName)
    {
        JSONObject obj;
        if( roomEntries.TryGetValue(rmName, out obj ) )
            return obj;
        return null;
    }

    public string GetRoomVal(string rmName, string varName)
    {
        JSONObject rmInfo = GetRoomInfo(rmName);
        if (rmInfo != null)
        {
            Debug.LogError("Got room info");
            return rmInfo.GetString(varName);
        }
        Debug.LogError("Did not get var " + varName + " for room: " + rmName);
        return "";
    }

    public static void Destroy()
    {
        mInstance = null;
    }
}
