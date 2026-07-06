using UniRx;

public class ItemData
{
    public int Tid { get; }
    public CTable.eItemType ItemType { get; }
    public string ItemName { get; }
    public string Atlas { get; }
    public string Icon { get; }

    public IReadOnlyReactiveProperty<int> Count => mCount;
    private readonly ReactiveProperty<int> mCount = new ReactiveProperty<int>(0);

    public ItemData(CTable.ItemRow row)
    {
        Tid      = row.Tid;
        ItemType = row.ItemType;
        ItemName = row.ItemName;
        Atlas    = row.Atlas;
        Icon     = row.Icon;
    }

    internal void Add(int amount)            => mCount.Value += amount;
    internal void Set(int amount)            => mCount.Value = UnityEngine.Mathf.Max(0, amount);
    internal void Consume(int amount)        => mCount.Value = UnityEngine.Mathf.Max(0, mCount.Value - amount);
    internal void Dispose()                  => mCount.Dispose();
}
