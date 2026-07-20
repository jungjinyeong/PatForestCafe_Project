using System.Collections.Generic;
using UnityEngine;

public class DrinkModel : IModelBase
{
    private readonly Dictionary<int, DrinkData> mDicDrinks = new();

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
    }

    public DrinkData Get(int tableId)
    {
        return mDicDrinks.TryGetValue(tableId, out var drink) ? drink : null;
    }

    public int GetRandomSpecialOrderTid()
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
        DefaultDrink = null;
    }
}
