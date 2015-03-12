using UnityEngine;
using System.Collections;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Variables;
using System.Collections.Generic;

public class RoomVariableToEnable : MonoBehaviour {

    public string roomVariableName = "";
    public bool defaultVisible = false;

    private static Dictionary<string, List<RoomVariableToEnable>> allRmVarToEnables = new Dictionary<string, List<RoomVariableToEnable>>();

    void Awake()
    {
        List<RoomVariableToEnable> rmVarToEnableList;
        if (allRmVarToEnables.TryGetValue(roomVariableName, out rmVarToEnableList))
            rmVarToEnableList.Add(this);
        else
        {
            rmVarToEnableList = new List<RoomVariableToEnable>() { this };
            allRmVarToEnables.Add(roomVariableName, rmVarToEnableList);
        }

        gameObject.SetActive(defaultVisible);

    }

    void OnDestroy()
    {
        List<RoomVariableToEnable> rmVarToEnableList;
        if (allRmVarToEnables.TryGetValue(roomVariableName, out rmVarToEnableList))
            rmVarToEnableList.Remove(this);
    }

    public static void HandleRoomVariableUpdate(BaseEvent evt)
    {
        List<RoomVariableToEnable> rmVarToEnableList;
        Room room = (Room)evt.Params["room"];

        ArrayList changedVars = (ArrayList)evt.Params["changedVars"];
        foreach (string varName in changedVars)
        {
            if (allRmVarToEnables.TryGetValue(varName, out rmVarToEnableList))
                for (int i = 0; i < rmVarToEnableList.Count; ++i)
                    rmVarToEnableList[i].gameObject.SetActive(room.GetVariable(varName).GetBoolValue());
        }
    }

    public static void HandleRoomVariableUpdate(UserVariable userVar)
    {
        List<RoomVariableToEnable> rmVarToEnableList;
        if (allRmVarToEnables.TryGetValue(userVar.Name, out rmVarToEnableList))
            for (int i = 0; i < rmVarToEnableList.Count; ++i)
                rmVarToEnableList[i].gameObject.SetActive(userVar.GetBoolValue());
    }

    public static void HandleRoomJoin(BaseEvent evt)
    {
        if (evt == null)
            return;
        Room room = (Room)evt.Params["room"];
        foreach (RoomVariable rmVar in room.GetVariables())
            HandleRoomVariableUpdate(rmVar);
    }
}
