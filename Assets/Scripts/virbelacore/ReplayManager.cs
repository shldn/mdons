using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Sfs2X.Core;
using Sfs2X.Util;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Boomlagoon.JSON;

public struct MsgData
{
    public DateTime time;
    public int id;
    public int playerID;
    public string name;
    public string room;
    public string msgType;
    public string msgStr;
    public byte[] msg;
    public byte[] data;
    public List<UserVariable> changedVars;

    public bool IsPositionChange()
    {
        if (msgType != SFSEvent.USER_VARIABLES_UPDATE)
            return false;
        for (int i = 0; i < changedVars.Count; ++i)
        {
            string name = changedVars[i].Name;
            if ( name == "x" || name == "y" || name == "z" )
                return true;
        }
        return false;
    }
    public Vector3 GetPosition()
    {
        float newRotAngle = 0;
        Vector3 newPos = Vector3.zero;
        int transformMsgCount = 0;
        for (int i = 0; changedVars != null && i < changedVars.Count; ++i)
        {
            UserVariable userVar = changedVars[i];
            if (userVar.Name == "x" || userVar.Name == "y" || userVar.Name == "z" || userVar.Name == "rot")
            {
                ReplayManager.UpdateTransformVars(userVar, ref newRotAngle, ref newPos);
                transformMsgCount++;
                if (transformMsgCount == 4)
                    return newPos;
            }
        }
        Debug.LogError("Didn\'t find position");
        return newPos;
    }
}

public class ReplayManager : MonoBehaviour {

    private List<MsgData> messagesToPlay = new List<MsgData>();
    public Dictionary<int, Player> replayPlayers = new Dictionary<int, Player>();
    public List<string> allowedPlayerNames = new List<string>(); // only replay players in this list.
    private DateTime startFileTime;
    private DateTime playbackTime;
    private float playbackSpeed = 1.0f;
    private int nextMsgIdx = 0;
    private int msgOffset = 0;
    private int numMsgsToLoad = -1;
    private bool saveCSV = false;
    private bool replayClicks = true;
    private bool startTimeOnFirstVoiceMsg = false;
    public bool hideNativeGui = false; // used as a per replay file switch, reset to false with a new file load.
    private string replayResource = ""; // filename or url
    private string commandOnComplete = "";
    private static ReplayManager mInstance;
    public static ReplayManager Inst
    {
        get
        {
            if (mInstance == null)
                mInstance = (new GameObject("ReplayManager")).AddComponent(typeof(ReplayManager)) as ReplayManager;
            return mInstance;
        }
    }
    public static bool Initialized { get { return mInstance != null; } }

    public bool Paused { get; set; }
    public bool Done { get { return nextMsgIdx >= NumMessagesToPlay; } }
    public bool Playing { get { return !Paused && !Done; } }
    public int NextMsgIdx { get { return nextMsgIdx; } }
    public DateTime NextMsgTime { get { return messagesToPlay[Math.Min(nextMsgIdx, messagesToPlay.Count - 1)].time; } }
    public int MessageOffset { get { return msgOffset; } } 
    public int NumMessagesToPlay { get { return messagesToPlay.Count; } }
    public DateTime PlaybackTime { get { return playbackTime; } set { SetPlaybackTime(value); } }
    public DateTime StartTime { get { return startFileTime; } }
    public DateTime EndTime { get { return messagesToPlay.Count == 0 ? StartTime : messagesToPlay[messagesToPlay.Count - 1].time; } }
    public TimeSpan PlaybackTimeSpan { get { return EndTime.Subtract(StartTime); } }
    public float Speed { get { return playbackSpeed; } set { playbackSpeed = value; } }
    public int GetNextMessageFromTime(DateTime time)
    {
        int idx = nextMsgIdx; // guess a starting point to start looking (could do binary search instead)
        if (idx < NumMessagesToPlay && time > messagesToPlay[idx].time)
        {
            while (idx < NumMessagesToPlay && time > messagesToPlay[idx].time)
                ++idx;
            return idx;
        }
        else
        {
            while (idx > 0 && time < messagesToPlay[idx - 1].time)
                --idx;
            return idx;
        }
    }

