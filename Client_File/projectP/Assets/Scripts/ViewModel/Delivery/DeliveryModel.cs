using System;
using UniRx;

// 둘기딜리버리 주문서(최대 MAX_ORDER_COUNT장)와 커피머신 완성 음료 픽업대(PICKUP_SLOT_COUNT칸) 상태.
public partial class DeliveryModel : IModelBase
{
    public const int MAX_ORDER_COUNT = 3;
    public const int PICKUP_SLOT_COUNT = 3;

    public ReactiveCollection<DeliveryOrderData> Orders { get; } = new ReactiveCollection<DeliveryOrderData>();

    // null = 빈 칸.
    public ReactiveProperty<PickupDrinkData>[] PickupSlots { get; } = CreatePickupSlots();

    // 이 시각(Unix 초) 이후 빈 주문 자리를 채운다. 0이면 즉시.
    public long NextRefillUnixSeconds { get; private set; }
    public int NextOrderNo { get; private set; } = 1;

    private IDisposable mRefillDisposable;

    public void Init()
    {
        // GameInstance.Init() 체인 안에서 호출되지만, 첫 틱은 1초 뒤라 SaveManager.Load()로
        // 도감/주문 복원이 끝난 뒤에 보충이 시작된다.
        mRefillDisposable = Observable.Interval(TimeSpan.FromSeconds(1))
            .Subscribe(_ => TickRefill());
    }

    public void Dispose()
    {
        mRefillDisposable?.Dispose();
        mRefillDisposable = null;

        Orders.Clear();
        foreach (var slot in PickupSlots)
            slot.Value = null;
    }

    private static ReactiveProperty<PickupDrinkData>[] CreatePickupSlots()
    {
        var slots = new ReactiveProperty<PickupDrinkData>[PICKUP_SLOT_COUNT];
        for (int i = 0; i < slots.Length; i++)
            slots[i] = new ReactiveProperty<PickupDrinkData>(null);
        return slots;
    }
}
