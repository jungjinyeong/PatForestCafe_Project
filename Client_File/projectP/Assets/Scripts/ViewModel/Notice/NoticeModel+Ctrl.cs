using System;
using UniRx;

public partial class NoticeModel
{
    public void Push(eNoticeType type, string message)
    {
        mNotices.Insert(0, new NoticeData
        {
            Type = type,
            Message = message,
            UnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        });

        while (mNotices.Count > MAX_NOTICE_COUNT)
            mNotices.RemoveAt(mNotices.Count - 1);
    }

    // 알림 소스 구독. 세이브 복원(배달 주문·진열 수량)이 알림으로 쏟아지지 않도록 로비 초기화가 끝난 뒤
    // (GameModeLobby+FSM.OnEnterOpenLobbyUI) 호출한다. 다시 불려도 기존 구독을 지우고 새로 건다.
    public void Bind()
    {
        mSourceDisposables.Clear();

        var model = GameInstance.Model;

        model.Oven.OnBakeCompleted
            .Subscribe(e => Push(eNoticeType.Oven, $"오븐 {GetMenuName(e.recipeTid)} ×{e.quantity} 굽기 완료"))
            .AddTo(mSourceDisposables);

        model.Delivery.Orders.ObserveAdd()
            .Subscribe(e => Push(eNoticeType.Delivery, $"둘기딜리버리 주문 #{e.Value.OrderNo} 접수 · {GetMenuName(e.Value.DrinkTid)}"))
            .AddTo(mSourceDisposables);

        model.Cafe.OnLevelUp
            .Subscribe(level => Push(eNoticeType.CafeLevel, $"카페 레벨 {level} 달성!"))
            .AddTo(mSourceDisposables);

        model.Business.State
            .Skip(1)
            .Subscribe(state => Push(eNoticeType.Business, GetBusinessMessage(state)))
            .AddTo(mSourceDisposables);

        // 다음 날로 넘어가며 새 계절 1일이 되면 계절 시작 알림(영업 시작 알림보다 먼저 쌓여 목록에선 그 아래).
        model.Business.Day
            .Skip(1)
            .Where(day => model.Business.IsSeasonFirstDay(day))
            .Subscribe(day => Push(eNoticeType.Business, GetSeasonMessage(model.Business.GetSeason(day))))
            .AddTo(mSourceDisposables);

        // 진열 수량이 있다가 0이 되면(마지막 빵이 팔리면) 재고 부족 알림.
        foreach (var bread in model.Bread.GetAll())
        {
            int tid = bread.TId;
            bread.Count.Pairwise()
                .Where(p => p.Previous > 0 && p.Current == 0)
                .Subscribe(_ => Push(eNoticeType.BreadStock, $"빵 진열대 재고 부족 · {GetMenuName(tid)}"))
                .AddTo(mSourceDisposables);
        }
    }

    private static string GetBusinessMessage(eBusinessState state)
    {
        var business = GameInstance.Model.Business;
        switch (state)
        {
            case eBusinessState.Closing: return "영업 마감 시작 · 남은 손님이 나가면 정산해요";
            case eBusinessState.Closed: return $"{business.GetDateText(business.Day.Value)} 영업 종료 · 매출 {business.TodayGold.Value:N0}G";
            default: return $"{business.GetDateText(business.Day.Value)} 영업 시작";
        }
    }

    private static string GetSeasonMessage(CTable.SeasonRow season)
    {
        if (season == null) return "새 계절이 시작됐어요";

        string hint = season.IceDrinkRate > season.HotDrinkRate ? " · ICE 음료 인기"
            : season.HotDrinkRate > season.IceDrinkRate ? " · HOT 음료 인기" : string.Empty;
        return $"{season.Name}이 시작됐어요{hint}";
    }

    private static string GetMenuName(int tid)
    {
        return GameInstance.Table.Get<CTable.MenuItemRow>(tid)?.Name ?? tid.ToString();
    }
}
