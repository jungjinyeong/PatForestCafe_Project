
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : Singleton<AudioManager>
{
    [SerializeField] private List<AudioClip> _playlist = new List<AudioClip>(); 

    private AudioSource _audioSource = null;
    
    public override void Initialize()
    {
        base.Initialize();

        _audioSource = gameObject.GetComponent<AudioSource>();
    }

    public override void Destroy()
    {
        base.Destroy();
        _playlist.Clear();
        _playlist = null;
    }

    public void PlayEffect(AudioClip audioClip)
    {
        _playlist.Add(audioClip);
    }
}
