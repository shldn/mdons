using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sfs2X.Requests;
using Sfs2X.Entities.Data;
using Sfs2X.Core;

//----------------------------------------------------------
// VoiceManager
//
// Handles Voice Chat setup, sending & recieving of voice data packets
//----------------------------------------------------------
public class VoiceManager : MonoBehaviour {

    private float forceToPushToTalkFeedbackThreshold = 0.95f; // [0,1] The percentage of time that the player is talking when someone else is talking to force them to use push to talk
    private int minNumSamplesForForcePushToTalk = 100; // num samples to allow data collection before acting on the percentage gathered.

    public Dictionary<int, VoiceChatPlayer> voiceChatPlayers; // need one per player, so they can all talk at once
	private bool initialized = false;
    private int numMicrophoneDevices = 0;
    private string forceToUseDeviceName = null; //"Logitech USB Headset" // if this is set and a device includes this string in it's description, we will use it.
    private static BoolSamplePercentage IsTalkingWhilePlayingAudio = new BoolSamplePercentage();

    // Menu/options -------------------------------
    public bool micSelectMenuOpen = false;

    public float micGain = 1.0f;
    public float logarithmicMicGain = 1.0f;
    float micMenuHeight = 0f;

    Texture2D micIcon = null;
    Texture2D whiteTex = null;
    // --------------------------------------------

    private static VoiceManager mInstance;
    public static VoiceManager Instance
    {
        get
        {
            if (mInstance == null)
            {
                mInstance = (new GameObject("VoiceManager")).AddComponent(typeof(VoiceManager)) as VoiceManager;
            }
            return mInstance;
        }
    }
    
    public static VoiceManager Inst
    {
        get{return Instance;}
    }

    // user is toggled on to stream all audio from their mic, or toggled off to send no audio -- tied to VoiceToggle On button in GUI
    public bool ToggleToTalk {
        get { return VoiceChatRecorder.Instance.ToggleToTalk; }
        set 
		{ 
			VoiceChatRecorder.Instance.ToggleToTalk = value;
			VoiceChatRecorder.Instance.AutoDetectSpeech = value; 
		}
    }

    public bool PushToTalkButtonDown
    {
        get { return VoiceChatRecorder.Instance.PushToTalkButtonDown; }
        set { VoiceChatRecorder.Instance.PushToTalkButtonDown = value; }
    }

    private bool allowVoiceToggle = true;
    public bool AllowVoiceToggle { 
        get { return allowVoiceToggle; } 
        set { 
            allowVoiceToggle = value;
            if (!allowVoiceToggle)
                ToggleToTalk = false;
        } 
    }

    private bool handleFeedbackDetection = false;
    public bool HandleFeedbackDetection {
        get { return handleFeedbackDetection; }
        set {
            if (value == false && IsFeedbackOverThreshold())
            {
                AllowVoiceToggle = true;
            }
            handleFeedbackDetection = value;
        }
    }

    public string DeviceName
    {
        get { return VoiceChatRecorder.Instance.Device; }
        set{
            forceToUseDeviceName = value;
            if( initialized )
                InitializeRecorder();
        }
    }

	void Start () {
        if (!CommunicationManager.IsConnected)
            return;

        voiceChatPlayers = new Dictionary<int, VoiceChatPlayer>();
		VoiceChatRecorder.Instance.NewSample += new System.Action<VoiceChatPacket>(OnNewVoiceSample);
        SetAutoDetectMinFreqFromServer();

        micIcon = Resources.Load("Textures/microphone_icon", typeof(Texture2D)) as Texture2D;
        whiteTex = Resources.Load("Textures/white", typeof(Texture2D)) as Texture2D;

        if (PlayerPrefs.HasKey("localMicGain"))
        {
            micGain = PlayerPrefs.GetFloat("localMicGain");
            logarithmicMicGain = (Mathf.Pow(2, micGain) - 1);
        }

        if (PlayerPrefs.HasKey("preferredMic"))
        {
            DeviceName = PlayerPrefs.GetString("preferredMic");
        }

	}

