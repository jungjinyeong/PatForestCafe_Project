
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;
using Extension;


public class UIPopupOrderDetail : UIWndBase, IUIParam<UIPopupOrderDetail.Param>
{
    public struct Param
    {
        public BoxCollider2D npc;

    }

    [Header("NPC")]
    [SerializeField] private GameObject mNpcRootTr;

    [Header("OrderDdetail")]
    [SerializeField] private TextMeshProUGUI mTextTitle;
    [SerializeField] private TextMeshProUGUI mTextDesc;

    [Header("Buttons")]
    [SerializeField] private UIButtonEx mBtnOpenSpecialDrink;
    [SerializeField] private UIButtonEx mBtnConfirm;

    private BoxCollider2D mNpcCollider;

    public override eUIType GetUIType() => eUIType.PopupOrderDetail;

    public override void Init()
    {
        base.Init();

        mBtnOpenSpecialDrink.OnSubscribeOnClick(OnClickOpenSpecialDrink).AddTo(this);
        mBtnConfirm.OnSubscribeOnClick(OnClickConfirm).AddTo(this);
    }

    public void Set(Param param)
    {
        mNpcCollider = param.npc;
    }

    public override void Open()
    {
        base.Open();

        SetupNpc();
    }

    public override void Destroy()
    {
    }

    private void SetupNpc()
    {

    }

    private void OnClickOpenSpecialDrink()
    {
        UIMgr.Open<UIPopupSpecialDrinkProduction, UIPopupSpecialDrinkProduction.Param>(eUIType.UIPopupSpecialDrinkProduction, 
            new UIPopupSpecialDrinkProduction.Param());
    }

    private void OnClickConfirm()
    {
        var npc = mNpcCollider != null ? mNpcCollider.GetComponentInParent<CharNpc>() : null;
        npc?.ResumeFromSpecialOrderWait();

        SelfClose();
    }
}
