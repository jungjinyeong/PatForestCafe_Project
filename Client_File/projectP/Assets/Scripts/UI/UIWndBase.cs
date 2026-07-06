using System;
using System.Collections.Generic;

using UnityEngine;

using UniRx;
using Extension;

public abstract class UIWndBase : UIBase
{
    [SerializeField] private UIButtonEx mBtnClose;
    [SerializeField] private UIButtonEx mBtnBgClose;

    protected UIManager UIMgr => GameInstance.UI;

    public abstract eUIType GetUIType();

    public virtual void Init()
    {
        if(mBtnClose != null)
            mBtnClose.OnSubscribeOnClick(SelfClose).AddTo(this);
        if(mBtnBgClose != null)
            mBtnBgClose.OnSubscribeOnClick(SelfClose).AddTo(this);
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