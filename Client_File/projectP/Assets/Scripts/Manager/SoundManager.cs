
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour, IManager
{
    private const string PREF_KEY_BGM_VOLUME = "Option_BgmVolume";
    private const string PREF_KEY_SFX_VOLUME = "Option_SfxVolume";

    [SerializeField] private List<AudioClip> mPlaylist = new List<AudioClip>();

    private AudioSource mAudioSource = null;

    public float BgmVolume { get; private set; } = 1f;
    public float SfxVolume { get; private set; } = 1f;

    public void Init()
    {
        mAudioSource = gameObject.GetComponent<AudioSource>();

        BgmVolume = PlayerPrefs.GetFloat(PREF_KEY_BGM_VOLUME, 1f);
        SfxVolume = PlayerPrefs.GetFloat(PREF_KEY_SFX_VOLUME, 1f);

        mAudioSource.volume = BgmVolume;
    }

    public void SetBgmVolume(float volume)
    {
        BgmVolume = Mathf.Clamp01(volume);
        mAudioSource.volume = BgmVolume;

        PlayerPrefs.SetFloat(PREF_KEY_BGM_VOLUME, BgmVolume);
    }

    public void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);

        PlayerPrefs.SetFloat(PREF_KEY_SFX_VOLUME, SfxVolume);
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
