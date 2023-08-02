
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour, IResManager
{
    [SerializeField] private List<AudioClip> _playlist = new List<AudioClip>(); 

    private AudioSource _audioSource = null;
    
    public void Init()
    {
        _audioSource = gameObject.GetComponent<AudioSource>();
    }

    public void Subscribe()
    {

    }

    public void Clear()
    {
        _playlist?.Clear();
    }

    public void Destory()
    {
        if(_playlist != null)
        {
            _playlist.Clear();
            _playlist = null;
        }
    }

    public void PlayEffect(AudioClip audioClip)
    {
        _playlist.Add(audioClip);
    }
}
