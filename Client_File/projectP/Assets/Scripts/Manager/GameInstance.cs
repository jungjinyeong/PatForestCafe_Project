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
        mResourceMgr = this.GetOrAddComponent<ResourceMgr>();
        mResourceMgr.Init();

        mTableMgr = new TableManager();
        mTableMgr.LoadAllTables();

        if (UI == null)
        {
            GameObject uiMgr = Resources.Load("UIManager") as GameObject;
            if(uiMgr != null)
            {
                var create = GameObject.Instantiate(uiMgr);
                create.transform.position = Vector3.zero;
                create.transform.rotation = Quaternion.identity;
                create.transform.localScale = Vector3.one;
            }
        }

        mSoundMgr = this.GetOrAddComponent<SoundManager>();
        mSoundMgr.Init();

        mInputMgr = this.GetOrAddComponent<InputManager>();
        mInputMgr.Init();

        mWayPointMgr = this.GetOrAddComponent<WayPointManager>();
        mWayPointMgr.Init();

        mPoolMgr = this.GetOrAddComponent<ObjectPoolManager>();
        mPoolMgr.Init();

        mSpawnMgr = this.GetOrAddComponent<SpawnManager>();

        mCommonModelMgr = this.GetOrAddComponent<CommonModelManager>();
        mCommonModelMgr.Init();

        mSceneMgr = new SceneManager();
        mSceneMgr.Init();

        mTimeMgr = this.GetOrAddComponent<TimeManager>();
        mTimeMgr.Init();

        mDayNightMgr = this.GetOrAddComponent<DayNightManager>();
        mDayNightMgr.Init();
    }

    public void Clear()
    {
        mSoundMgr?.Clear();
        mCommonModelMgr?.Clear();
        UI?.Clear();
    }

    public void Reset()
    {
        mSoundMgr?.Destory();
    }
}