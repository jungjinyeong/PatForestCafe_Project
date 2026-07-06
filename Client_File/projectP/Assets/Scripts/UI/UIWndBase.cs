using System;
using System.Collections.Generic;

using UnityEngine;

using UniRx;
using Extension;

public abstract class UIWndBase : UIBase
{
    [SerializeField] private UIButtonEx btnClose;
    [SerializeField] private UIButtonEx btnBgClose;

    protected UIManager UIMgr => GameInstance.UI;

    public abstract eUIType GetUIType();

    public virtual void Init()
    {
        btnClose.OnSubscribeOnClick(SelfClose).AddTo(this);
        btnBgClose.OnSubscribeOnClick(SelfClose).AddTo(this);
    }

    public virtual void Destroy() { }

    public virtual void Open()
    {
        Active();
    }

    public void Close()
    {
        Deative();
    }

    protected void SelfClose()
    {
        UIMgr.Close(GetUIType());
    }

}