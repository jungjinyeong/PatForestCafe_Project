using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UniRx;
using Unity.VisualScripting;

public partial class GameInstance : MonoBehaviour
{
    void Awake()
    {
        _instance = this;
        DontDestroyOnLoad(this);

        Init();
    }

    public void Init()
    {
        mSoundMgr = this.GetOrAddComponent<SoundManager>();
        mSoundMgr.Init();

        mInputMgr = this.GetOrAddComponent<InputManager>();

    }

    public void Clear()
    {
        mSoundMgr?.Clear();
    }

    public void Reset()
    {
        mSoundMgr?.Destory();

    }
}