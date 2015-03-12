using UnityEngine;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using System;

public class DataMessageManager : MonoBehaviour {

    private bool ignoreMessages = false; // record to disk if desired, but don't act on the message.
    MessageRecorder msgRecorder = null; // new MessageRecorder("messages.data");
    public DateTime LastRecMsgTime{ get; private set; }


    private static DataMessageManager mInstance;
    public static DataMessageManager Instance
    {
        get
        {
            if (mInstance == null)
            {
                mInstance = (new GameObject("DataMessageManager")).AddComponent(typeof(DataMessageManager)) as DataMessageManager;
            }
            return mInstance;
        }
    }
    
    public static DataMessageManager Inst
    {
        get{return Instance;}
    }

    void Awake()
    {
        LastRecMsgTime = DateTime.Now;
    }

    void Start()
    {
        DontDestroyOnLoad(this); // keep persistent when loading new levels
    }

    public void StartRecording(string _recordFilename, bool _ignoreMessages)
    {
        ignoreMessages  = _ignoreMessages;
        msgRecorder = new MessageRecorder(_recordFilename);
    }

    public void StopRecording()
    {
        msgRecorder = null;
        if (GameGUI.Inst.guiLayer != null)
            GameGUI.Inst.guiLayer.SendGuiLayerRecordingStop();
    }

    public bool MessagesRecorded()
    {
        return msgRecorder != null && msgRecorder.RecordFileCreated;
    }

    public void RecordObjectMessage(User sender, SFSObject message)
    {
        msgRecorder.RecordObjectMessage(sender, message);
    }

	//----------------------------------------------------------
	// SmartFox callbacks
	//----------------------------------------------------------
    public void OnObjectMessage(BaseEvent evt)
    {
        if( HandleRecordToDisk(evt) )
            return;

        SFSObject dataObj = (SFSObject)evt.Params["message"];
        User sender = (User)evt.Params["sender"];
        HandleObjectMessage(sender.Id, dataObj);
    }

    public void HandleObjectMessage(int userID, SFSObject dataObj)
    {
        //we are ignoring all msgs that doesnt have a "type" defined
        if (!dataObj.ContainsKey("type"))
        {
            HandleOtherObjectMessages(userID, dataObj); // support messages that don't conform to the "type" system for now.
            return;
        }

        switch (dataObj.GetUtfString("type"))
        {
            case "anim":
                {
                    Player player = GameManager.Inst.playerManager.GetPlayer(userID);
                    if (player != null)
                    {
                        string animName = dataObj.GetUtfString("anim");
                        player.gameObject.GetComponent<AnimatorHelper>().StartAnim(animName, false);
                    }
                }
                break;
            case "panim":
                {
                    Player player = GameManager.Inst.playerManager.GetPlayer(userID);
                    if (player != null)
                    {
                        string animName = dataObj.GetUtfString("anim");
                        player.gameObject.GetComponent<AnimatorHelper>().StopAnim(animName, false);
                    }
                }
                break;
            case "cmd":
                switch (dataObj.GetUtfString("cmd"))
                {
                    case "rm":
                        GameGUI.Inst.WriteToConsoleLog("Removing player unit from " + userID);
                        //	                    GameManager.Inst.playerManager.RemoveRemotePlayer(sender);
                        break;

                    case "whisper":
                        GameGUI.Inst.WriteToConsoleLog("whispering pm from " + userID);
                        break;
                }
                break;
            case "voice":
                VoiceManager.Inst.HandleMessage(dataObj);
                break;
            case "screen":
                Debug.LogError("Got Screen message, contains rs?: " + (dataObj.ContainsKey("type") ? "yes" : "no"));
                int stageItemID = dataObj.GetInt("rs");
                BizSimScreen.RefreshScreen(stageItemID);
                break;
            case "ss":
                if (StrategyScreen.Inst != null)
                    StrategyScreen.Inst.HandleMessage(dataObj);
                break;
        }
    }