    void SetAutoDetectMinFreqFromServer()
    {
        float newMinFreq = 3.0f;
        if (float.TryParse(CommunicationManager.voiceMinFreq, out newMinFreq))
            VoiceChatRecorder.Instance.AutoDetectMinFreq = newMinFreq;
        else
            Debug.LogError("Error parsing new voice Min Freq into a float");
    }

    bool IsFeedbackOverThreshold() {
        return IsTalkingWhilePlayingAudio.NumSamples >= minNumSamplesForForcePushToTalk && IsTalkingWhilePlayingAudio.Average > forceToPushToTalkFeedbackThreshold;
    }
	
	void Update () {

        if (!initialized && CommunicationManager.IsConnected)
		{
			if( VoiceChatRecorder.Instance.NetworkId == -1 )
				VoiceChatRecorder.Instance.NetworkId = CommunicationManager.MySelf.Id;
			
            // use the default device by keeping VoiceChatRecorder.Instance.Device == null
            numMicrophoneDevices = VoiceChatRecorder.Instance.AvailableDevices.Length;
            InitializeRecorder();
            ToggleToTalk = GameManager.Inst.voiceToggleInitSetting;
			initialized = true;
		}
        if (numMicrophoneDevices != VoiceChatRecorder.Instance.AvailableDevices.Length)
        {
            string msg = (numMicrophoneDevices < VoiceChatRecorder.Instance.AvailableDevices.Length) ? "new" : "lost";
            InfoMessageManager.Display("Detected a " + msg + " microphone device, reinitializing voice");
            VoiceChatRecorder.Instance.UpdateUserMicHWI("(unknown)");
            StartCoroutine(DelayedRecorderInit(2.0f));
            numMicrophoneDevices = VoiceChatRecorder.Instance.AvailableDevices.Length;
        }
        if (HandleFeedbackDetection && AllowVoiceToggle && IsFeedbackOverThreshold())
        {
            AnnouncementManager.Inst.Announce("Microphone Feedback", "We\'re sorry. Your computer has been detected to be causing audio feedback for other users.  Please use the Push To Talk option when you\'d like to speak.");
            AllowVoiceToggle = false;
        }
	}

    IEnumerator DelayedRecorderInit(float waitSeconds)
    {
        yield return new WaitForSeconds(waitSeconds);
        InitializeRecorder();
        InfoMessageManager.Display("Voice reinitialized: if voice does not work, please re-launch the application with the new mic plugged in");
    }
    private void InitializeRecorder()
    {
        VoiceChatRecorder.Instance.StopRecording();
        bool success = false;
        if (!string.IsNullOrEmpty(forceToUseDeviceName))
        {
            foreach (string device in VoiceChatRecorder.Instance.AvailableDevices)
            {
                if (device.Contains(forceToUseDeviceName))
                {
                    VoiceChatRecorder.Instance.Device = device;
                    success = VoiceChatRecorder.Instance.StartRecording();
                    Debug.LogError("Found " + forceToUseDeviceName + ": " + VoiceChatRecorder.Instance.Device);
                }
            }
        }
        if( !success )
            success = VoiceChatRecorder.Instance.StartRecording(); // default device used if not set above
        if (!success)
        {
            Debug.Log("Default device in use, looking for other available devices");
            foreach (string device in VoiceChatRecorder.Instance.AvailableDevices)
            {
                VoiceChatRecorder.Instance.Device = device;
                if (VoiceChatRecorder.Instance.StartRecording())
                {
                    Debug.Log("Using device: " + VoiceChatRecorder.Instance.Device);
                    break;
                }
                else
                {
                    Debug.Log("Device: " + VoiceChatRecorder.Instance.Device + " in use.");
                }
            }
        }
    }

    public static VoiceChatPacket GetVoicePacketFromMsg(ISFSObject msgObj)
    {
        VoiceChatPacket packet = new VoiceChatPacket();

        packet.Compression = (VoiceChatCompression)msgObj.GetByte("c");
        packet.Length = msgObj.GetInt("l");
        Sfs2X.Util.ByteArray t = msgObj.GetByteArray("d");
        packet.Data = t.Bytes;
        packet.NetworkId = msgObj.GetInt("i");
        return packet;
    }

