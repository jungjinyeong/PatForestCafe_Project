
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour, IManager
{
    [SerializeField] private List<AudioClip> mPlaylist = new List<AudioClip>(); 

    private AudioSource mAudioSource = null;
    
    public void Init()
    {
        mAudioSource = gameObject.GetComponent<AudioSource>();
    }

    public void Subscribe()
    {

    }

    public void Clear()
    {
        mPlaylist?.Clear();
    }

    public void Destory()
    {
        if(mPlaylist != null)
        {
            mPlaylist.Clear();
            mPlaylist = null;
        }
    }

    public void PlayEffect(AudioClip audioClip)
    {
        mPlaylist.Add(audioClip);
    }
}
