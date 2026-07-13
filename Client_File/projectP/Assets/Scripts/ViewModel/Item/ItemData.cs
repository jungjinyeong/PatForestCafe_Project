using CTable;
using UniRx;

public class ItemData
{
    public CTable.ItemRow mRow { get; private set; }
    public int Tid { get { return mRow.Tid; } }
    
    public IReadOnlyReactiveProperty<int> Count => mCount;
    private readonly ReactiveProperty<int> mCount = new ReactiveProperty<int>(0);

    public static ItemData Create(CTable.ItemRow row, int amount = 0)
    {
        ItemData res = new ItemData();
        res.Init(row, amount);
        return res;
    }

    public virtual void Init(ItemRow row, int amount = 0)
    {
        mRow = row;
        Set(amount);
    }

    public virtual void Add(int amount)
    {
        mCount.Value += amount;
        Logger.Log($"[ItemData] Add Tid: {Tid}, Amount: {amount}, New Count: {mCount.Value}");
    }

    public virtual void Set(int amount)
    {
        mCount.Value = UnityEngine.Mathf.Max(0, amount);
    }

    public virtual void Consume(int amount)
    {
        mCount.Value = UnityEngine.Mathf.Max(0, mCount.Value - amount);
    }

    public virtual void Dispose()
    {
        mCount.Dispose();
    }
}

public class WealthData : ItemData
{
    public CTable.ItemMoneyRow mMoneyRow { get; private set; }

    public CTable.eMoneyType MoneyType => mMoneyRow.MoneyType;

    public static WealthData CreateWealthData(CTable.ItemMoneyRow moneyRow, int amount = 0)
    {
        var res = new WealthData();
        res.Init(GameInstance.Table.Get<ItemRow>(moneyRow.Tid), amount);
        res.mMoneyRow = moneyRow;
        return res;
    }
}
