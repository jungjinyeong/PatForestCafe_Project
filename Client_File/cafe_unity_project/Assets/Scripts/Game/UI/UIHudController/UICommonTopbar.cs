using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Framework.UI;
using Framework.Extension;
using Game.Event;

public class UICommonTopbar : UIHudBase
{
    [SerializeField]
    private UIButtonEx mBtnMenu;


    public override void Init()
    {
        base.Init();

        mBtnMenu.OnSubscribeOnClick(_ =>
        {

        }).AddTo(this);
    }

    public override void OnEvent(IUIEvent evt)
    {
        
    }
}