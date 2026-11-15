using TMPro;
using UniRx;
using UnityEngine;
using Extension;

// 영업 정산 팝업(프리팹 UI_Popup_Settlement). 영업 종료(BusinessModel.State == Closed) 시 GameModeLobby가 연다.
// [확인]은 영업 종료 상태에서만 동작하며 다음 날 영업 준비로 넘어간다
// (영업 시작은 탑바의 [영업 시작] 버튼). 버튼 필드 이름(mBtnNextDay)은 프리팹 연결을 유지하려고 그대로 둔다.
public class UIPopupSettlement : UIWndBase, IUIParam<UIPopupSettlement.Param>
{
    public struct Param
    {
    }

    [SerializeField] private TextMeshProUGUI mTextTitle;
    [SerializeField] private TextMeshProUGUI[] mRowLabels;
    [SerializeField] private TextMeshProUGUI[] mRowValues;
    [SerializeField] private UIButtonEx mBtnNextDay;
    [SerializeField] private TextMeshProUGUI mTextNextDay;

    public override eUIType GetUIType() => eUIType.PopupSettlement;

    public override void Init()
    {
        base.Init();

        mBtnNextDay.OnSubscribeOnClick(OnClickConfirm).AddTo(this);
    }

    public override void Open()
    {
        base.Open();

        Refresh();
    }

    public void Set(Param param)
    {
    }

    private void OnClickConfirm()
    {
        SelfClose();
        GameModeLobby.ConfirmSettlement();
    }

    private void Refresh()
    {
        var business = GameInstance.Model.Business;
        bool isClosed = business.State.Value == eBusinessState.Closed;

        mTextTitle.SetTextEx($"{business.GetDateText(business.Day.Value)} 영업 정산");

        var rows = new (string label, string value)[]
        {
            ("오늘 매출", $"{business.TodayGold.Value:N0} G"),
            ("빵 판매", $"{business.TodayDessertSold.Value}개"),
            ("음료 판매", $"{business.TodayDrinkSold.Value}잔"),
            ("배달 완료", $"{business.TodayDelivered.Value}건"),
            ("방문 손님", $"{business.TodayVisitors.Value}명"),
            ("카페 경험치", $"+{business.TodayExp.Value}"),
        };

        for (int i = 0; i < mRowLabels.Length && i < mRowValues.Length; i++)
        {
            bool active = i < rows.Length;
            mRowLabels[i].transform.parent.gameObject.SetActive(active);
            if (!active) continue;
            mRowLabels[i].SetTextEx(rows[i].label);
            mRowValues[i].SetTextEx(rows[i].value);
        }

        mBtnNextDay.interactable = isClosed;
        mTextNextDay.SetTextEx(isClosed ? "확인" : "영업 중");
    }
}
