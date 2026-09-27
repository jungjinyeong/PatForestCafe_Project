using System;
using TMPro;
using UniRx;
using UnityEngine;
using Extension;

// 범용 확인 팝업(프리팹 UI_Popup_Confirm). 제목·본문·버튼 문구와 확인 콜백을 Param으로 받는다.
// 취소/✕/배경 클릭은 아무것도 하지 않고 닫는다.
public class UIPopupConfirm : UIWndBase, IUIParam<UIPopupConfirm.Param>
{
    public struct Param
    {
        public string Title;
        public string Message;
        public string OkText;       // 비면 "확인"
        public string CancelText;   // 비면 "취소"
        public Action OnOk;
    }

    [SerializeField] private TextMeshProUGUI mTextTitle;
    [SerializeField] private TextMeshProUGUI mTextMessage;
    [SerializeField] private UIButtonEx mBtnOk;
    [SerializeField] private TextMeshProUGUI mTextOk;
    [SerializeField] private UIButtonEx mBtnCancel;
    [SerializeField] private TextMeshProUGUI mTextCancel;

    private Action mOnOk;

    public override eUIType GetUIType() => eUIType.PopupConfirm;

    public override void Init()
    {
        base.Init();

        mBtnOk.OnSubscribeOnClick(OnClickOk).AddTo(this);
        mBtnCancel.OnSubscribeOnClick(SelfClose).AddTo(this);
    }

    public void Set(Param param)
    {
        mOnOk = param.OnOk;
        mTextTitle.SetTextEx(param.Title);
        mTextMessage.SetTextEx(param.Message);
        mTextOk.SetTextEx(string.IsNullOrEmpty(param.OkText) ? "확인" : param.OkText);
        mTextCancel.SetTextEx(string.IsNullOrEmpty(param.CancelText) ? "취소" : param.CancelText);
    }

    private void OnClickOk()
    {
        var onOk = mOnOk;
        mOnOk = null;
        SelfClose();
        onOk?.Invoke();
    }
}
