using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class BotManager
{
	public static bool botControlled = false;
	private bool botChat = false;
	private bool botStill = false;
	private int botMoveCount = 0;
	private bool botRoomSelect = false;
	private bool botLevelSelect = false;
	private bool botDelayMove = false;
	private int botDelay = 0;
    private float botVoiceDelay = -1.0f; // number of seconds between voice chats
    private DateTime nextVoiceChatTime = DateTime.Now + TimeSpan.FromSeconds(10);
    private List<VoiceChatPacket> voiceData = new List<VoiceChatPacket>();
	
	private static BotManager mInstance;
    public static BotManager Instance
    {
        get
        {
            if (mInstance == null)
            {
				mInstance = new BotManager();
            }
            return mInstance;
        }
    }
    
    public static BotManager Inst
    {
        get{return Instance;}
    }
	
	public void UpdateBot(PlayerInputManager playerInputMgr)
    {
        /*
		if (GameManager.Inst.playerManager == null)
			return;
        int randNum = UnityEngine.Random.Range(0, 10);
		if (!botStill && !botDelayMove)
		{
	        if( randNum < 2 )
				playerInputMgr.InjectTurnMovement(randNum == 1);
	        else
	            playerInputMgr.InjectForwardMovement(randNum > 5);
	        if (UnityEngine.Random.Range(0, 100) >= 99)
	            playerInputMgr.InjectJump();
		}
		if (botChat)
				if (UnityEngine.Random.Range(0,500) >= 499)
					ChatManager.Inst.sendPublicMsg("Testing: " + UnityEngine.Random.Range(0,100));
		if (botStill && botMoveCount < 500)
		{
			if( randNum <= 2 )
	            playerInputMgr.InjectTurnMovement(randNum == 1);
	        else
	            playerInputMgr.InjectForwardMovement(randNum > 5);
	        if (UnityEngine.Random.Range(0, 100) >= 99)
	            playerInputMgr.InjectJump();
			botMoveCount++;
		}
		if (botDelayMove)
		{
			if ((Time.frameCount%botDelay) == 0)
			{
				if( randNum < 2 )
		            playerInputMgr.InjectTurnMovement(randNum == 1);
		        else
		            playerInputMgr.InjectForwardMovement(randNum > 5);
		        if (UnityEngine.Random.Range(0, 100) >= 99)
		            playerInputMgr.InjectJump();
			}
		}
        if (botVoiceDelay > -1.0f && nextVoiceChatTime.Subtract(DateTime.Now).TotalSeconds < 0)
        {
            if (CommunicationManager.IsConnected && voiceData.Count == 0)
                ReadVoiceData();
            foreach (VoiceChatPacket packet in voiceData)
                VoiceManager.Inst.OnNewVoiceSample(packet);
            nextVoiceChatTime = DateTime.Now + TimeSpan.FromSeconds(botVoiceDelay);
        }
        */
    }
	
	public void SetBotOptions(string[] cmdLnArgs)
	{
		for (int i = 0; i < cmdLnArgs.Length; i++)
		{
			switch(cmdLnArgs[i])
			{
			case "-batchmode":
				botControlled = true;
				CommunicationManager.Inst.roomNumToLoad = 1;
				break;
			case "-chatbot":
				botChat = true;
				break;
			case "-stillbot":
				botStill = true;
				break;
			case "-roomselect":
				if (cmdLnArgs.Length > i+1)
				{
					string tempRoomNum = cmdLnArgs[i+1];
					CommunicationManager.Inst.roomNumToLoad = Convert.ToInt32(tempRoomNum);
				}
				botRoomSelect = true;
				break;
			case "-levelselect":
				botLevelSelect = true;
				if (cmdLnArgs.Length > i+1)
				{
					string levelName = cmdLnArgs[i+1].ToString().ToLower();
                    CommunicationManager.LevelToLoad = ConsoleInterpreter.GetLevel(cmdLnArgs[i + 1]);
				}
				else
                    CommunicationManager.LevelToLoad = GameManager.Level.CAMPUS;
				break;
			case "-delaymove":
				botDelayMove = true;
				if (cmdLnArgs.Length > i+1)
				{
					string tempDelay = cmdLnArgs[i+1];
					botDelay = Convert.ToInt32(tempDelay);
				}
				break;
			case "-framerate":
				if (cmdLnArgs.Length > i+1)
				{
					string tempFrameRate = cmdLnArgs[i+1];
					Application.targetFrameRate = Convert.ToInt32(tempFrameRate);
				}
				break;
            case "-voice":
                if (cmdLnArgs.Length > i + 1)
                {
                    string voiceDelayStr = cmdLnArgs[i + 1];
                    botVoiceDelay = (float)Convert.ToDouble(voiceDelayStr);
                }
                break;
			default:
				break;
			}
			if (botControlled && !botLevelSelect)
                CommunicationManager.LevelToLoad = GameManager.Level.CAMPUS;
		}
	}
    private void ReadVoiceData()
    {
        string path = "C:/temp/";
        string filename = "voice_packet";
        string ext = ".data";

        for( int i=0; i < 37; ++i )
        {
            try
            {
                using (FileStream fsSource = new FileStream(path + filename + i + ext, FileMode.Open, FileAccess.Read)) // using makes sure file handle is cleaned up.
                {

                    VoiceChatPacket packet = new VoiceChatPacket();
                    packet.Length = (int)fsSource.Length;
                    packet.Data = new byte[packet.Length];
                    packet.Compression = VoiceChatCompression.Speex;
                    packet.NetworkId = CommunicationManager.CurrentUserID;

                    int numBytesToRead = (int)packet.Length;
                    int numBytesRead = 0;
                    while (numBytesToRead > 0)
                    {
                        // Read may return anything from 0 to numBytesToRead. 
                        int n = fsSource.Read(packet.Data, numBytesRead, numBytesToRead);

                        // Break when the end of the file is reached. 
                        if (n == 0)
                            break;

                        numBytesRead += n;
                        numBytesToRead -= n;
                    }
                    voiceData.Add(packet);
                }
            }
            catch (FileNotFoundException ioEx)
            {
                Debug.LogError("ReadVoiceData: FileNotFound, make sure the sample voice packets are in " + path + "msg: " + ioEx.Message);
            }
            catch (Exception e)
            {
                Debug.LogError("ReadVoiceData Exception: " + e.ToString());
            }
        }
    }
}

