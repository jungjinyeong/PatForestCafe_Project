using System;
using UniRx;

// 오븐 제작 상태: 오븐 레벨 + 트레이(최대 MAX_TRAY_COUNT칸) 반죽 설정/굽기 진행.
// 굽기가 끝난 트레이는 1초 틱에서 빵 재고(BreadModel 생산 재고, 품질별)로 자동 보관된다(오프라인 경과 포함).
public partial class OvenModel : IModelBase
{
    public const int MAX_TRAY_COUNT = 3;

    public IReadOnlyReactiveProperty<int> Level => mLevel;
    private readonly ReactiveProperty<int> mLevel = new ReactiveProperty<int>(1);

    public OvenTrayData[] Trays { get; } = CreateTrays();

    // 트레이 설정/굽기 시작/완료 등 트레이 상태가 바뀔 때마다 발행(UI 갱신용).
    public IObservable<Unit> OnTraysChanged => mOnTraysChanged;
    private readonly Subject<Unit> mOnTraysChanged = new Subject<Unit>();

    // 굽기 완료 → 창고 자동 보관 알림(레시피 Tid, 품질, 수량).
    public IObservable<(int recipeTid, eBreadQuality quality, int quantity)> OnBakeCompleted => mOnBakeCompleted;
    private readonly Subject<(int, eBreadQuality, int)> mOnBakeCompleted = new Subject<(int, eBreadQuality, int)>();

    private IDisposable mTickDisposable;

    public void Init()
    {
        // GameInstance.Init() 체인 안에서 호출되지만 첫 틱은 1초 뒤라 SaveManager.Load() 복원 이후에 완료 판정이 돈다.
        mTickDisposable = Observable.Interval(TimeSpan.FromSeconds(1))
            .Subscribe(_ => TickBake());
    }

    public void Dispose()
    {
        mTickDisposable?.Dispose();
        mTickDisposable = null;

        foreach (var tray in Trays)
            tray.Clear();

        mLevel.Value = 1;
    }

    private static OvenTrayData[] CreateTrays()
    {
        var trays = new OvenTrayData[MAX_TRAY_COUNT];
        for (int i = 0; i < trays.Length; i++)
            trays[i] = new OvenTrayData();
        return trays;
    }
}
