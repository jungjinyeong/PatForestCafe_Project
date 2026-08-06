
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

    // 스페셜 주문 기획이 삭제되어 이 팝업은 더 이상 InputManager에서 열리지 않는다(도달 불가능한 죽은 코드).
    // UI_Popup_OrderDetail.prefab의 스크립트 참조(GUID)가 깨지지 않도록 파일/클래스는 남겨두고
    // 컴파일만 유지한다 — 프리팹 정리는 Unity 에디터에서 진행할 것.
    private void OnClickOpenSpecialDrink()
    {
        UIMgr.Open<UIPopupSpecialDrinkProduction, UIPopupSpecialDrinkProduction.Param>(eUIType.UIPopupSpecialDrinkProduction,
            new UIPopupSpecialDrinkProduction.Param());
    }

    private void OnClickConfirm()
    {
        SelfClose();
    }
}
