
using Cysharp.Threading.Tasks;
using Framework.Scene;
using Game.Scene;
using System.Collections.Generic;

public class SceneManager : IResManager
{
    Dictionary<eScene, SceneBase> m_SceneDic = new Dictionary<eScene, SceneBase>();

    SceneBase mCurScene;

    public void Init()
    {
    }

    public void Subscribe()
    {
    }

    public void Destory()
    {
        m_SceneDic?.Clear();
        m_SceneDic = null;
    }

    public void Clear()
    {
        m_SceneDic?.Clear();
    }

    public void Add(SceneBase page)
    {
        m_SceneDic.Add(page.ID, page);
    }

    public SceneBase Find(eScene ID)
    {
        if (m_SceneDic.ContainsKey(ID))
        {
            return m_SceneDic[ID];
        }
        return null;
    }

    public void ChangeScene(eScene changeID)
    {
        Processing(changeID).Forget();
    }

    protected async UniTask Processing(eScene changeID)
    {
        if (mCurScene != null)
        {
            mCurScene.OnExit();
        }

        var nextScene = Find(changeID);

        if (nextScene != null)
        {
            mCurScene = nextScene;

            await mCurScene.Preprocessing();

            mCurScene.OnEnter();
        }
    }
}