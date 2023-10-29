
using Cysharp.Threading.Tasks;
using Framework.Scene;
using Game.Scene;
using System;
using System.Collections.Generic;
using UnityEngine;

public class SceneManager : IResManager
{
    SceneBase mCurScene;

    public void Init()
    {
        var find = GameObject.Find(eScene.Intro.ToString());

        var curScene = find.GetComponent<SceneBase>();

        mCurScene = curScene;

        mCurScene.Prepare();
    }

    public void Subscribe()
    {
    }

    public void Destory()
    {
    }

    public void Clear()
    {
    }

    public void Add(SceneBase page)
    {
    }

    public SceneBase Find(eScene ID)
    {
        return null;
    }

    public async UniTask<SceneBase> Load(eScene ID)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(ID.ToString(), UnityEngine.SceneManagement.LoadSceneMode.Single);

        await UniTask.Delay(100);

        var find = GameObject.Find(ID.ToString());
        if(null == find)
        {
            Debug.LogError($"null == find {ID}");
            return null;
        }

        var curScene = find.GetComponent<SceneBase>();
        return curScene;
    }

    public void ChangeScene(eScene changeID)
    {
        Processing(changeID).Forget();
    }

    protected async UniTask Processing(eScene changeID)
    {
        if (mCurScene != null)
        {
            GameInstance.Instance.Clear();

            mCurScene.OnExit();
        }

        var nextScene = await Load(changeID);

        if (nextScene != null)
        {
            mCurScene = nextScene;

            mCurScene.Prepare();

            await mCurScene.Preprocessing();

            mCurScene.OnEnter();
        }
        else
        {
            Debug.LogError("next scene == null.");
        }
    }
}