    private void HandleOtherObjectMessages(int userID, SFSObject msgObj)
    {
        if (msgObj.ContainsKey("mp") || msgObj.ContainsKey("mpx") || msgObj.ContainsKey("me"))
            RemoteMouseManager.Inst.OnObjectMessage(userID, msgObj);
        if (GameManager.Inst.LevelLoaded == GameManager.Level.BIZSIM && BizSimManager.Inst.productMgr != null)
            BizSimManager.Inst.productMgr.HandleMessageObject(msgObj);
        if (msgObj.ContainsKey("url"))
            Debug.LogError("url -- message no longer supported, use room variables to change shared web panels");
        if (msgObj.ContainsKey("js") || msgObj.ContainsKey("pl"))
            BizSimScreen.HandleMessage(msgObj);

        // Effects (confetti!)
        if (msgObj.ContainsKey("cnfon") || msgObj.ContainsKey("typn"))
        {
            PlayerManager.remoteEffectManager.OnObjectMessage(msgObj);
        }
    }

    private bool IsURL(string str)
    {
        string trimmedStr = str.Trim();
        return (trimmedStr.IndexOf(' ') == -1) && trimmedStr.Length > 5 && (trimmedStr.IndexOf("http", 0, 5) != -1); // no spaces and starts with http
    }

    private bool IsConsoleCommand(string str)
    {
        return !string.IsNullOrEmpty(str) && str[0] == '/';
    }

    public void OnAdminMessage(BaseEvent evt)
    {
        if (HandleRecordToDisk(evt))
            return;

        ISFSObject dataObj = (SFSObject)evt.Params["data"];
        if (dataObj != null && dataObj.ContainsKey("d"))
            VoiceManager.Inst.HandleMessage(dataObj);
        else
        {
            string msg = (string)evt.Params["message"];
            if (IsConsoleCommand(msg))
            {
                PlayerType permission = PlayerType.ADMIN;
                if (dataObj != null && dataObj.ContainsKey("ptype"))
                    permission = (PlayerType)dataObj.GetInt("ptype");
                ConsoleInterpreter.Inst.ProcCommand(msg, permission);
            }
            else if (IsURL(msg))
                GameGUI.Inst.guiLayer.HandleDisplayUrlRequest(msg.Trim());
            else
                AnnouncementManager.Inst.Announce("Announcement", WebStringHelpers.HtmlEncode(msg));
        }
    }

    public void OnPrivateMessage(BaseEvent evt)
    {
        if (HandleRecordToDisk(evt))
            return;

        ChatManager.Inst.OnPrivateMessage(evt);
    }

    public void OnPublicMessage(BaseEvent evt)
    {
        if (HandleRecordToDisk(evt))
            return;

        ChatManager.Inst.OnPublicMessage(evt);
    }

    public void OnUserVariableUpdate(BaseEvent evt)
    {
        if (HandleRecordToDisk(evt))
            return;
        if (GameManager.Inst.playerManager != null)
            GameManager.Inst.playerManager.OnUserVariableUpdate(evt);
    }

    public void OnRoomVariablesUpdate(BaseEvent evt)
    {
        if (HandleRecordToDisk(evt))
            return;
        switch (GameManager.Inst.LevelLoaded)
        {
            case GameManager.Level.BIZSIM:
                BizSimManager.Inst.OnRoomVariablesUpdate(evt);
                break;
            case GameManager.Level.ORIENT:
            case GameManager.Level.TEAMROOM:
                // These are handled by RoomVariableUrlController now.
                break;
            default:
                Debug.LogError("Room variable update not handled in level: " + (int)GameManager.Inst.LevelLoaded);
                break;
        }

        RoomVariableUrlController.HandleRoomVariableUpdate(evt);
        RoomVariableToEnable.HandleRoomVariableUpdate(evt);
    }

    public void OnUserEnterRoom(BaseEvent evt)
    {
        if (HandleRecordToDisk(evt))
            return;
        if (GameManager.Inst.playerManager != null)
            GameManager.Inst.playerManager.OnUserEnterRoom(evt);
    }

    public void OnUserExitRoom(BaseEvent evt)
    {
        if (HandleRecordToDisk(evt))
            return;
        if (GameManager.Inst.playerManager != null)
            GameManager.Inst.playerManager.OnUserExitRoom(evt);
    }

    private bool HandleRecordToDisk(BaseEvent evt)
    {
        if (msgRecorder != null)
        {
            msgRecorder.RecordEvent(evt);
            LastRecMsgTime = DateTime.Now;
        }
        return ignoreMessages;
    }



}
