using UniRx;

// 임시: MoneyType 테이블 컬럼이 추가되기 전까지 ItemData 쪽에서 관리
public enum eMoneyType
{
    Gold
}

public class ItemData
{
    public CTable.ItemRow mRow { get; private set; }
    public int Tid { get { return mRow.Tid; } }
    
    public IReadOnlyReactiveProperty<int> Count => mCount;
    private readonly ReactiveProperty<int> mCount = new ReactiveProperty<int>(0);

    public static ItemData Create(CTable.ItemRow row, int amount = 0)
    {
        ItemData res = new ItemData();
        res.mRow = row;
        res.Set(amount);
        return res;
    }

    public virtual void Init()
    {

    }

    public virtual void Add(int amount)
    {
        mCount.Value += amount;
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
    public CTable.ItemRow mItemRow { get; private set; }

    public static WealthData Create(CTable.ItemRow itemRow)
    {
        var res = new WealthData();
        res.mItemRow = itemRow;
        return res;
    }
}
