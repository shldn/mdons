using System;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class VoiceChatPlayer : MonoBehaviour
{
    float lastTime = 0;
    double played = 0;
    double received = 0;
    int index = 0;
    float[] data;
    float playDelay = 0;
    bool shouldPlay = false;
    float lastRecvTime = 0;
	bool initialized = false;
    NSpeex.SpeexDecoder speexDec = new NSpeex.SpeexDecoder(NSpeex.BandMode.Narrow);
	public int userId = -1;

    [SerializeField]
    int playbackDelay = 2;

    public float LastRecvTime
    {
        get { return lastRecvTime; }
    }

    void Start()
    {
		if( initialized )
			return;
        int size = VoiceChatSettings.Instance.Frequency * 20;

        GetComponent<AudioSource>().loop = true;
        GetComponent<AudioSource>().clip = AudioClip.Create("VoiceChat", size, 1, VoiceChatSettings.Instance.Frequency, false, false);
        data = new float[size];

        if (VoiceChatSettings.Instance.LocalDebug)
        {
            VoiceChatRecorder.Instance.NewSample += OnNewSample;
        }
		initialized = true;
    }

    void FixedUpdate()
    {
        if (GetComponent<AudioSource>().isPlaying)
        {
            // Wrapped around
            if (lastTime > GetComponent<AudioSource>().time)
            {
                played += GetComponent<AudioSource>().clip.length;
            }

            lastTime = GetComponent<AudioSource>().time;

            // Check if we've played too far
            if (played + GetComponent<AudioSource>().time >= received)
            {
                Stop();
                shouldPlay = false;
                Player player = null;
                if (GameManager.Inst.playerManager.TryGetPlayer(userId, out player) && player != null)
                    player.IsTalking = false;
                if (VoiceChatSettings.Instance.NoInteruptMode)
                    VoiceChatRecorder.Instance.StartRecording();
            }
            else
                if (VoiceChatSettings.Instance.NoInteruptMode)
                    VoiceChatRecorder.Instance.StopRecording();
        }
        else
        {
            if (shouldPlay)
            {
                playDelay -= Time.deltaTime;

                if (playDelay <= 0)
                {
                    GetComponent<AudioSource>().Play();
                    Player player = null;
                    if (GameManager.Inst.playerManager.TryGetPlayer(userId, out player) && player != null)
                        player.IsTalking = true;
                }
            }
        }
    }

    void Stop()
    {
        GetComponent<AudioSource>().Stop();
        GetComponent<AudioSource>().time = 0;
        index = 0;
        played = 0;
        received = 0;
        lastTime = 0;
    }

    public void OnNewSample(VoiceChatPacket packet)
    {
		if( !initialized )
			Start();
		
        // Store last packet

        // Set last time we got something
        lastRecvTime = Time.time;

        // Decompress
        float[] sample = null;
        int length = VoiceChatUtils.Decompress(speexDec, packet, out sample);

        // Add more time to received
        received += VoiceChatSettings.Instance.SampleTime;

        // Push data to buffer
        Array.Copy(sample, 0, data, index, length);

        // Increase index
        index += length;

        // Handle wrap-around
        if (index >= GetComponent<AudioSource>().clip.samples)
        {
            index = 0;
        }

        // Set data
        GetComponent<AudioSource>().clip.SetData(data, 0);

        // If we're not playing
        if (!GetComponent<AudioSource>().isPlaying)
        {
            // Set that we should be playing
            shouldPlay = true;

            // And if we have no delay set, set it.
            if (playDelay <= 0)
            {
                playDelay = (float)VoiceChatSettings.Instance.SampleTime * playbackDelay;
            }
        }

        VoiceChatFloatPool.Instance.Return(sample);
    }
}