	public void HandleMessage(ISFSObject msgObj)
	{
        if (!initialized)
            return;

        VoiceChatPacket packet = GetVoicePacketFromMsg(msgObj);

        if (!voiceChatPlayers.ContainsKey(packet.NetworkId))
		{
            voiceChatPlayers.Add(packet.NetworkId, ((GameObject)Instantiate(Resources.Load("VoiceChat_Player"))).GetComponent<VoiceChatPlayer>());
			voiceChatPlayers[packet.NetworkId].userId = packet.NetworkId;
		}
        voiceChatPlayers[packet.NetworkId].OnNewSample(packet); // plays audio
        if (ToggleToTalk && GameManager.Inst.LocalPlayer)
            IsTalkingWhilePlayingAudio.AddSample(GameManager.Inst.LocalPlayer.IsTalking);
	}
	
	public void OnNewVoiceSample(VoiceChatPacket packet)
	{
        if( !GameManager.DoesLevelHaveSmartFoxRoom(GameManager.Inst.LevelLoaded) || !CommunicationManager.InASmartFoxRoom )
            return;

        Sfs2X.Util.ByteArray data = new Sfs2X.Util.ByteArray();
        data.WriteBytes(packet.Data, 0, packet.Length);
        SendPacket(packet, data);
	}
	
    void SendPackets(Queue<VoiceChatPacket> packets)
    {
		if( packets.Count == 0 )
			return;
		
        VoiceChatPacket v = new VoiceChatPacket();
        Sfs2X.Util.ByteArray data = new Sfs2X.Util.ByteArray();
        v.Length = 0;
        while (packets.Count > 0)
        {
            VoiceChatPacket packet = packets.Dequeue();
            v.Compression = packet.Compression;
            data.WriteBytes(packet.Data, 0, packet.Length);
            v.Length += packet.Length;
            v.NetworkId = packet.NetworkId;
        }
        SendPacket(v, data);

    }

    void SendPacket(VoiceChatPacket packet, Sfs2X.Util.ByteArray data)
    {
//        SFVoicePacket sfPacket = new SFVoicePacket(packet);
        // CommunicationManager.SendObjectMsg(sfPacket.GetSerialized());

        SFSObject voiceData = new SFSObject();
        voiceData.PutUtfString("type", "voice");
        voiceData.PutByte("c", (byte)packet.Compression); // Compression
        voiceData.PutInt("l", packet.Length); // Length
        voiceData.PutByteArray("d", new Sfs2X.Util.ByteArray(packet.Data)); // Data
        voiceData.PutInt("i", packet.NetworkId); // NetworkId

        bool sendToAllInZone = false;
        if(sendToAllInZone)
            CommunicationManager.SendMsg(new AdminMessageRequest("v", (new MessageRecipientMode((int)MessageRecipientType.TO_ZONE, null)), voiceData));
        else
            CommunicationManager.SendObjectMsg(voiceData);

        // Optimization notes:
        // use class serialization, current method is probably slow (http://docs2x.smartfoxserver.com/AdvancedTopics/class-serialization)
        // can also use the pool setup for the demo, so you don't have to instantiate a new buffer for every message
        // don't need to send compression byte every time, only one networkID needed if a queue is grouped together.
    }


