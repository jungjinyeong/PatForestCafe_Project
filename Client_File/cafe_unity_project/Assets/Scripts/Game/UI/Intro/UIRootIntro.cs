﻿using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Framework.UI;
using Framework.Extension;

public class UIRootIntro : UIWndBase
{
    #region override
    public override eUIType GetUIType() => eUIType.UIRootIntro;

    public override void Init()
    {
        base.Init();

    }

    public override void Open()
    {
        base.Open();

        Process().Forget();
    }
    #endregion

    async UniTask Process()
    {
        await UniTask.Delay(400);

        GameInstance.SceneMgr.ChangeScene(Game.Scene.eScene.Login);
    }
}