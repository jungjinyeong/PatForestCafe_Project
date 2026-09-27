using System;
using UniRx;

public enum eNoticeType
{
    Oven,
    Delivery,
    CafeLevel,
    BreadStock,
    Business,
}

public class NoticeData
{
    public eNoticeType Type;
    public string Message;
    public long UnixSeconds;
}

// 로비 알림 패널용 최근 알림 목록(최신이 0번). 오븐 완료·배달 주문 접수·카페 레벨업·진열 재고 소진을 모은다.
// 세이브하지 않는 휘발성 데이터다.
public partial class NoticeModel : IModelBase
{
    public const int MAX_NOTICE_COUNT = 20;

    public IReadOnlyReactiveCollection<NoticeData> Notices => mNotices;
    private readonly ReactiveCollection<NoticeData> mNotices = new ReactiveCollection<NoticeData>();

    private readonly CompositeDisposable mSourceDisposables = new CompositeDisposable();

    public void Init() { }

    public void Dispose()
    {
        mSourceDisposables.Dispose();
        mNotices.Clear();
    }
}