    void OnGUI()
    {
        if (micSelectMenuOpen){
            PlayerController.ClickToMoveInterrupt();

            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            float micListWidth = 350f;
            float micListHeight = 30f;
            float micListGutter = 5f;

            float micListTop = (Screen.height * 0.5f) - 200f;
            Rect micListRect = new Rect((Screen.width * 0.5f) - (micListWidth * 0.5f), micListTop, micListWidth, micListHeight);

            float bgMargin = 50f;
            Rect voiceMenuRect = new Rect(micListRect.x - bgMargin, micListRect.y - (bgMargin * 0.25f), micListWidth + (bgMargin * 2f), micMenuHeight + bgMargin);
            GameGUI.Inst.NativeGUIBox(voiceMenuRect);
            if(Input.GetMouseButton(0) && !GameGUI.Inst.RectActuallyContainsMouse(voiceMenuRect))
                micSelectMenuOpen = false;


            GUI.Label(micListRect, "Choose a Microphone:");
            micListRect.y += micListHeight + micListGutter;
            if(VoiceChatRecorder.Instance.AvailableDevices.Length == 0){
                GUI.Label(micListRect, "No recording devices detected on this computer.");
                micListRect.y += micListHeight + micListGutter;
            }
            for (int i = 0; i < VoiceChatRecorder.Instance.AvailableDevices.Length; i++)
            {
                string currentDevice = VoiceChatRecorder.Instance.AvailableDevices[i];
                if(((DeviceName != null) && currentDevice.Equals(DeviceName)) || ((DeviceName == null) && (currentDevice.Equals(VoiceChatRecorder.Instance.AvailableDevices[0])))){

                    GUI.color = Color.green;
                    GUI.DrawTexture(new Rect(micListRect.x - (micListRect.height + micListGutter), micListRect.y, micListRect.height, micListRect.height), micIcon);

                    float micInlay = 7f;
                    float micVolSize = Mathf.Clamp01(VoiceChatRecorder.Instance.lastVolume * 0.01f);

                    if (micVolSize >= 0.8)
                        GUI.color = Color.red;
                    else if (micVolSize >= 0.6)
                        GUI.color = Color.yellow;

                    GUI.DrawTexture(new Rect(micListRect.x + micInlay, micListRect.y + micInlay, (micListRect.width - (micInlay * 2f)) * micVolSize, micListRect.height - (micInlay * 2f)), whiteTex);

                    // Reset gui
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    GUI.color = Color.white;
                }

                if (GUI.Button(micListRect, currentDevice))
                {
                    DeviceName = VoiceChatRecorder.Instance.AvailableDevices[i];
                    PlayerPrefs.SetString("preferredMic", VoiceChatRecorder.Instance.AvailableDevices[i]);
                }
                micListRect.y += micListHeight + micListGutter;
            }
            
            // Client-side unity-native mic volume adjust.
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            GUI.Label(micListRect, "Microphone Volume: " + micGain.ToString("F2"));
            micListRect.y += micListHeight;
            micGain = GUI.HorizontalSlider(micListRect, micGain, 0f, 2f);
            PlayerPrefs.SetFloat("localMicGain", micGain);
            logarithmicMicGain = (Mathf.Pow(2, micGain) - 1);

            micListRect.y += (micListHeight * 0.6f) + micListGutter;


            if ((Application.platform == RuntimePlatform.WindowsPlayer) || (Application.platform == RuntimePlatform.WindowsEditor))
            {
                float windowsSettingsWidth = 200f;
                Rect windowsSettingsRect = new Rect((Screen.width * 0.5f) - (windowsSettingsWidth * 0.5f), micListRect.y, windowsSettingsWidth, micListHeight * 0.8f);
                if (GUI.Button(windowsSettingsRect, "Windows Sound Settings"))
                    Native.OpenMicSettings();

                // tooltip if hovered over sound settings button
                if ((Input.mousePosition.x > windowsSettingsRect.x) && (Input.mousePosition.x < (windowsSettingsRect.x + windowsSettingsRect.width)) &&
                    ((Screen.height - Input.mousePosition.y) > windowsSettingsRect.y) && ((Screen.height - Input.mousePosition.y) < (windowsSettingsRect.y + windowsSettingsRect.height)))
                    GameGUI.Inst.SetFixedTooltip("Opens Windows Control Panel for Mic and Speakers");
                else
                    GameGUI.Inst.ClearFixedTooltip();
            }

            micListRect.y += (micListHeight * 0.8f) + micListGutter;
            float doneWidth = 100f;
            Rect micListCloseRect = new Rect((Screen.width * 0.5f) - (doneWidth * 0.5f), micListRect.y, doneWidth, micListHeight);
            if (GUI.Button(micListCloseRect, "Done"))
                micSelectMenuOpen = false;
            micListRect.y += micListGutter;

            // Get actual menu height (for drawing background rect).
            micMenuHeight = micListRect.y - micListTop;
        }
    }
	
}
