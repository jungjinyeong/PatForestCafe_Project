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
        if(UIMgr == null)
        {
            GameObject uiMgr = Resources.Load("UIManager") as GameObject;
            var create = GameObject.Instantiate(uiMgr);
            create.transform.position = Vector3.zero;
            create.transform.rotation = Quaternion.identity;
            create.transform.localScale = Vector3.one;
        }

        mSoundMgr = this.GetOrAddComponent<SoundManager>();
        mSoundMgr.Init();

        mInputMgr = this.GetOrAddComponent<InputManager>();

        mSceneMgr = new SceneManager();
        mSceneMgr.Init();
    }

    public void Clear()
    {
        mSoundMgr?.Clear();
        UIMgr?.Clear();
    }

    public void Reset()
    {
        mSoundMgr?.Destory();
    }
}