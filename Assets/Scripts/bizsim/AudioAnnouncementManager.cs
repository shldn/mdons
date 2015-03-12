using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TimeSoundPair
{
    public TimeSoundPair(AudioClip clip_, int timeToPlay_)
    {
        clip = clip_;
        timeToPlay = timeToPlay_;
        hasPlayed = false;
    }

    public AudioClip clip;
    public int timeToPlay;
    public bool hasPlayed;
}

public class AudioAnnouncementManager {

    private static AudioAnnouncementManager mInstance;
    public static AudioAnnouncementManager Instance
    {
        get
        {
            if (mInstance == null)
                mInstance = new AudioAnnouncementManager();
            return mInstance;
        }
    }

    public static AudioAnnouncementManager Inst { get { return Instance; } }
    GameObject go;
    AudioSource audioSource;
    List<TimeSoundPair> announceData = new List<TimeSoundPair>();

    private AudioAnnouncementManager()
    {
        go = new GameObject("AudioAnnouncements");
        audioSource = go.AddComponent<AudioSource>();
        announceData.Add(new TimeSoundPair((AudioClip)Resources.Load("Sounds/10Minutes"), 10 * 60));
        announceData.Add(new TimeSoundPair((AudioClip)Resources.Load("Sounds/5Minutes"), 5 * 60));
        announceData.Add(new TimeSoundPair((AudioClip)Resources.Load("Sounds/1Minute"), 1 * 60));
        announceData.Add(new TimeSoundPair((AudioClip)Resources.Load("Sounds/QuarterEnded"), 0 * 60));

    }

    public void Update(int time)
    {
        for (int i = 0; i < announceData.Count; ++i)
            if (!announceData[i].hasPlayed && time == announceData[i].timeToPlay)
            {
                audioSource.clip = announceData[i].clip;
                audioSource.Play();
                announceData[i].hasPlayed = true;
            }
    }

    public void Reset()
    {
        for (int i = 0; i < announceData.Count; ++i)
            announceData[i].hasPlayed = false;
    }

    public void Destroy()
    {
        GameObject.DestroyImmediate(go);
        mInstance = null;
    }

}
