using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Framework.UI;
using Framework.Extension;

public class UIRootIntro : UIWndBase, IUIParam<UIRootIntro.Param>
{
    [SerializeField]
    private Button mBtnStart;

    public struct Param
    {
        public string strName;
        public Param(string strName)
        {
            this.strName = strName;
        }
    }

    #region override
    public override eUIType GetUIType() => eUIType.UIRootIntro;

    public override void Init()
    {
        base.Init();
        mBtnStart?.OnSubscribeOnClick(_ =>
        {
            UIMgr.Open<UIRootLobby, UIEmptyParam>(eUIType.UIRootLobby, new UIEmptyParam());
        }).AddTo(this);
    }

    public void Set(Param param)
    {

    }
    #endregion
}