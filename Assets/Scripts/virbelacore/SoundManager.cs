using UnityEngine;
using System.Collections.Generic;
using Sfs2X.Core;
using Sfs2X.Requests;
using Sfs2X.Entities;
using Sfs2X.Entities.Variables;

public class SoundManager : MonoBehaviour
{
    private GameObject go;
    private AudioSource teleportSound;
    private AudioSource clickSound;
    private AudioSource messageSound;
    private AudioSource factorySpawnSound;
    private AudioSource enterSound;
    private AudioSource exitSound;
    private static SoundManager mInstance;
    public static SoundManager Instance
    {
        get
        {
            if (mInstance == null)
            {
                mInstance = (new GameObject("SoundManager")).AddComponent(typeof(SoundManager)) as SoundManager;
            }
            return mInstance;
        }
    }

    public static SoundManager Inst
    {
        get { return Instance; }
    }

    void Awake()
    {
        go = new GameObject("AudioSource");
        teleportSound = go.AddComponent<AudioSource>();
        teleportSound.volume = 0.15f;
        teleportSound.clip = (AudioClip)Resources.Load("Sounds/Teleport");

        clickSound = go.AddComponent<AudioSource>();
        clickSound.volume = 0.15f;
        clickSound.clip = (AudioClip)Resources.Load("Sounds/Click");

        messageSound = go.AddComponent<AudioSource>();
        messageSound.volume = 0.15f;
        messageSound.clip = (AudioClip)Resources.Load("Sounds/IM_Sound");

        factorySpawnSound = go.AddComponent<AudioSource>();
        factorySpawnSound.volume = 0.35f;
        factorySpawnSound.clip = (AudioClip)Resources.Load("Sounds/Factory_Spawn");

        enterSound = go.AddComponent<AudioSource>();
        enterSound.volume = 0.35f;
        enterSound.clip = (AudioClip)Resources.Load("Sounds/Enter");

        exitSound = go.AddComponent<AudioSource>();
        exitSound.volume = 0.35f;
        exitSound.clip = (AudioClip)Resources.Load("Sounds/Exit");
    }
    public void Touch() { }

    public void PlayTeleport()
    {
        teleportSound.Play();
    }

    public void PlayPM()
    {
        messageSound.Play();
    }

    public void PlayClick()
    {
        clickSound.Play();
    }

    public void PlayFactorySpawn()
    {
        factorySpawnSound.Play();
    }

    public void PlayEnter()
    {
        enterSound.Play();
    }

    public void PlayExit()
    {
        exitSound.Play();
    }
        
}
