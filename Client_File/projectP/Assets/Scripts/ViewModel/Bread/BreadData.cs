using CTable;
using Extension;
using UniRx;
using UnityEngine;

public class BreadData
{
    public int TId { get; private set; }

    public IReadOnlyReactiveProperty<int> Count => mCount;
    private readonly ReactiveProperty<int> mCount = new ReactiveProperty<int>(0);

    // 빵 공장에서 만들었지만 아직 진열대에 내놓지 않은 재고. Count(진열 수량)와는 별개로 관리한다.
    public IReadOnlyReactiveProperty<int> ProducedCount => mProducedCount;
    private readonly ReactiveProperty<int> mProducedCount = new ReactiveProperty<int>(0);

    public BreadRow Row { get; private set; }
    public MenuItemRow MenuItemRow { get; private set; }

    public static BreadData Create(MenuItemRow menuItemRow)
    {
        if(null == menuItemRow)
        {
            return null;
        }

        return new BreadData
        {
            Row = menuItemRow.GetBreadRow(),
            MenuItemRow = menuItemRow,
            TId = menuItemRow.Tid
        };
    }

    public static BreadData Create(BreadRow breadRow)
    {
        if(null == breadRow)
        {
            return null;
        }
        return new BreadData
        {
            Row = breadRow,
            MenuItemRow = GameInstance.Table.Get<MenuItemRow>(breadRow.Tid),
            TId = breadRow.Tid
        };
    }

    public void Add(int count = 1)
    {
        mCount.Value += count;
    }

    public void Consume(int count = 1)
    {
        mCount.Value = Mathf.Max(0, mCount.Value - count);
    }

    public void SetCount(int count)
    {
        mCount.Value = Mathf.Max(0, count);
    }

    public void SetProduced(int count)
    {
        mProducedCount.Value = Mathf.Max(0, count);
    }

    public void AddProduced(int count = 1)
    {
        mProducedCount.Value += count;
    }

    public bool TryConsumeProduced(int count = 1)
    {
        if (mProducedCount.Value < count)
            return false;

        mProducedCount.Value -= count;
        return true;
    }

    public void Dispose()
    {
        mCount.Dispose();
        mProducedCount.Dispose();
    }
}