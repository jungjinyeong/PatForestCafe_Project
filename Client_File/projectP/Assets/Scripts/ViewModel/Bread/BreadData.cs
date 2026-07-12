using CTable;
using Extension;
using UniRx;
using UnityEngine;

public class BreadData
{
    public int TId { get; private set; }

    public IReadOnlyReactiveProperty<int> Count => mCount;
    private readonly ReactiveProperty<int> mCount = new ReactiveProperty<int>(0);

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

    public void Dispose()
    {
        mCount.Dispose();
    }
}