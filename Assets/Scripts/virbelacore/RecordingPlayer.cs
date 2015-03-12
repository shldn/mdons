using UnityEngine;
using System.Collections;
using System;
using Sfs2X.Requests;
using Sfs2X.Entities.Data;

public class RecordingPlayer : MonoBehaviour {

    private float minInactivityTimeForFile = 5 * 60;
    private bool mIgnoreMessages = true; // if true, don't act on incomming messages
    private bool mRecordMyActions = true; // record my outgoing messages in addition to everyone else's messages.
    private string mRecordingFilename = "";

    public bool RecordMyActions { get { return mRecordMyActions; } }
    public string RecordingFilename { get { return mRecordingFilename; } }


    private static RecordingPlayer mInstance;
    public static RecordingPlayer Inst
    {
        get
        {
            if (mInstance == null)
            {
                mInstance = (new GameObject("RecordingPlayer")).AddComponent(typeof(RecordingPlayer)) as RecordingPlayer;
            }
            return mInstance;
        }
    }

    public static void Destroy()
    {
        DataMessageManager.Inst.StopRecording();
        if( mInstance == null )
            return;
        mInstance.CancelInvoke();
        GameObject.Destroy(mInstance);
        mInstance = null;
    }

    public static bool Active { get { return mInstance != null; } }

    public void Init(string recordingFilename, bool ignoreMessages = true, bool recordMyActions = true)
    {
        mIgnoreMessages = ignoreMessages;
        mRecordMyActions = recordMyActions;
        if (recordingFilename == "")
            recordingFilename = MessageRecorder.GetDefaultFileName();
        mRecordingFilename = System.IO.Path.GetFullPath(recordingFilename);
        DataMessageManager.Inst.StartRecording(recordingFilename, mIgnoreMessages);
        if (GameManager.InBatchMode())
        {
            InvokeRepeating("InactivityCheck", 0, 0.1f * minInactivityTimeForFile);
            InvokeRepeating("KeepAlive", 20, 4 * 60);
        }
    }

    void Start()
    {
        DontDestroyOnLoad(this); // keep persistent when loading new levels
    }

    void KeepAlive()
    {
        ChatManager.Inst.sendPrivateMsg("m", CommunicationManager.MySelf);
    }
    void InactivityCheck()
    {
        if (DataMessageManager.Inst.MessagesRecorded() && DateTime.Now.Subtract(DataMessageManager.Inst.LastRecMsgTime).TotalSeconds > minInactivityTimeForFile)
            DataMessageManager.Inst.StartRecording("", mIgnoreMessages);
    }
}
