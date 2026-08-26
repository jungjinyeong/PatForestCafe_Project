using System.Collections.Generic;
using UniRx;

public class BreadModel : IModelBase
{
    private readonly Dictionary<int, BreadData> mDicBreads = new();

    // 예전엔 진열대(Intaraction_BreadStand)가 자기 Tid를 Register()해줘야만 항목이 생겼다 — 모든 빵 종류에 반드시
    // 진열대가 하나씩 있던 시절엔 문제없었지만, 진열대가 배치 후 임의 종류로 배정되는 지금은 "아직 아무 진열대도
    // 맡지 않은 빵 종류"가 있을 수 있다. 그 상태에서 빵 공장(UIPopupBreadProduction)이 AddProduced()를 호출하면
    // 항목이 없어 조용히 무시되고 생산량이 영구히 유실된다 — DrinkModel/ItemModel처럼 CTable을 즉시 전부 로드해 막는다.
    public void Init()
    {
        var group = GameInstance.Table.GetTable<CTable.BreadRow>();
        if (group == null)
        {
            Logger.Warning("[BreadModel] BreadGroup을 찾을 수 없습니다.");
            return;
        }

        foreach (var row in group.All.Values)
            Register(row.Tid);
    }

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
