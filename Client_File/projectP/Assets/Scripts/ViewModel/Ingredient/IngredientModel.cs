using System.Collections.Generic;

public class IngredientModel : IModelBase
{
    private readonly Dictionary<int, IngredientData> mDicIngredients = new();

    public void Init()
    {
        var drinkIngredientGroup = GameInstance.Table.GetTable<CTable.DrinkIngredientRow>();
        if (drinkIngredientGroup == null)
            Logger.Warning("[IngredientModel] DrinkIngredientGroup을 찾을 수 없습니다.");
        else
            foreach (var row in drinkIngredientGroup.All.Values)
                mDicIngredients[row.Tid] = IngredientData.Create(row.Tid, row.Name);

        var breadIngredientGroup = GameInstance.Table.GetTable<CTable.BreadIngredientRow>();
        if (breadIngredientGroup == null)
            Logger.Warning("[IngredientModel] BreadIngredientGroup을 찾을 수 없습니다.");
        else
            foreach (var row in breadIngredientGroup.All.Values)
                mDicIngredients[row.Tid] = IngredientData.Create(row.Tid, row.Name);
    }

    public IngredientData Get(int tid)
    {
        return mDicIngredients.TryGetValue(tid, out var ingredient) ? ingredient : null;
    }

    public IEnumerable<IngredientData> GetAll() => mDicIngredients.Values;

    public void Gather(int tid, int amount = 1)
    {
        Get(tid)?.Add(amount);
    }

    public bool HasEnough(int tid, int amount)
    {
        var ingredient = Get(tid);
        return ingredient != null && ingredient.Count.Value >= amount;
    }

    public void Consume(int tid, int amount)
    {
        Get(tid)?.Consume(amount);
    }

    public void SetByTid(int tid, int amount)
    {
        Get(tid)?.Set(amount);
    }

    public void Dispose()
    {
        foreach (var ingredient in mDicIngredients.Values)
            ingredient.Dispose();
        mDicIngredients.Clear();
    }
}
