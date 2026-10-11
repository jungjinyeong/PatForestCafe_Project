using UniRx;

public class WorkshopSlotData
{
    public int SlotIndex;

    // 0 = 배치된 일꾼 없음(빈 슬롯)
    public readonly ReactiveProperty<int> IngredientTid = new ReactiveProperty<int>(0);

    public void Dispose()
    {
        IngredientTid.Dispose();
    }
}
