using System.Collections.Generic;
using UniRx;
using UnityEngine;

public partial class ItemModel : IModelBase
{
    Dictionary<int, ItemData> mDicItems = new();
    Dictionary<eMoneyType, WealthData> mDicWealths = new();

    CompositeDisposable mDisposables = new CompositeDisposable();

    public void Init()
    {
        var group = GameInstance.Table.GetGroup<CTable.ItemRow>();
        if (group == null)
        {
            Debug.LogWarning("[ItemModel] ItemGroup을 찾을 수 없습니다.");
            return;
        }

        // TODO : 아이템 저장을 하기 시작하면 다 만들 필요없음.
        foreach (var row in group.All.Values)
        {
            if (row.ItemType == CTable.eItemType.Money)
            {
                var wealth = WealthData.Create(row);
                mDicWealths[(eMoneyType)row.Tid] = wealth;
                continue;
            }

            var item = ItemData.Create(row);
            mDicItems[row.Tid] = item;
        }
    }

    #region Wealth

    public WealthData GetWealth(eMoneyType moneyType)
    {
        return mDicWealths.TryGetValue(moneyType, out var wealth) ? wealth : null;
    }

    #endregion

    public ItemData Get(int tid)
    {
        return mDicItems.TryGetValue(tid, out var item) ? item : null;
    }

    public IEnumerable<ItemData> GetAll() => mDicItems.Values;

    public void Add(int tid, int amount = 1)
    {
        if (mDicItems.TryGetValue(tid, out var item))
            item.Add(amount);
        else
            Debug.LogWarning($"[ItemModel] 존재하지 않는 아이템 Tid: {tid}");
    }

    public void Consume(int tid, int amount = 1)
    {
        if (mDicItems.TryGetValue(tid, out var item))
            item.Consume(amount);
        else
            Debug.LogWarning($"[ItemModel] 존재하지 않는 아이템 Tid: {tid}");
    }

    public void Set(int tid, int amount)
    {
        if (mDicItems.TryGetValue(tid, out var item))
            item.Set(amount);
        else
            Debug.LogWarning($"[ItemModel] 존재하지 않는 아이템 Tid: {tid}");
    }

    public bool HasEnough(int tid, int amount)
    {
        var item = Get(tid);
        return item != null && item.Count.Value >= amount;
    }

    public void Dispose()
    {
        mDisposables.Dispose();
        
        foreach (var item in mDicItems.Values)
            item.Dispose();
        mDicItems.Clear();

        mDicWealths.Clear();
    }
}
