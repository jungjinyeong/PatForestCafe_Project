using System.Collections.Generic;
using UniRx;

public class BreadModel : IModelBase
{
    private readonly Dictionary<int, BreadData> mDicBreads = new();

    public void Init() { }

    public void Register(int tableId)
    {
        if (mDicBreads.ContainsKey(tableId))
            return;

        var bread = BreadData.Create(GameInstance.Table.Get<CTable.BreadRow>(tableId));
        if (bread == null)
        {
            Logger.Warning($"[BreadModel] 존재하지 않는 빵 Tid: {tableId}");
            return;
        }

        mDicBreads[tableId] = bread;
    }

    public IReadOnlyReactiveProperty<int> GetCount(int tableId)
    {
        return mDicBreads.TryGetValue(tableId, out var bread) ? bread.Count : null;
    }

    public BreadData Get(int tableId)
    {
        return mDicBreads.TryGetValue(tableId, out var bread) ? bread : null;
    }

    public IEnumerable<BreadData> GetAll() => mDicBreads.Values;

    public void SetByTid(int tableId, int count, int producedCount)
    {
        if (!mDicBreads.TryGetValue(tableId, out var bread))
            return;

        bread.SetCount(count);
        bread.SetProduced(producedCount);
    }

    public void Add(int tableId, int count = 1)
    {
        if (mDicBreads.TryGetValue(tableId, out var bread))
            bread.Add(count);
    }

    public void Consume(int tableId, int count = 1)
    {
        if (mDicBreads.TryGetValue(tableId, out var bread))
            bread.Consume(count);
    }

    public void AddProduced(int tableId, int count = 1)
    {
        if (mDicBreads.TryGetValue(tableId, out var bread))
            bread.AddProduced(count);
    }

    public bool TryConsumeProduced(int tableId, int count = 1)
    {
        return mDicBreads.TryGetValue(tableId, out var bread) && bread.TryConsumeProduced(count);
    }

    public void Dispose()
    {
        foreach (var bread in mDicBreads.Values)
            bread?.Dispose();
        mDicBreads.Clear();
    }
}
