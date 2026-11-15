using System.Collections.Generic;
using UniRx;

public class DessertModel : IModelBase
{
    private readonly Dictionary<int, DessertData> mDicDesserts = new();

    // 예전엔 진열대(Intaraction_DessertStand)가 자기 Tid를 Register()해줘야만 항목이 생겼다 — 모든 빵 종류에 반드시
    // 진열대가 하나씩 있던 시절엔 문제없었지만, 진열대가 배치 후 임의 종류로 배정되는 지금은 "아직 아무 진열대도
    // 맡지 않은 빵 종류"가 있을 수 있다. 그 상태에서 오븐(OvenModel)이 AddProduced()를 호출하면
    // 항목이 없어 조용히 무시되고 생산량이 영구히 유실된다 — DrinkModel/ItemModel처럼 CTable을 즉시 전부 로드해 막는다.
    public void Init()
    {
        var group = GameInstance.Table.GetTable<CTable.DessertRow>();
        if (group == null)
        {
            Logger.Warning("[DessertModel] DessertGroup을 찾을 수 없습니다.");
            return;
        }

        foreach (var row in group.All.Values)
            Register(row.Tid);
    }

    public void Register(int tableId)
    {
        if (mDicDesserts.ContainsKey(tableId))
            return;

        var dessert = DessertData.Create(GameInstance.Table.Get<CTable.DessertRow>(tableId));
        if (dessert == null)
        {
            Logger.Warning($"[DessertModel] 존재하지 않는 빵 Tid: {tableId}");
            return;
        }

        mDicDesserts[tableId] = dessert;
    }

    public IReadOnlyReactiveProperty<int> GetCount(int tableId)
    {
        return mDicDesserts.TryGetValue(tableId, out var dessert) ? dessert.Count : null;
    }

    public DessertData Get(int tableId)
    {
        return mDicDesserts.TryGetValue(tableId, out var dessert) ? dessert : null;
    }

    public IEnumerable<DessertData> GetAll() => mDicDesserts.Values;

    // 세이브 복원 — 품질별 진열/생산 수량을 "더한다". 빵 세이브는 진열대 등록 뒤(ApplyPendingDessertData)에 늦게 적용되는데,
    // 그 사이 오븐(OvenModel 1초 틱)이 오프라인 동안 끝난 트레이를 먼저 보관할 수 있어 덮어쓰면 그 빵이 사라진다.
    // 복원 전 값은 0(또는 그 오븐 완료분)뿐이므로 더하기가 곧 정확한 복원이다.
    public void RestoreFromSave(int tableId, int[] countByQuality, int[] producedByQuality)
    {
        if (!mDicDesserts.TryGetValue(tableId, out var dessert))
            return;

        for (int i = 0; i < DessertQuality.COUNT; i++)
        {
            var quality = (eDessertQuality)(i + 1);
            dessert.Add(quality, countByQuality != null && i < countByQuality.Length ? countByQuality[i] : 0);
            dessert.AddProduced(quality, producedByQuality != null && i < producedByQuality.Length ? producedByQuality[i] : 0);
        }
    }

    public void Add(int tableId, eDessertQuality quality, int count = 1)
    {
        if (mDicDesserts.TryGetValue(tableId, out var dessert))
            dessert.Add(quality, count);
    }

    public void Consume(int tableId, eDessertQuality quality, int count = 1)
    {
        if (mDicDesserts.TryGetValue(tableId, out var dessert))
            dessert.Consume(quality, count);
    }

    public void AddProduced(int tableId, eDessertQuality quality, int count = 1)
    {
        if (mDicDesserts.TryGetValue(tableId, out var dessert))
            dessert.AddProduced(quality, count);
    }

    public bool TryConsumeProducedBest(int tableId, out eDessertQuality quality)
    {
        quality = eDessertQuality.Low;
        return mDicDesserts.TryGetValue(tableId, out var dessert) && dessert.TryConsumeProducedBest(out quality);
    }

    public void Dispose()
    {
        foreach (var dessert in mDicDesserts.Values)
            dessert?.Dispose();
        mDicDesserts.Clear();
    }
}
