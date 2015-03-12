using UnityEngine;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Util;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class MessageRecorder {

    private string recordFilename = "messages.data";
    private int fileFormatVersion = 5; // 5 = save current level, 4 = save current state of room vars on start, 3 = new avatars, 2 = Player Enter/Exit msgs
    public bool RecordFileCreated { get { return File.Exists(recordFilename); } }

    public MessageRecorder(string filename)
    {
        recordFilename = filename;
        if (string.IsNullOrEmpty(recordFilename))
            recordFilename = GetDefaultFileName();
        if( GameGUI.Inst.guiLayer != null )
            GameGUI.Inst.guiLayer.SendGuiLayerRecordingStart(recordFilename);
    }

    public static string GetDefaultFileName()
    {
        return (GameManager.LevelToShortString(CommunicationManager.LevelToLoad) + "_" + CommunicationManager.Inst.roomNumToLoad.ToString() + "_" + DateTime.Now.ToString("MM-dd-yy-h-mm-ss") + ".virbeo");
    }

    byte GetCurrentRecordedLevel()
    {
        LevelInfo levelInfo = GameManager.Inst.LevelLoadedInfo;
         
        if (levelInfo.level == GameManager.Level.CONNECT)
        {
            VDebug.LogError("Recording from Connection level, try based on smartfox room");
            levelInfo = LevelInfo.GetInfo(CommunicationManager.LevelToLoad);
        }
        return (byte)levelInfo.level;
    }

    void CreateRecordFile(string recordFilename)
    {
#if !UNITY_WEBPLAYER
        using (BinaryWriter writer = new BinaryWriter(File.Open(recordFilename, FileMode.CreateNew, FileAccess.Write)))
        {
            writer.Write(DateTime.UtcNow.ToBinary());
            writer.Write(fileFormatVersion);
            writer.Write(GetCurrentRecordedLevel());
        }
#endif
    }

    private void AddAllCurrentUsers()
    {
#if !UNITY_WEBPLAYER
        using (BinaryWriter writer = new BinaryWriter(File.Open(recordFilename, FileMode.Append, FileAccess.Write)))
        {
            VDebug.LogError("Adding all current users");
            foreach (SFSUser u in CommunicationManager.LastJoinedRoom.UserList)
            {
                if (u != CommunicationManager.MySelf || (RecordingPlayer.Active && RecordingPlayer.Inst.RecordMyActions))
                    RecordUserEnterExit(writer, u, SFSEvent.USER_ENTER_ROOM, CommunicationManager.LastJoinedRoom);
            }
        }
#endif
    }

    private void AddAllCurrentRoomVariables()
    {
#if !UNITY_WEBPLAYER
        using (BinaryWriter writer = new BinaryWriter(File.Open(recordFilename, FileMode.Append, FileAccess.Write)))
        {
            foreach( Room rm in CommunicationManager.JoinedRooms)
                RecordRoomVariables(writer, rm);
        }
#endif
    }

    private bool Init(string evtType)
    {
        if (!File.Exists(recordFilename))
        {
            CreateRecordFile(recordFilename);
            AddAllCurrentUsers();
            AddAllCurrentRoomVariables();
            if (evtType == SFSEvent.USER_ENTER_ROOM)
            {
                VDebug.LogError("Don't add user twice, stopping user enter room event");
                return false;
            }
        }
        return true;
    }

    public void RecordEvent(BaseEvent evt)
    {
        if (!Init(evt.Type))
            return;
#if !UNITY_WEBPLAYER
        using (BinaryWriter writer = new BinaryWriter(File.Open(recordFilename, FileMode.Append, FileAccess.Write)))
        {
            RecordEventType(writer, evt);
        }
#endif
    }

    void RecordEventType(BinaryWriter writer, BaseEvent evt)
    {
        // can't use a switch statement cleanly since event types are not const (they are read-only)
        if (evt.Type == SFSEvent.USER_VARIABLES_UPDATE)
            RecordUserVariableUpdate(writer, evt);
        else if (evt.Type == SFSEvent.OBJECT_MESSAGE)
            RecordObjectMessage(writer, evt);
        else if (evt.Type == SFSEvent.ROOM_VARIABLES_UPDATE)
            RecordRoomVariablesUpdate(writer, evt);
        else if (evt.Type == SFSEvent.PUBLIC_MESSAGE || evt.Type == SFSEvent.PRIVATE_MESSAGE || evt.Type == SFSEvent.ADMIN_MESSAGE)
            RecordPublicPrivateAdminMessage(writer, evt);
        else if (evt.Type == SFSEvent.USER_ENTER_ROOM || evt.Type == SFSEvent.USER_EXIT_ROOM)
            RecordUserEnterExit(writer, evt);
        else
            Debug.LogError("Record Event Unsupported type: " + evt.Type);
    }

    void RecordTimeAndType(BinaryWriter writer, BaseEvent evt)
    {
        RecordTimeAndType(writer, evt.Type);
    }

    void RecordTimeAndType(BinaryWriter writer, string evtType)
    {
        writer.Write(DateTime.UtcNow.ToBinary());
        writer.Write(evtType);
    }

    void RecordUser(BinaryWriter writer, User user)
    {
        writer.Write(user.Id);
        writer.Write(user.PlayerId);
        writer.Write(user.Name);
    }

    bool IsValidUser(User user)
    {
        return user != null && (user != CommunicationManager.MySelf || (RecordingPlayer.Active && RecordingPlayer.Inst.RecordMyActions));
    }

    void RecordObjectMessage(BinaryWriter writer, BaseEvent evt)
    {
        User sender = (User)evt.Params["sender"];
        if (!IsValidUser(sender))
            return;

        SFSObject msgObj = (SFSObject)evt.Params["message"];
        ByteArray msg = new ByteArray(msgObj.ToBinary().Bytes);

        RecordObjectMessageImpl(writer, sender, msg);
    }

    public void RecordObjectMessage(User sender, SFSObject message)
    {
        if (!Init(SFSEvent.OBJECT_MESSAGE) || !IsValidUser(sender))
            return;

        ByteArray msg = new ByteArray(message.ToBinary().Bytes);
#if !UNITY_WEBPLAYER
        using (BinaryWriter writer = new BinaryWriter(File.Open(recordFilename, FileMode.Append, FileAccess.Write)))
        {
            RecordObjectMessageImpl(writer, sender, msg);
        }
#endif
    }

    void RecordObjectMessageImpl(BinaryWriter writer, User sender, ByteArray msg)
    {
        RecordTimeAndType(writer, SFSEvent.OBJECT_MESSAGE);
        RecordUser(writer, sender);
        writer.Write(msg.Length);
        writer.Write(msg.Bytes);
        writer.Write(0); // data.Length
    }

    void RecordPublicPrivateAdminMessage(BinaryWriter writer, BaseEvent evt)
    {
        User sender = (User)evt.Params["sender"];
        if (!IsValidUser(sender))
            return;

        RecordTimeAndType(writer, evt);
        RecordUser(writer, sender);

        writer.Write((string)evt.Params["message"]);
        writer.Write(0); // msg.Length for obj messages

        ByteArray data = null;
        if (evt.Params.ContainsKey("data"))
        {
            SFSObject dataObj = (SFSObject)evt.Params["data"];
            if (dataObj != null)
                data = new ByteArray(dataObj.ToBinary().Bytes);
        }
        writer.Write(data == null ? 0 : data.Length);
        if (data != null)
            writer.Write(data.Bytes);
    }

    void RecordUserVariable(BinaryWriter writer, UserVariable userVar)
    {
        writer.Write(userVar.Name);
        writer.Write((int)userVar.Type);
        switch (userVar.Type)
        {
            case VariableType.INT:
                writer.Write(userVar.GetIntValue());
                break;
            case VariableType.DOUBLE:
                writer.Write(userVar.GetDoubleValue());
                break;
            case VariableType.STRING:
                writer.Write(userVar.GetStringValue());
                break;
            case VariableType.OBJECT:
                ByteArray ba = userVar.GetSFSObjectValue().ToBinary();
                writer.Write(ba.Length);
                writer.Write(ba.Bytes);
                break;
        }
    }

    void RecordUserVariables(BinaryWriter writer, List<UserVariable> userVars)
    {
        writer.Write(userVars.Count);
        foreach (UserVariable userVar in userVars)
            RecordUserVariable(writer, userVar);
    }

    void RecordUserVariableUpdate(BinaryWriter writer, BaseEvent evt)
    {
        User user = (User)evt.Params["user"];
        if (!IsValidUser(user))
            return;

        RecordTimeAndType(writer, evt);
        RecordUser(writer, user);
        writer.Write(0); // msg.Length for obj messages
        writer.Write(0); // data.Length for obj messages

        ArrayList changedVars = (ArrayList)evt.Params["changedVars"];
        writer.Write(changedVars.Count);
        foreach (string changedVar in changedVars)
            RecordUserVariable(writer, user.GetVariable(changedVar));
    }

    void RecordRoomVariables(BinaryWriter writer, Room room)
    {
        if (room == null)
            return;

        RecordTimeAndType(writer, SFSEvent.ROOM_VARIABLES_UPDATE);
        writer.Write(0); // msg.Length for obj messages
        writer.Write(0); // data.Length for obj messages
        writer.Write(room.Name);

        List<RoomVariable> vars = room.GetVariables();
        writer.Write(vars.Count);
        foreach (RoomVariable v in vars)
            RecordUserVariable(writer, v);
    }

    void RecordRoomVariablesUpdate(BinaryWriter writer, BaseEvent evt)
    {
        Room room = (Room)evt.Params["room"];
        if (room == null)
            return;

        RecordTimeAndType(writer, evt);
        writer.Write(0); // msg.Length for obj messages
        writer.Write(0); // data.Length for obj messages
        writer.Write(room.Name);

        ArrayList changedVars = (ArrayList)evt.Params["changedVars"];
        writer.Write(changedVars.Count);
        foreach (string changedVar in changedVars)
            RecordUserVariable(writer, room.GetVariable(changedVar));
    }

    void RecordUserEnterExit(BinaryWriter writer, BaseEvent evt)
    {
        RecordUserEnterExit(writer, (User)evt.Params["user"], evt.Type, (Room)evt.Params["room"]);
    }

    void RecordUserEnterExit(BinaryWriter writer, User user, string evtType, Room room)
    {

        if (!IsValidUser(user))
            return;
        RecordTimeAndType(writer, evtType);
        RecordUser(writer, user);
        writer.Write(0); // msg.Length for obj messages
        writer.Write(0); // data.Length for obj messages

        writer.Write(((room != null) ? room.Name : ""));
        if (fileFormatVersion > 1 && evtType == SFSEvent.USER_ENTER_ROOM)
            RecordUserVariables(writer, user.GetVariables());
    }

}
