using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Framework.UI;
using Framework.Extension;

public class UIRootLogin : UIWndBase
{
    [SerializeField]
    UIButtonEx mBtnStart = null;

    #region override
    public override eUIType GetUIType() => eUIType.UIRootLogin;

    public override void Init()
    {
        base.Init();

        mBtnStart.OnSubscribeOnClick(_ =>
        {
            GameInstance.SceneMgr.ChangeScene(Game.Scene.eScene.Ingame);
        }).AddTo(this);
    }

    public override void Open()
    {
        base.Open();


    }
    #endregion
}