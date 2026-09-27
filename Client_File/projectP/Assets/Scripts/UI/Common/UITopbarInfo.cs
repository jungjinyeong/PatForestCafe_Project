using System.Collections.Generic;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Extension;

public class UITopbarInfo : UIHUDBase
{
    [SerializeField] private SerializableDictionary<CTable.eMoneyType, UIWealthItem> mWealthItems = new();

    // 카페 레벨/경험치. 프리팹에 배선되기 전일 수 있어 전부 null 체크한다.
    [SerializeField] private TextMeshProUGUI mTextCafeLevel;
    [SerializeField] private TextMeshProUGUI mTextCafeExp;
    [SerializeField] private Image mImageExpGauge;      // Image.type = Filled
    [SerializeField] private UIButtonEx mBtnCafeLevel;  // 누르면 경험치 안내 토글
    [SerializeField] private GameObject mExpGuideRoot;
    [SerializeField] private UIButtonEx mBtnExpGuideClose;

    // 날짜/시계 + 영업 상태. 영업 중에 누르면 조기 마감 확인, 영업 종료 후 누르면 정산 팝업.
    [SerializeField] private TextMeshProUGUI mTextDayClock;
    [SerializeField] private UIButtonEx mBtnShopStatus;
    [SerializeField] private TextMeshProUGUI mTextShopStatus;
    [SerializeField] private Color mColorOpen = new Color(0.45f, 0.7f, 0.4f);
    [SerializeField] private Color mColorClosed = new Color(0.62f, 0.52f, 0.46f);

    public override void Init()
    {
        base.Init();

        SubscribeWealthInfos();
        SubscribeCafeLevel();
        SubscribeBusiness();
    }

    private void OnClickShopStatus()
    {
        switch (GameInstance.Model.Business.State.Value)
        {
            case eBusinessState.Open:
                GameInstance.UI.Open<UIPopupConfirm, UIPopupConfirm.Param>(eUIType.PopupConfirm, new UIPopupConfirm.Param
                {
                    Title = "오늘 영업을 마감할까요?",
                    Message = "새로운 손님의 입장이 중단됩니다.\n매장에 남은 손님이 모두 나가면 정산하고 저장합니다.",
                    OkText = "조기 마감",
                    CancelText = "계속 영업",
                    OnOk = () => GameInstance.Model.Business.RequestClose(),
                });
                break;

            case eBusinessState.Closed:
                GameInstance.UI.Open<UIPopupSettlement, UIPopupSettlement.Param>(eUIType.PopupSettlement, new UIPopupSettlement.Param());
                break;
        }
    }

    private void OnClickCafeLevel()
    {
        if (mExpGuideRoot != null)
            mExpGuideRoot.SetActive(!mExpGuideRoot.activeSelf);
    }

    private void OnClickExpGuideClose()
    {
        if (mExpGuideRoot != null)
            mExpGuideRoot.SetActive(false);
    }

    private void SubscribeWealthInfos()
    {
        foreach (var item in mWealthItems)
        {
            var wealth = GameInstance.Model.Item.GetWealth(item.Key);
            var wealthItem = item.Value;

            if (wealth == null)
            {
                wealthItem.UpdateWealthInfos(0);
                continue;
            }

            wealth.Count
                .Subscribe(amount => wealthItem.UpdateWealthInfos(amount))
                .AddTo(this);
        }
    }

    private void SubscribeCafeLevel()
    {
        if (mExpGuideRoot != null)
            mExpGuideRoot.SetActive(false);
        // UnityEngine.Object의 fake null 때문에 ?. 대신 != null로 검사한다.
        if (mBtnCafeLevel != null)
            mBtnCafeLevel.OnSubscribeOnClick(OnClickCafeLevel).AddTo(this);
        if (mBtnExpGuideClose != null)
            mBtnExpGuideClose.OnSubscribeOnClick(OnClickExpGuideClose).AddTo(this);

        var cafe = GameInstance.Model.Cafe;
        cafe.Level.CombineLatest(cafe.Exp, (level, exp) => Unit.Default)
            .Subscribe(_ => RefreshCafeLevel())
            .AddTo(this);
    }

    private void SubscribeBusiness()
    {
        if (mBtnShopStatus != null)
            mBtnShopStatus.OnSubscribeOnClick(OnClickShopStatus).AddTo(this);

        var business = GameInstance.Model.Business;

        // 게임 분 단위가 바뀔 때만 다시 그린다.
        business.Day.CombineLatest(GameInstance.Time.CurrentHour.Select(h => Mathf.FloorToInt(h * 60f)).DistinctUntilChanged(),
                (day, minutes) => (day, minutes))
            .Subscribe(v => mTextDayClock.SetTextEx($"{business.GetDateText(v.day)} {FormatClock(v.minutes)}"))
            .AddTo(this);

        business.State.CombineLatest(GameInstance.Spawn.ActiveCount, (state, guests) => (state, guests))
            .Subscribe(v => RefreshShopStatus(v.state, v.guests))
            .AddTo(this);
    }

    private void RefreshShopStatus(eBusinessState state, int guests)
    {
        switch (state)
        {
            case eBusinessState.Open:
                mTextShopStatus.SetTextEx("영업 중 OPEN");
                break;
            case eBusinessState.Closing:
                mTextShopStatus.SetTextEx($"마감 중 · 손님 {guests}명");
                break;
            default:
                mTextShopStatus.SetTextEx("영업 종료");
                break;
        }

        if (mTextShopStatus != null)
            mTextShopStatus.color = state == eBusinessState.Open ? mColorOpen : mColorClosed;
    }

    // 하루 분(0~1439) → "AM 10:24"
    private static string FormatClock(int minutes)
    {
        int hour = (minutes / 60) % 24;
        int minute = minutes % 60;
        string ampm = hour < 12 ? "AM" : "PM";
        int hour12 = hour % 12 == 0 ? 12 : hour % 12;
        return $"{ampm} {hour12:00}:{minute:00}";
    }

    private void RefreshCafeLevel()
    {
        var cafe = GameInstance.Model.Cafe;
        int required = cafe.RequiredExp;

        mTextCafeLevel.SetTextEx($"Lv. {cafe.Level.Value}");
        mTextCafeExp.SetTextEx(cafe.IsMaxLevel ? "MAX" : $"{cafe.Exp.Value} / {required}");

        if (mImageExpGauge != null)
            mImageExpGauge.fillAmount = cafe.IsMaxLevel ? 1f : (float)cafe.Exp.Value / required;
    }
}
