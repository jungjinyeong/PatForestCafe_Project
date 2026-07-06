using System.Collections.Generic;
using UnityEngine;

public class ItemModel : IModelBase
{
    private readonly Dictionary<int, ItemData> mItems = new();

    public void Init()
    {
        var group = GameInstance.Table.GetGroup<CTable.ItemRow>();
        if (group == null)
        {
            Debug.LogWarning("[ItemModel] ItemGroup을 찾을 수 없습니다.");
            return;
        }

        foreach (var row in group.All.Values)
            mItems[row.Tid] = new ItemData(row);
    }

    public ItemData Get(int tid)
    {
        return mItems.TryGetValue(tid, out var item) ? item : null;
    }

    public IEnumerable<ItemData> GetAll() => mItems.Values;

    public void Add(int tid, int amount = 1)
    {
        if (mItems.TryGetValue(tid, out var item))
            item.Add(amount);
        else
            Debug.LogWarning($"[ItemModel] 존재하지 않는 아이템 Tid: {tid}");
    }

    public void Consume(int tid, int amount = 1)
    {
        if (mItems.TryGetValue(tid, out var item))
            item.Consume(amount);
        else
            Debug.LogWarning($"[ItemModel] 존재하지 않는 아이템 Tid: {tid}");
    }

    public void Set(int tid, int amount)
    {
        if (mItems.TryGetValue(tid, out var item))
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
        foreach (var item in mItems.Values)
            item.Dispose();
        mItems.Clear();
    }
}