    private void SetPlaybackTime(DateTime newTime)
    {
        if (newTime < StartTime || newTime > EndTime)
        {
            Debug.LogError("New Playback time out of range");
            return;
        }

        SetCurrentMessage(GetNextMessageFromTime(newTime));
        playbackTime = newTime;
    }

    public float GetPlaybackPercent()
    {
        return ((float)(ReplayManager.Inst.PlaybackTime.Ticks - ReplayManager.Inst.StartTime.Ticks)) / (float)ReplayManager.Inst.PlaybackTimeSpan.Ticks;
    }

    public void SetPlaybackPercent(float percent)
    {
        percent = Mathf.Clamp01(percent);
        SetPlaybackTime(ReplayManager.Inst.StartTime.AddTicks((long)(percent * PlaybackTimeSpan.Ticks)));
    }

    private void InitVars(string filename, int msgOffset_, int numMsgsToLoad_, bool saveCSV_, string commandOnComplete_, bool replayClicks_)
    {
        replayResource = filename;
        msgOffset = msgOffset_;
        commandOnComplete = commandOnComplete_;
        numMsgsToLoad = numMsgsToLoad_;
        saveCSV = saveCSV_;
        replayClicks = replayClicks_;
    }

    public void ReadUrl(string url, int msgOffset_ = 0, int numMsgsToLoad_ = -1, bool saveCSV_ = false, string commandOnComplete_ = "", bool replayClicks_ = true)
    {
        InitVars(url, msgOffset_, numMsgsToLoad_, saveCSV_, commandOnComplete_, replayClicks_);
        gameObject.AddComponent<DownloadHelper>().StartDownload(url, HandleReplayDownload, true);
    }

    private void HandleReplayDownload(WWW downloadObj)
    {
        List<MsgData> msgData = ReplayFileReader.Inst.ReadStream(new MemoryStream(downloadObj.bytes), "replaydata", msgOffset, numMsgsToLoad, saveCSV);
        if (!saveCSV)
            PlayBack(msgData, 0, startTimeOnFirstVoiceMsg);
        else
            Destroy();
    }

    public void ReadFile(string filename, int msgOffset_ = 0, int numMsgsToLoad_ = -1, bool saveCSV_ = false, string commandOnComplete_ = "", bool replayClicks_ = true)
    {
        if (filename.StartsWith("http") || filename.StartsWith("www."))
        {
            ReadUrl(filename, msgOffset_, numMsgsToLoad_, saveCSV_, commandOnComplete_, replayClicks_);
            return;
        }


        InitVars(filename, msgOffset_, numMsgsToLoad_, saveCSV_, commandOnComplete_, replayClicks_);

        List<MsgData> msgData = ReplayFileReader.Inst.ReadFile(filename, msgOffset, numMsgsToLoad, saveCSV);
        if (!saveCSV)
            PlayBack(msgData, 0, startTimeOnFirstVoiceMsg);
        else
            Destroy();
    }

    IEnumerator HandleVoiceMessage(float waitSeconds, SFSObject dataObj)
    {
        yield return new WaitForSeconds(Math.Max(0.001f, waitSeconds));
        VoiceManager.Inst.HandleMessage(dataObj);
    }

    public static bool IsMsgString(string evtType)
    {
        return evtType == SFSEvent.PUBLIC_MESSAGE || evtType == SFSEvent.PRIVATE_MESSAGE || evtType == SFSEvent.ADMIN_MESSAGE;
    }

