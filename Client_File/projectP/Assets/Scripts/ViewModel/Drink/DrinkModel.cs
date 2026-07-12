using System.Collections.Generic;
using UnityEngine;

public class DrinkModel : IModelBase
{
    private readonly Dictionary<int, DrinkData> mDicDrinks = new();

    public DrinkData DefaultDrink { get; private set; }

    public void Init()
    {
        var group = GameInstance.Table.GetGroup<CTable.DrinkRow>();
        if (group == null)
        {
            Debug.LogWarning("[DrinkModel] DrinkGroup을 찾을 수 없습니다.");
            return;
        }

        foreach (var row in group.All.Values)
            mDicDrinks[row.Tid] = DrinkData.Create(row);

        int defaultDrinkTid = GameInstance.Config.GetValue(eConfigType.DefaultDrinkTid);
        DefaultDrink = Get(defaultDrinkTid);
        if (DefaultDrink == null)
            Debug.LogWarning($"[DrinkModel] 기본 Drink Tid({defaultDrinkTid})를 찾을 수 없습니다.");
    }

    public DrinkData Get(int tableId)
    {
        return mDicDrinks.TryGetValue(tableId, out var drink) ? drink : null;
    }

    public void Dispose()
    {
        mDicDrinks.Clear();
        DefaultDrink = null;
    }
}
