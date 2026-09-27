using UniRx;

// 카페 레벨/경험치. 경험치 획득원: 매장 결제(건당 소량) · 둘기딜리버리 배달 완료 · (추후) 퀘스트 완료.
// 레벨별 필요 경험치/획득량 수치는 기획 확정 전까지 코드 내 고정값으로 관리한다(UpgradeModel과 동일한 임시 패턴).
public partial class CafeModel : IModelBase
{
    public const int MIN_LEVEL = 1;
    public const int MAX_LEVEL = 50;

    public const int EXP_STORE_PAYMENT = 2;
    public const int EXP_DELIVERY = 10;
    public const int EXP_QUEST = 20;

    public IReadOnlyReactiveProperty<int> Level => mLevel;
    private readonly ReactiveProperty<int> mLevel = new ReactiveProperty<int>(MIN_LEVEL);

    // 현재 레벨 안에서 쌓인 경험치(레벨업 시 필요 경험치만큼 차감).
    public IReadOnlyReactiveProperty<int> Exp => mExp;
    private readonly ReactiveProperty<int> mExp = new ReactiveProperty<int>(0);

    // 레벨업 알림(도달한 레벨).
    public System.IObservable<int> OnLevelUp => mOnLevelUp;
    private readonly Subject<int> mOnLevelUp = new Subject<int>();

    public void Init() { }

    public void Dispose()
    {
        mLevel.Value = MIN_LEVEL;
        mExp.Value = 0;
    }
}
