using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class BreadModel : IModelBase
{
    private readonly Dictionary<int, ReactiveProperty<int>> mTableBreads = new();

    public void Init() { }

    public void Register(int tableId)
    {
        if (!mTableBreads.ContainsKey(tableId))
            mTableBreads[tableId] = new ReactiveProperty<int>(0);
    }

    public IReadOnlyReactiveProperty<int> GetCount(int tableId)
    {
        return mTableBreads.TryGetValue(tableId, out var prop) ? prop : null;
    }

    public void Add(int tableId, int count = 1)
    {
        if (mTableBreads.TryGetValue(tableId, out var prop))
            prop.Value += count;
    }

    public void Consume(int tableId, int count = 1)
    {
        if (mTableBreads.TryGetValue(tableId, out var prop))
            prop.Value = Mathf.Max(0, prop.Value - count);
    }

    public void Dispose()
    {
        foreach (var prop in mTableBreads.Values)
            prop?.Dispose();
        mTableBreads.Clear();
    }
}
