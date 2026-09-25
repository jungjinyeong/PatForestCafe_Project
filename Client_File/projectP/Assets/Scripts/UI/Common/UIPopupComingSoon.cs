using UnityEngine;
using TMPro;
using Extension;

// 아직 대응 시스템이 없는 서비스를 눌렀을 때 띄우는 "준비 중" 안내 팝업(프리팹 UI_Popup_ComingSoon).
// 2026-09: UI_Root_Lobby/UI_Popup_ShopStreet에 각각 붙어 있던 Panel_MousePlaceholder를 팝업으로 분리.
public class UIPopupComingSoon : UIWndBase, IUIParam<UIPopupComingSoon.Param>
{
    public struct Param
    {
        // null/빈 문자열이면 프리팹에 적힌 기본 문구를 그대로 쓴다.
        public string Message;
    }

    [SerializeField] private TextMeshProUGUI mTextMessage;

    private string mDefaultMessage;

    public override eUIType GetUIType() => eUIType.PopupComingSoon;

    public override void Init()
    {
        base.Init();

        mDefaultMessage = mTextMessage != null ? mTextMessage.text : string.Empty;
    }

    public void Set(Param param)
    {
        mTextMessage.SetTextEx(string.IsNullOrEmpty(param.Message) ? mDefaultMessage : param.Message);
    }
}
