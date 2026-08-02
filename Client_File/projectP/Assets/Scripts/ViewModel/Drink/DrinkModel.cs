using System.Collections.Generic;
using UnityEngine;

public class DrinkModel : IModelBase
{
    private readonly Dictionary<int, DrinkData> mDicDrinks = new();
    private readonly List<CTable.DrinkRequestRow> mDrinkRequests = new();
    private int mDrinkRequestTotalWeight;

    public DrinkData DefaultDrink { get; private set; }

    public void Init()
    {
        var group = GameInstance.Table.GetTable<CTable.DrinkRow>();
        if (group == null)
        {
            Logger.Warning("[DrinkModel] DrinkGroup을 찾을 수 없습니다.");
            return;
        }

        foreach (var row in group.All.Values)
            mDicDrinks[row.Tid] = DrinkData.Create(row);

        int defaultDrinkTid = GameInstance.Config.GetValue(eConfigType.DefaultDrinkTid);
        DefaultDrink = Get(defaultDrinkTid);
        if (DefaultDrink == null)
            Logger.Warning($"[DrinkModel] 기본 Drink Tid({defaultDrinkTid})를 찾을 수 없습니다.");

        LoadDrinkRequests();
    }

    public DrinkData Get(int tableId)
    {
        return mDicDrinks.TryGetValue(tableId, out var drink) ? drink : null;
    }

    // 요청 테이블(DrinkRequest)에 등록된 가중치 기반으로 특수 주문 음료를 뽑는다.
    // 요청 테이블이 비어 있으면 기본 음료를 제외한 전체 음료 중 균등 랜덤으로 대체한다.
    public int GetRandomSpecialOrderTid()
    {
        if (mDrinkRequests.Count > 0)
            return GetWeightedRandomRequestTid();

        return GetRandomNonDefaultDrinkTid();
    }

    private void LoadDrinkRequests()
    {
        var group = GameInstance.Table.GetTable<CTable.DrinkRequestRow>();
        if (group == null)
        {
            Logger.Warning("[DrinkModel] DrinkRequestGroup을 찾을 수 없습니다.");
            return;
        }

        foreach (var row in group.All.Values)
        {
            if (row.Weight <= 0 || !mDicDrinks.ContainsKey(row.DrinkTid))
                continue;

            mDrinkRequests.Add(row);
            mDrinkRequestTotalWeight += row.Weight;
        }
    }

    private int GetWeightedRandomRequestTid()
    {
        int roll = UnityEngine.Random.Range(0, mDrinkRequestTotalWeight);
        int cumulative = 0;

        foreach (var row in mDrinkRequests)
        {
            cumulative += row.Weight;
            if (roll < cumulative)
                return row.DrinkTid;
        }

        return mDrinkRequests[mDrinkRequests.Count - 1].DrinkTid;
    }

    private int GetRandomNonDefaultDrinkTid()
    {
        var candidates = new List<int>();
        foreach (var tid in mDicDrinks.Keys)
        {
            if (DefaultDrink != null && tid == DefaultDrink.TId)
                continue;

            candidates.Add(tid);
        }

        if (candidates.Count == 0)
            return -1;

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    public void Dispose()
    {
        mDicDrinks.Clear();
        mDrinkRequests.Clear();
        mDrinkRequestTotalWeight = 0;
        DefaultDrink = null;
    }
}
