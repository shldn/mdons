using System;
using System.Linq;
using UnityEngine;

public class VoiceChatRecorder : MonoBehaviour
{
    #region Instance

    static VoiceChatRecorder instance;

    public static VoiceChatRecorder Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType(typeof(VoiceChatRecorder)) as VoiceChatRecorder;
            }

            return instance;
        }
    }

    #endregion
		
    [SerializeField]
    bool autoDetectSpeaking = false;

    [SerializeField]
    //int autoDetectIndex = 4;
    float autoDetectFreq= 3.0f;

    [SerializeField]
    float forceTransmitTime = 2f;

    int previousPosition = 0;
    int sampleIndex = 0;
    string device = null;
    AudioClip clip = null;
    bool transmitToggled = false;
	bool pushToTalkButtonDown = false;
    bool recording = false;
    static bool reportedInitErrors = false;
    float forceTransmit = 0f;
    int recordFrequency = 0;
    int recordSampleSize = 0;
    int targetFrequency = 0;
    int targetSampleSize = 0;
    float[] fftBuffer = null;
    float[] sampleBuffer = null;
    SampleAverage fftMaxAve = new SampleAverage();
    float minFreqDiffAboveAveToTransmit = 1.5f; // if the average FFT is 4, only sounds above (4 + this) will be transmitted.
    public float lastVolume = 0.0f;
    bool trackVolume = true;
    //VoiceChatCircularBuffer<float[]> previousSampleBuffer = new VoiceChatCircularBuffer<float[]>(5);
    

	public bool PushToTalkButtonDown
    {
        get { return pushToTalkButtonDown; }
        set { 
            pushToTalkButtonDown = value;
            if (pushToTalkButtonDown == false)
                StopTransmitting();
        }
    }

    public bool ToggleToTalk
    {
        get { return transmitToggled; }
        set { 
            transmitToggled = value;
            if (transmitToggled == false)
                StopTransmitting();
            GameManager.Inst.voiceToggleInitSetting = transmitToggled;
        }
    }

    public bool AutoDetectSpeech
    {
        get { return autoDetectSpeaking; }
        set { autoDetectSpeaking = value; }
    }

    public float AutoDetectMinFreq
    {
        get { return autoDetectFreq; }
        set { autoDetectFreq = value; }
    }

    public float AutoDetectSpeechAveFFTOffset
    {
        get { return minFreqDiffAboveAveToTransmit; }
        set { minFreqDiffAboveAveToTransmit = value; }
    }

    public bool TrackVolume
    {
        get { return trackVolume; }
        set { trackVolume = value; lastVolume = 0.0f; }
    }

    public float LastVolume
    {
        get { return lastVolume; }
    }

    public int NetworkId
    {
        get;
        set;
    }

    public string Device
    {
        get { return device; }
        set
        {
            if (value != null && !Microphone.devices.Contains(value))
            {
                Debug.LogError(value + " is not a valid microphone device");
                UpdateUserMicHWI(value + "(invalid)");
                return;
            }

            device = value;
            UpdateUserMicHWI(value);
        }
    }

    public bool HasDefaultDevice
    {
        get { return device == null; }
    }

    public bool HasSpecificDevice
    {
        get { return device != null; }
    }

    public bool IsTransmitting
    {
        get { return transmitToggled || forceTransmit > 0 || pushToTalkButtonDown; }
    }

    public bool IsRecording
    {
        get { return recording; }
    }

    public string[] AvailableDevices
    {
        get { return Microphone.devices; }
    }

    public event System.Action<VoiceChatPacket> NewSample;

    void Start()
    {
        if (instance != null && instance != this)
        {
            MonoBehaviour.Destroy(this);
            Debug.LogError("Only one instance of VoiceChatRecorder can exist");
            return;
        }
        if (GameManager.Inst.ServerConfig != "Assembly" && GameManager.Inst.ServerConfig != "MDONS")
            Application.RequestUserAuthorization(UserAuthorization.Microphone);
        NetworkId = -1;
        instance = this;
    }

    void OnEnable()
    {
        if (instance != null && instance != this)
        {
            MonoBehaviour.Destroy(this);
            Debug.LogError("Only one instance of VoiceChatRecorder can exist");
            return;
        }

        if (GameManager.Inst.ServerConfig != "Assembly" && GameManager.Inst.ServerConfig != "MDONS")
            Application.RequestUserAuthorization(UserAuthorization.Microphone);
        instance = this;
    }

    void OnDisable()
    {
        instance = null;
    }

    void OnDestroy()
    {
        instance = null;
    }


    


    DateTime lastTime = DateTime.Now;
    void FixedUpdate()
    {
        if (!recording || GameManager.Inst.LocalPlayerType == PlayerType.STEALTH)
        {
            return;
        }

        forceTransmit -= Time.deltaTime;

        bool transmit = transmitToggled || pushToTalkButtonDown;
        int currentPosition = Microphone.GetPosition(Device);

        // This means we wrapped around
        if (currentPosition < previousPosition)
        {
            lastTime = DateTime.Now;
            while (sampleIndex < recordFrequency)
            {
                ReadSample(transmit);
            }

            sampleIndex = 0;
        }

        // have had problems on the Mac with the voice suddenly going out, this is to fix it.
        if (recording && !Microphone.IsRecording(Device))
        {
            StopRecording();
            StartRecording();
        }

        // Read non-wrapped samples
        previousPosition = currentPosition;

        while (sampleIndex + recordSampleSize <= currentPosition)
        {
            ReadSample(transmit);
        }
    }

    void Resample(float[] src, float[] dst)
    {
        if (src.Length == dst.Length)
        {
            Array.Copy(src, 0, dst, 0, src.Length);
        }
        else
        {
            //TODO: Low-pass filter 
            float rec = 1.0f / (float)dst.Length;

            for (int i = 0; i < dst.Length; ++i)
            {
                float interp = rec * (float)i * (float)src.Length;
                dst[i] = src[(int)interp];
            }
        }
    }

    float GetMinFreqToTransmit()
    {
        const int minNumSamplesToUseAve = 25;
        return ((VoiceChatSettings.Instance.UseAverageFFTForVoiceDetect && ((int)fftMaxAve.NumSamples > minNumSamplesToUseAve)) ? ((float)(fftMaxAve.Average) + minFreqDiffAboveAveToTransmit) : autoDetectFreq);
    }

    void ReadSample(bool transmit)
    {
        // Extract data
        clip.GetData(sampleBuffer, sampleIndex);
        if( VoiceManager.Inst.micGain != 1.0f )
        {
            for (int i = 0; i < sampleBuffer.Length; i++)
                sampleBuffer[i] = Mathf.Clamp(sampleBuffer[i] * VoiceManager.Inst.logarithmicMicGain,-1.0f, 1.0f);
        }

        // Grab a new sample buffer
        float[] targetSampleBuffer = VoiceChatFloatPool.Instance.Get();

        // Resample our real sample into the buffer
        Resample(sampleBuffer, targetSampleBuffer);

        // Forward index
        sampleIndex += recordSampleSize;

        // Highest auto-detected frequency
        float freq = float.MinValue;

        // Auto detect speech, but no need to do if we're holding down a button to transmit
        if ((autoDetectSpeaking && transmit && !pushToTalkButtonDown) || trackVolume)
        {
            // Clear FFT buffer
            for (int i = 0; i < fftBuffer.Length; ++i)
            {
                fftBuffer[i] = 0;
            }

            // Copy to FFT buffer
            Array.Copy(targetSampleBuffer, 0, fftBuffer, 0, targetSampleBuffer.Length);

            // Apply FFT
            Exocortex.DSP.Fourier.FFT(fftBuffer, fftBuffer.Length / 2, Exocortex.DSP.FourierDirection.Forward);

            // Get highest frequency
            for (int i = 0; i < fftBuffer.Length; ++i)
            {
                if (fftBuffer[i] > freq)
                {
                    freq = fftBuffer[i];
                }
            }
            fftMaxAve.AddSample(freq);
            lastVolume = freq;
        }

        bool isTransmitting = (NewSample != null && transmit && ( forceTransmit > 0 || (freq >= GetMinFreqToTransmit()) || pushToTalkButtonDown));
        if (isTransmitting)
        {

            // If we auto-detected a voice, force recording for a while
            if (freq >= GetMinFreqToTransmit() || pushToTalkButtonDown)
                forceTransmit = forceTransmitTime;
            if (forceTransmit > 0)
                TransmitBuffer(targetSampleBuffer);
        }

        Player player = GameManager.Inst.playerManager.GetLocalPlayer();
        if (player != null)
            player.IsTalking = isTransmitting;
    }

    void TransmitBuffer(float[] buffer)
    {
        // Compress into packet
        VoiceChatPacket packet = VoiceChatUtils.Compress(buffer);

        // Set networkid of packet
        packet.NetworkId = NetworkId;

        // Raise event
        NewSample(packet);
    }

    void StopTransmitting()
    {
        forceTransmit = 0.0f;
    }

    public bool StartRecording()
    {
        if (NetworkId == -1 && !VoiceChatSettings.Instance.LocalDebug)
        {
            Debug.LogError("NetworkId is -1");
            return false;
        }

        if (recording)
        {
            Debug.LogError("Already recording");
            return false;
        }

        if (Microphone.devices.Length == 0)
        {
            if (!reportedInitErrors)
            {
                Debug.LogError("No Microphone found");
                AnnouncementManager.Inst.Announce("Warning", "No Microphone Found on this computer, your voice will not be heard by others");
                UpdateUserMicHWI("(none)");
                reportedInitErrors = true;
            }
            return false;
        }

        targetFrequency = VoiceChatSettings.Instance.Frequency;
        targetSampleSize = VoiceChatSettings.Instance.SampleSize;

        int minFreq;
        int maxFreq;
        Microphone.GetDeviceCaps(Device, out minFreq, out maxFreq);

        recordFrequency = minFreq == 0 && maxFreq == 0 ? 44100 : maxFreq;
        recordSampleSize = recordFrequency / (targetFrequency / targetSampleSize);

        clip = Microphone.Start(Device, true, 1, recordFrequency);
        sampleBuffer = new float[recordSampleSize];
        fftBuffer = new float[VoiceChatUtils.ClosestPowerOfTwo(targetSampleSize)];
        recording = true;

        return recording;
    }

    public void StopRecording()
    {
        clip = null;
        recording = false;
    }

    public void UpdateUserMicHWI(string newMic)
    {
        // Update local user variable.
        string oldMic = CommunicationManager.CurrentUserProfile.hwi_mic;
        CommunicationManager.CurrentUserProfile.hwi_mic = newMic;

        // Update Parse if mic is different from stored information.
        if (oldMic != CommunicationManager.CurrentUserProfile.hwi_mic)
            CommunicationManager.CurrentUserProfile.UpdateProfile("hwi_mic", newMic);
    }
}