    private int FindNextVoiceMsg(List<MsgData> msgData, int startOffset)
    {
        for (int i = startOffset; i < msgData.Count; ++i)
        {
            MsgData msg = msgData[i];
            if (msg.msgType == SFSEvent.OBJECT_MESSAGE && msg.msg != null)
            {
                SFSObject dataObj = SFSObject.NewFromBinaryData(new ByteArray(msg.msg));
                if (dataObj.ContainsKey("type") && dataObj.GetUtfString("type") == "voice")
                    return i;
            }
        }
        return msgData.Count - 1;
    }

    private void PlayBack(List<MsgData> msgData, int startPlaybackMsg, bool startTimeOnFirstVoiceMsg_)
    {
        startFileTime = (MessageOffset == 0 || msgData.Count == 0) ? ReplayFileReader.Inst.StartFileTime : msgData[0].time;

        if (startTimeOnFirstVoiceMsg_)
            startPlaybackMsg = FindNextVoiceMsg(msgData, startPlaybackMsg);

        messagesToPlay = msgData;
        SetCurrentMessage(startPlaybackMsg);
        Paused = false;
        hideNativeGui = false;

        if (msgData.Count > 0)
            Debug.LogError("Loaded " + msgData.Count + " messages (" + msgData[0].time.ToString() + " - " + msgData[msgData.Count - 1].time.ToString() + ")");
        if (startPlaybackMsg > 0 && startPlaybackMsg < msgData.Count)
            Debug.LogError("\tStarting at msg " + startPlaybackMsg + " " + msgData[startPlaybackMsg].time);

        // Send gui layer replay info
        GameGUI.Inst.guiLayer.SendGuiLayerReplayStartInfo(StartTime, EndTime, replayResource, ReplayFileReader.Inst.Level);
    }

    private void StopAllPlayerAnimations()
    {
        foreach (KeyValuePair<int, Player> playerPair in replayPlayers)
            playerPair.Value.UpdateSpeed(0);
    }

    private void ResetPlayerGazes()
    {
        foreach (KeyValuePair<int, Player> playerPair in replayPlayers)
        {
            playerPair.Value.UpdateGaze("gt", 0.0f);
            playerPair.Value.UpdateGaze("gp", 0.0f);
        }
    }

    private void PlayLastMoveMessages(HashSet<int> playersToMove, int startMsgIdx, int endMsgIdx)
    {
        if (startMsgIdx > endMsgIdx)
        {
            Debug.LogError("PlayLastMoveMessages: startIdx shouldn\'t be greater than endIdx");
            return;
        }

        HashSet<int> handledPlayers = new HashSet<int>();
        for (int i = endMsgIdx; i >= startMsgIdx; --i)
        {
            MsgData msg = messagesToPlay[i];
            if (!handledPlayers.Contains(msg.id) && msg.IsPositionChange())
            {
                PlayBackMsg(msg);
                handledPlayers.Add(msg.id);
            }
            if (playersToMove.Count == handledPlayers.Count)
                return;
        }
    }

    private void PlayLastRoomVariableChanges(int startMsgIdx, int endMsgIdx)
    {
        if (startMsgIdx > endMsgIdx)
        {
            Debug.LogError("PlayLastRoomVariableChanges: startIdx shouldn\'t be greater than endIdx");
            return;
        }

        HashSet<string> handledVars = new HashSet<string>();
        for (int i = endMsgIdx; i >= startMsgIdx; --i)
        {
            MsgData msg = messagesToPlay[i];
            if (msg.msgType == SFSEvent.ROOM_VARIABLES_UPDATE)
            {
                for (int varcount = 0; varcount < msg.changedVars.Count; ++varcount)
                {
                    if (!handledVars.Contains(msg.changedVars[varcount].Name))
                    {
                        HandleRoomVariableUpdate(msg.changedVars[varcount]);
                        handledVars.Add(msg.changedVars[varcount].Name);
                    }
                }
            }
        }
    }

