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

    // 날짜/시계 + 영업 상태. 영업 상태 자체가 버튼이다(로비 UX 개편: 별도 영업 버튼 없음).
    // 영업 중 = 조기 마감 확인, 영업 종료(정산 후)·영업 준비 = 영업 시작 확인, 마감 중 = 반응 없음.
    [SerializeField] private TextMeshProUGUI mTextDayClock;
    [SerializeField] private TextMeshProUGUI mTextShopStatus;
    [SerializeField] private UIButtonEx mBtnShopStatus;
    [SerializeField] private Color mColorOpen = new Color(0.45f, 0.7f, 0.4f);
    [SerializeField] private Color mColorClosed = new Color(0.62f, 0.52f, 0.46f);

    public override void Init()
    {
        base.Init();

        SubscribeWealthInfos();
        SubscribeCafeLevel();
        SubscribeBusiness();
    }

    private void OnClickBusiness()
    {
        var business = GameInstance.Model.Business;
        if (business.State.Value == eBusinessState.Open)
        {
            GameInstance.UI.Open<UIPopupConfirm, UIPopupConfirm.Param>(eUIType.PopupConfirm, new UIPopupConfirm.Param
            {
                Title = "오늘 영업을 마감할까요?",
                Message = "새로운 손님의 입장이 중단됩니다.\n매장에 남은 손님이 모두 나가면 정산하고 저장합니다.",
                OkText = "조기 마감",
                CancelText = "계속 영업",
                OnOk = () => GameInstance.Model.Business.RequestClose(),
            });
        }
        else if (business.CanEditLayout)
        {
            // 영업 종료(정산 후)·영업 준비. 배치 중이던 가구는 취소되고 배치 모드가 닫힌다(GameModeLobby).
            GameInstance.UI.Open<UIPopupConfirm, UIPopupConfirm.Param>(eUIType.PopupConfirm, new UIPopupConfirm.Param
            {
                Title = "영업을 시작할까요?",
                Message = "손님이 들어오기 시작해요.\n영업 중에는 가구를 배치할 수 없어요.",
                OkText = "영업 시작",
                CancelText = "준비 계속",
                OnOk = GameModeLobby.StartBusiness,
            });
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
            mBtnShopStatus.OnSubscribeOnClick(OnClickBusiness).AddTo(this);

        var business = GameInstance.Model.Business;

        // 게임 분 단위가 바뀔 때만 다시 그린다.
        business.Day.CombineLatest(GameInstance.Time.CurrentHour.Select(h => Mathf.FloorToInt(h * 60f)).DistinctUntilChanged(),
                (day, minutes) => (day, minutes))
            .Subscribe(v => mTextDayClock.SetTextEx($"{FormatDate(business, v.day)}   {FormatClock(v.minutes)}"))
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
                mTextShopStatus.SetTextEx("영업 중");
                break;
            case eBusinessState.Closing:
                mTextShopStatus.SetTextEx($"마감 중 · 손님 {guests}명");
                break;
            case eBusinessState.Preparing:
                mTextShopStatus.SetTextEx("영업 준비 중");
                break;
            default:
                mTextShopStatus.SetTextEx("영업 종료");
                break;
        }

        if (mTextShopStatus != null)
            mTextShopStatus.color = state == eBusinessState.Open ? mColorOpen : mColorClosed;

        // 마감 중에는 누를 수 있는 동작이 없으므로 버튼을 비활성화해 눌리는 것처럼 보이지 않게 한다.
        if (mBtnShopStatus != null)
            mBtnShopStatus.interactable = state != eBusinessState.Closing;
    }

    // "1일 · 봄" (계절 테이블이 없으면 "3일차")
    private static string FormatDate(BusinessModel business, int day)
    {
        var season = business.GetSeason(day);
        return season != null ? $"{business.GetDayInSeason(day)}일 · {season.Name}" : $"{day}일차";
    }

    // 하루 분(0~1439) → "10:24 AM"
    private static string FormatClock(int minutes)
    {
        int hour = (minutes / 60) % 24;
        int minute = minutes % 60;
        string ampm = hour < 12 ? "AM" : "PM";
        int hour12 = hour % 12 == 0 ? 12 : hour % 12;
        return $"{hour12:00}:{minute:00} {ampm}";
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
