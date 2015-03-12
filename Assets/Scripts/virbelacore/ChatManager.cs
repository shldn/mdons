using UnityEngine;
using System.Collections;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Requests;


public class ChatManager : MonoBehaviour {
	
	//----------------------------------------------------------
	// Chat variables
	//----------------------------------------------------------
	private ArrayList messages = new ArrayList();
    public System.Object messagesLocker = new System.Object();	// Locker to use for messages collection to ensure its cross-thread safety
	public string lastmsg;
    public string tutorialChatCheck = "";

    private static ChatManager mInstance;
    public static ChatManager Instance
    {
        get
        {
            if (mInstance == null)
            {
                mInstance = (new GameObject("ChatManager")).AddComponent(typeof(ChatManager)) as ChatManager;
            }
            return mInstance;
        }
    }
    
    public static ChatManager Inst
    {
        get{return Instance;}
    }
	

    void Start()
    {
    	DontDestroyOnLoad(this); // keep persistent when loading new levels
    }

    public void sendPublicMsg(string newMessage)
    {
        if (!GameManager.DoesLevelHaveSmartFoxRoom(GameManager.Inst.LevelLoaded) || !CommunicationManager.InASmartFoxRoom)
        {
            tutorialChatCheck = newMessage;
            GameGUI.Inst.WriteToConsoleLog("me: " + newMessage);
            return;
        }
        CommunicationManager.SendMsg(new PublicMessageRequest(newMessage, null, CommunicationManager.LastValidRoom()));
    }

    public void OnPublicMessage(BaseEvent evt)
    {
        string message = (string)evt.Params["message"];
        User sender = (User)evt.Params["sender"];
        Room room = (Room)evt.Params["room"];
        OnPublicMessage(message, sender, room);
    }
	
	public void OnPublicMessage(string message, User sender, Room room)
    {
	
		// We use lock here to ensure cross-thread safety on the messages collection 
		lock (messagesLocker) {
            //checking who the sender name is 
            if (sender.IsItMe) { lastmsg = "me: " + message; }
            else 
            { 
                lastmsg = sender.Name + ": " + message;
                SoundManager.Inst.PlayPM();
            }
            messages.Add(lastmsg);
            GameGUI.Inst.UpdateChatGUI(getChatBuffer());
            GameGUI.Inst.guiLayer.SendGuiLayerChat(message, sender.Name, sender.Id, room != null && CommunicationManager.IsPrivateRoom(room.Name));
		}
		VDebug.Log("User " + sender.Name + " said: " + message);
	}

    public void sendPrivateMsg(string newMessage, User user)
    {
        SFSObject name = new SFSObject();
        name.PutUtfString("name", user.Name);
        CommunicationManager.SendMsg( new PrivateMessageRequest(newMessage, user.Id, name) );
    }
	
	public bool sendPrivateMsg(string newMessage, string username)
    {
		Sfs2X.Entities.User user = CommunicationManager.Inst.FindUser(username);
		if (user != null)
		{
			sendPrivateMsg(newMessage, user);
			return true;
		}
		return false;
    }
	
    public void OnPrivateMessage(BaseEvent evt)
    {
        string message = (string)evt.Params["message"];
        User sender = (User)evt.Params["sender"];
        ISFSObject recipient = (SFSObject)evt.Params["data"];

        if (sender.IsItMe) { lastmsg = "PM to " + recipient.GetUtfString("name") + " : " + message; }
        else
        {
            lastmsg = "PM from " + sender.Name + ": " + message;
            SoundManager.Inst.PlayPM();
        }
        messages.Add(lastmsg);
        GameGUI.Inst.UpdateChatGUI(getChatBuffer());
        GameGUI.Inst.guiLayer.SendGuiLayerChat(message, sender.Name, sender.Id, false, true, recipient.GetUtfString("name"));

        VDebug.Log("PM: " + message + ", from: " + sender.Name);
    }

	public string getChatBuffer()
	{
		string ret = "";
		foreach (string msg in messages)
		{
		    ret += msg + "\n";
		}
		return ret;
	}
}