    private void PlayUserEnterExitMessages(int startMsgIdx, int endMsgIdx)
    {
        if (startMsgIdx > endMsgIdx)
        {
            Debug.LogError("PlayUserEnterExitMessages: startIdx shouldn\'t be greater than endIdx");
            return;
        }

        for (int i = startMsgIdx; i < endMsgIdx; ++i)
        {
            MsgData msg = messagesToPlay[i];
            if (msg.msgType == SFSEvent.USER_ENTER_ROOM || msg.msgType == SFSEvent.USER_EXIT_ROOM)
                PlayBackMsg(msg);
            else if (msg.msgType == SFSEvent.USER_VARIABLES_UPDATE)
            {
                bool msgPlayed = false;
                for (int j = 0; !msgPlayed && j < msg.changedVars.Count; ++j)
                {
                    if (msg.changedVars[j].Name == "op" || msg.changedVars[j].Name == "displayName")
                    {
                        PlayBackMsg(msg);
                        msgPlayed = true;
                    }
                }
            }
        }
    }

    private HashSet<int> GetVisiblePlayerIDs()
    {
        HashSet<int> visPlayerIDs = new HashSet<int>();
        foreach (KeyValuePair<int, Player> playerPair in replayPlayers)
            if( playerPair.Value.Visible )
                visPlayerIDs.Add(playerPair.Value.Id);
        return visPlayerIDs;
    }

    // Helper function to handle a break in the sequential message processing, doesn't assign the new message index.
    private void HandleMessageSkip(int newDesiredMsgIdx)
    {
        // Skipping messages can leave players stuck running in place, stop animations.
        StopAllPlayerAnimations();

        ResetPlayerGazes();

        // Mouse spheres could be stuck in wrong position, set them invisible and let new movements update them.
        RemoteMouseManager.Inst.SetAllVisibility(false); 

        // Handle messages in between before jumping to new desired message.
        int rangeToPlayStartIdx = (newDesiredMsgIdx > nextMsgIdx) ? nextMsgIdx : 0;
        if (rangeToPlayStartIdx == 0)
            SetAllPlayerVisibility(false);

        PlayUserEnterExitMessages(rangeToPlayStartIdx, newDesiredMsgIdx);
        PlayLastRoomVariableChanges(rangeToPlayStartIdx, newDesiredMsgIdx);
        PlayLastMoveMessages(GetVisiblePlayerIDs(), rangeToPlayStartIdx, newDesiredMsgIdx);
    }

    public bool SetCurrentMessage(int newStartIdx)
    {
        if (newStartIdx >= messagesToPlay.Count)
        {
            Debug.LogError("Invalid start idx: " + newStartIdx + " / " + messagesToPlay.Count);
            return false;
        }

        HandleMessageSkip(newStartIdx);
        nextMsgIdx = newStartIdx;
        playbackTime = (newStartIdx == 0) ? startFileTime : messagesToPlay[nextMsgIdx - 1].time;
        return true;
    }

    void Update()
    {
        if (NumMessagesToPlay > 0 && Input.GetKeyUp(KeyCode.Space))
            Paused = !Paused;
        if (Paused || Done)
            return;

        playbackTime = playbackTime.AddSeconds(playbackSpeed * Time.deltaTime); // this is not exact, based on the last frame time, not the actual time that has passed... revisit

        while (nextMsgIdx < NumMessagesToPlay && playbackTime > messagesToPlay[nextMsgIdx].time)
        {
            PlayBackMsg(messagesToPlay[nextMsgIdx]);
            ++nextMsgIdx;
            if (Done)
                OnReplayComplete();
        }
    }

    void OnReplayComplete()
    {
        ConsoleInterpreter.Inst.ProcCommand(commandOnComplete);
    }

