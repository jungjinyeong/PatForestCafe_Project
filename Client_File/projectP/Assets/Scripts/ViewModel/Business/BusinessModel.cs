using System;
using UniRx;

public enum eBusinessState
{
    Open,       // 영업 중 — 손님 입장
    Closing,    // 마감 중 — 새 손님 입장 중단, 남은 손님 퇴장 대기
    Closed,     // 영업 종료 — 정산 완료, [다음 날 영업 시작] 대기
}

// 영업 종료 중(Closed)에 생긴 통계(배달 매출·경험치 등)를 다음 날로 넘기기 위한 묶음. 세이브에도 그대로 쓴다.
[Serializable]
public class BusinessStats
{
    public int Gold;
    public int BreadSold;
    public int DrinkSold;
    public int Delivered;
    public int Visitors;
    public int Exp;
}

// 하루 영업 상태 + 일차 + 오늘 통계(정산 팝업용).
// 하루 길이·개점/마감 시각·마감 대기 제한은 GameTime.csv(CTable.GameTimeRow, Tid 1)에서 읽는다.
// 영업 종료(Closed)는 세이브한다 — 정산만 보고 종료해도 재실행 시 같은 날을 다시 영업하지 않는다.
// 마감 중(Closing)은 세이브하지 않는다 — 재실행하면 같은 날 영업 중으로 이어진다(정산 전이라 통계 중복 없음).
public partial class BusinessModel : IModelBase
{
    public const int GAME_TIME_TID = 1;

    // 테이블이 없거나 값이 비었을 때의 기본값.
    private const float DEFAULT_DAY_DURATION_SECONDS = 120f;
    private const int DEFAULT_OPEN_HOUR = 8;
    private const int DEFAULT_CLOSE_HOUR = 22;
    private const float DEFAULT_CLOSING_TIMEOUT_SECONDS = 60f;
    private const int DEFAULT_SEASON_DAYS = 28;

    public static CTable.GameTimeRow GameTimeRow => GameInstance.Table?.Get<CTable.GameTimeRow>(GAME_TIME_TID);

    public static float DayDurationSeconds => GameTimeRow != null && GameTimeRow.DayDurationSeconds > 0f ? GameTimeRow.DayDurationSeconds : DEFAULT_DAY_DURATION_SECONDS;
    public static int OpenHour => GameTimeRow != null ? UnityEngine.Mathf.Clamp(GameTimeRow.OpenHour, 0, 23) : DEFAULT_OPEN_HOUR;
    public static int CloseHour => GameTimeRow != null && GameTimeRow.CloseHour > 0 ? UnityEngine.Mathf.Clamp(GameTimeRow.CloseHour, 1, 24) : DEFAULT_CLOSE_HOUR;
    public static float ClosingTimeoutSeconds => GameTimeRow != null && GameTimeRow.ClosingTimeoutSeconds > 0f ? GameTimeRow.ClosingTimeoutSeconds : DEFAULT_CLOSING_TIMEOUT_SECONDS;
    public static int SeasonDays => GameTimeRow != null && GameTimeRow.SeasonDays > 0 ? GameTimeRow.SeasonDays : DEFAULT_SEASON_DAYS;

    public IReadOnlyReactiveProperty<eBusinessState> State => mState;
    private readonly ReactiveProperty<eBusinessState> mState = new ReactiveProperty<eBusinessState>(eBusinessState.Open);

    public IReadOnlyReactiveProperty<int> Day => mDay;
    private readonly ReactiveProperty<int> mDay = new ReactiveProperty<int>(1);

    public IReadOnlyReactiveProperty<int> TodayGold => mTodayGold;
    private readonly ReactiveProperty<int> mTodayGold = new ReactiveProperty<int>(0);

    public IReadOnlyReactiveProperty<int> TodayBreadSold => mTodayBreadSold;
    private readonly ReactiveProperty<int> mTodayBreadSold = new ReactiveProperty<int>(0);

    public IReadOnlyReactiveProperty<int> TodayDrinkSold => mTodayDrinkSold;
    private readonly ReactiveProperty<int> mTodayDrinkSold = new ReactiveProperty<int>(0);

    public IReadOnlyReactiveProperty<int> TodayDelivered => mTodayDelivered;
    private readonly ReactiveProperty<int> mTodayDelivered = new ReactiveProperty<int>(0);

    public IReadOnlyReactiveProperty<int> TodayVisitors => mTodayVisitors;
    private readonly ReactiveProperty<int> mTodayVisitors = new ReactiveProperty<int>(0);

    public IReadOnlyReactiveProperty<int> TodayExp => mTodayExp;
    private readonly ReactiveProperty<int> mTodayExp = new ReactiveProperty<int>(0);

    // 영업 종료 중에 쌓인 통계. [다음 날 영업 시작] 때 오늘 통계로 옮긴다(이미 끝난 정산에 섞이거나 사라지지 않도록).
    public BusinessStats NextDayStats => mNextDayStats;
    private BusinessStats mNextDayStats = new BusinessStats();

    public void Init() { }

    public void Dispose()
    {
        mState.Value = eBusinessState.Open;
        mDay.Value = 1;
        ResetTodayStats();
        mNextDayStats = new BusinessStats();
    }
}
