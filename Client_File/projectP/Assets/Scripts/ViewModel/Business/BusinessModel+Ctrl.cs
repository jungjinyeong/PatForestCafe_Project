using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class BusinessModel
{
    // 영업 중·마감 중(남은 손님 결제 포함)은 오늘 통계에, 영업 종료 중(이미 정산됨)은 다음 날 통계에 쌓는다.
    private bool IsSettled => mState.Value == eBusinessState.Closed;

    public void RecordBreadSale(int gold)
    {
        gold = Mathf.Max(0, gold);
        if (IsSettled) { mNextDayStats.BreadSold++; mNextDayStats.Gold += gold; return; }
        mTodayBreadSold.Value++;
        mTodayGold.Value += gold;
    }

    public void RecordDrinkSale(int gold)
    {
        gold = Mathf.Max(0, gold);
        if (IsSettled) { mNextDayStats.DrinkSold++; mNextDayStats.Gold += gold; return; }
        mTodayDrinkSold.Value++;
        mTodayGold.Value += gold;
    }

    public void RecordDelivery(int gold)
    {
        gold = Mathf.Max(0, gold);
        if (IsSettled) { mNextDayStats.Delivered++; mNextDayStats.Gold += gold; return; }
        mTodayDelivered.Value++;
        mTodayGold.Value += gold;
    }

    public void RecordVisitor()
    {
        if (IsSettled) { mNextDayStats.Visitors++; return; }
        mTodayVisitors.Value++;
    }

    public void RecordExp(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (IsSettled) { mNextDayStats.Exp += amount; return; }
        mTodayExp.Value += amount;
    }

    // 영업 중 → 마감 중. 영업 중이 아니면 무시.
    public bool RequestClose()
    {
        if (mState.Value != eBusinessState.Open) return false;

        mState.Value = eBusinessState.Closing;
        return true;
    }

    // 마감 중 → 영업 종료(남은 손님이 모두 나간 뒤).
    public bool CompleteClose()
    {
        if (mState.Value != eBusinessState.Closing) return false;

        mState.Value = eBusinessState.Closed;
        return true;
    }

    // 영업 종료 → 다음 날 영업 중. 일차 +1, 오늘 통계 = 영업 종료 중에 쌓인 다음 날 통계.
    public bool StartNextDay()
    {
        if (mState.Value != eBusinessState.Closed) return false;

        mDay.Value++;
        SetTodayStats(mNextDayStats);
        mNextDayStats = new BusinessStats();
        mState.Value = eBusinessState.Open;
        return true;
    }

    // 세이브 복원. 구 세이브(0)는 1일차로 보정한다. 영업 종료 상태였으면 종료 상태로 복원한다(로비가 정산 팝업을 다시 연다).
    public void Restore(int day, BusinessStats today, bool isClosed, BusinessStats nextDay)
    {
        mDay.Value = Mathf.Max(1, day);
        SetTodayStats(today);
        mNextDayStats = nextDay ?? new BusinessStats();
        mState.Value = isClosed ? eBusinessState.Closed : eBusinessState.Open;
    }

    public BusinessStats GetTodayStats()
    {
        return new BusinessStats
        {
            Gold = mTodayGold.Value,
            BreadSold = mTodayBreadSold.Value,
            DrinkSold = mTodayDrinkSold.Value,
            Delivered = mTodayDelivered.Value,
            Visitors = mTodayVisitors.Value,
            Exp = mTodayExp.Value,
        };
    }

    private void SetTodayStats(BusinessStats stats)
    {
        stats = stats ?? new BusinessStats();
        mTodayGold.Value = Mathf.Max(0, stats.Gold);
        mTodayBreadSold.Value = Mathf.Max(0, stats.BreadSold);
        mTodayDrinkSold.Value = Mathf.Max(0, stats.DrinkSold);
        mTodayDelivered.Value = Mathf.Max(0, stats.Delivered);
        mTodayVisitors.Value = Mathf.Max(0, stats.Visitors);
        mTodayExp.Value = Mathf.Max(0, stats.Exp);
    }

    #region 계절

    // Season.csv 행을 Tid 순서대로 돌린다(봄 → 여름 → 가을 → 겨울 → 봄 …). 한 계절은 GameTime.csv SeasonDays일.
    private List<CTable.SeasonRow> mSeasons;

    private List<CTable.SeasonRow> Seasons
    {
        get
        {
            if (mSeasons == null)
            {
                var table = GameInstance.Table?.GetTable<CTable.SeasonRow>();
                mSeasons = table != null ? table.All.Values.OrderBy(r => r.Tid).ToList() : new List<CTable.SeasonRow>();
            }
            return mSeasons;
        }
    }

    public CTable.SeasonRow CurrentSeason => GetSeason(mDay.Value);

    // 테이블이 비어 있으면 null(효과 배율 1, 이름 없이 일차만 표시).
    public CTable.SeasonRow GetSeason(int day)
    {
        if (Seasons.Count == 0) return null;
        return Seasons[(Mathf.Max(1, day) - 1) / SeasonDays % Seasons.Count];
    }

    public int GetDayInSeason(int day) => (Mathf.Max(1, day) - 1) % SeasonDays + 1;

    // "봄 3일" (계절 테이블이 없으면 "3일차")
    public string GetDateText(int day)
    {
        var season = GetSeason(day);
        return season != null ? $"{season.Name} {GetDayInSeason(day)}일" : $"{day}일차";
    }

    public bool IsSeasonFirstDay(int day) => GetDayInSeason(day) == 1;

    // 매장 결제(빵·음료) 가격 배율.
    public float GetSaleRate()
    {
        var season = CurrentSeason;
        return season != null && season.SaleRate > 0f ? season.SaleRate : 1f;
    }

    public int ApplySaleRate(int gold) => Mathf.RoundToInt(gold * GetSaleRate());

    // 손님 음료 추첨 가중치 배율(온도별 인기).
    public float GetDrinkPopularityRate(CTable.eDrinkTempType temp)
    {
        var season = CurrentSeason;
        if (season == null) return 1f;

        float rate = temp == CTable.eDrinkTempType.Ice ? season.IceDrinkRate : season.HotDrinkRate;
        return rate > 0f ? rate : 1f;
    }

    #endregion

    private void ResetTodayStats() => SetTodayStats(null);
}