    bool IsValidPlayer(string name)
    {
        return allowedPlayerNames.Count == 0 || allowedPlayerNames.IndexOf(name) != -1;
    }
    void PlayBackMsg(MsgData msg)
    {
        //Debug.LogError("time: " + DateTime.FromBinary(msg.time).ToString() + " id:" + msg.id + " name: " + msg.name + " msgType: " + msg.msgType);
        bool validPlayer = IsValidPlayer(msg.name);

        if (msg.msgType == SFSEvent.OBJECT_MESSAGE && msg.msg != null && validPlayer)
        {
            GetPlayer(ref msg); // hack to get the mouse colors to show up - problem when jumping in the timeline over the enter message.
            DataMessageManager.Inst.HandleObjectMessage(msg.id, SFSObject.NewFromBinaryData(new ByteArray(msg.msg)));
        }
        else if (msg.msgType == SFSEvent.USER_VARIABLES_UPDATE && validPlayer)
            HandleUserVariableUpdate(ref msg);
        else if (msg.msgType == SFSEvent.ROOM_VARIABLES_UPDATE)
            HandleRoomVariableUpdate(ref msg);
        else if (validPlayer && (msg.msgType == SFSEvent.PUBLIC_MESSAGE || msg.msgType == SFSEvent.PRIVATE_MESSAGE || msg.msgType == SFSEvent.ADMIN_MESSAGE))
            HandleChatMessages(ref msg);
        else if (validPlayer && (msg.msgType == SFSEvent.USER_ENTER_ROOM || msg.msgType == SFSEvent.USER_EXIT_ROOM))
            HandleUserEnterExit(ref msg);
        //else
        //    Debug.LogError("PlayBackMsg Unsupported type: " + msg.msgType);
    }

    public static void UpdateTransformVars(UserVariable userVar, ref float rotAngle, ref Vector3 pos)
    {
        if( userVar.Name == "rot" )
            rotAngle = (float)userVar.GetDoubleValue();
        else if( userVar.Name == "x" )
            pos.x = (float)userVar.GetDoubleValue();
        else if( userVar.Name == "y" )
            pos.y = (float)userVar.GetDoubleValue();
        else if( userVar.Name == "z" )
            pos.z = (float)userVar.GetDoubleValue();
    }

    void HandleRoomVariableUpdate(UserVariable userVar)
    {
        switch (GameManager.Inst.LevelLoaded)
        {
            case GameManager.Level.ORIENT:
            case GameManager.Level.TEAMROOM:
                RoomVariableUrlController.HandleRoomVariableUpdate(userVar);
                break;
            default:
                Debug.LogError("Room variable update not handled for level: " + (int)GameManager.Inst.LevelLoaded);
                break;
        }
        RoomVariableToEnable.HandleRoomVariableUpdate(userVar);
    }

    void HandleRoomVariableUpdate(ref MsgData msg)
    {
        for (int i = 0; i < msg.changedVars.Count; ++i)
            HandleRoomVariableUpdate(msg.changedVars[i]);
    }

    void HandleUserVariableUpdate(ref MsgData msg)
    {
        Player player = GetPlayer(ref msg);

        Vector3 newPos = Vector3.zero;
        float newRotAngle = 0;
        int transformMsgCount = 0;
        for( int i=0; i < msg.changedVars.Count; ++i )
        {
            UserVariable userVar = msg.changedVars[i];
            if (userVar.Name == "playerModel")
                Debug.LogError("Replay: Changed playerModel");
            else if( userVar.Name == "x" || userVar.Name == "y" || userVar.Name == "z" || userVar.Name == "rot")
            {
                UpdateTransformVars(userVar, ref newRotAngle, ref newPos);
                transformMsgCount++;
                if( transformMsgCount == 4 )
                {
                    player.UpdateTransform(newPos, newRotAngle);
                    player.playerController.shuffleTime = 0.25f;
                }
            }
            else
                player.UpdateUserVar(userVar, true);
        }
    }

    void HandleChatMessages(ref MsgData msg)
    {
        if (msg.msgType == SFSEvent.PUBLIC_MESSAGE)
        {
            Player p = GetPlayer(ref msg);
            ChatManager.Inst.OnPublicMessage(msg.msgStr, p.User, null);
        }
    }

    void HandleUserEnterExit(ref MsgData msg) // const ref
    {
        bool entered = (msg.msgType == SFSEvent.USER_ENTER_ROOM);
        Debug.LogError("\tuser " + msg.id + " - " + msg.name + ((entered) ? " entered" : " left"));
        if( entered )
        {
            Player replayPlayer = null;
            if (!replayPlayers.TryGetValue(msg.id, out replayPlayer))
            {
                // Create replay player
                User user = new ReplayUser(msg);
                PlayerInit pInit = PlayerManager.InitPlayerVariablesFromUserVariables(msg.changedVars);
                GameObject go = GameManager.Inst.playerManager.CreateRemotePlayerGO(pInit.modelIndex);
                replayPlayer = new Player(user, go, pInit);
                replayPlayers[msg.id] = replayPlayer;
            }
            replayPlayer.Visible = true;
            HandleUserVariableUpdate(ref msg);
        }
        else
        {
            Player p = GetPlayer(ref msg);
            p.Visible = false;
            //replayPlayers.Remove(msg.id);
        }
        Debug.LogError("HandleUserEnterExit complete");
    }

    Player GetPlayer(ref MsgData msg)
    {
        Player replayPlayer = null;
        if( !replayPlayers.TryGetValue(msg.id, out replayPlayer) )
        {
            User user = new ReplayUser(msg);
            UserVariable playerTypeOverride = null;
            if (msg.changedVars != null)
            {
                for (int i = 0; i < msg.changedVars.Count && playerTypeOverride == null; ++i)
                    if( msg.changedVars[i].Name == "ptype" )
                        playerTypeOverride = msg.changedVars[i];
            }
            string jsonRequest = "{\"displayname\":\"" + msg.name + "\"}";
            UserProfile userProfile = new UserProfile();
            userProfile.InitFromColumnValue(jsonRequest);

            if (!userProfile.Initialized)
                Debug.LogError("Didn't get the user " + msg.name + " they may have changed their display name");
            else
            {
                int modelIdx = PlayerManager.GetModelIdx(userProfile.Model);
                GameObject go = GameManager.Inst.playerManager.CreateRemotePlayerGO(modelIdx);
                replayPlayer = new Player(user, go, userProfile);
                if( playerTypeOverride != null )
                    replayPlayer.UpdateType(playerTypeOverride);
                replayPlayers[msg.id] = replayPlayer;
            }
            if( replayClicks )
                SetPlayerMouseToReplayMode(replayPlayer);
        }
        if( replayPlayer == null )
        {
            // save parse requests and use our best guess for now.
            Debug.LogError("Creating user: " + msg.name + " as a stealth user");
            int modelIdx = 0;
            GameObject go = GameManager.Inst.playerManager.CreateRemotePlayerGO(modelIdx);
            User user = new ReplayUser(msg);
            replayPlayer = new Player(user, go, modelIdx, PlayerType.STEALTH, "", msg.name, 0);
            replayPlayers[msg.id] = replayPlayer;

            if (replayClicks)
                SetPlayerMouseToReplayMode(replayPlayer);
        }
        if (!replayPlayer.Visible)
            replayPlayer.Visible = true;
        return replayPlayer;
    }

    private void SetPlayerMouseToReplayMode(Player replayPlayer)
    {
        if (replayPlayer != null)
        {
            PlayerMouseVisual mouseVisual = RemoteMouseManager.Inst.GetVisual(replayPlayer.Id);
            if (mouseVisual != null)
                mouseVisual.replayMode = true;
        }
    }

    private void SetAllPlayerVisibility(bool visible)
    {
        foreach (KeyValuePair<int, Player> playerPair in replayPlayers)
            playerPair.Value.Visible = visible;
    }

    public void Destroy()
    {
        ClearPlayerList();
        Destroy(mInstance);
        mInstance = null;
    }

    void ClearPlayerList()
    {
        foreach (KeyValuePair<int, Player> p in replayPlayers)
            Destroy(p.Value.gameObject);
        replayPlayers.Clear();
    }
}
