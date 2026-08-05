using System.Collections.Generic;

public class MaterialModel : IModelBase
{
    private readonly Dictionary<int, MaterialData> mDicMaterials = new();

    public void Init()
    {
        var drinkMaterialGroup = GameInstance.Table.GetTable<CTable.DrinkMaterialRow>();
        if (drinkMaterialGroup == null)
            Logger.Warning("[MaterialModel] DrinkMaterialGroup을 찾을 수 없습니다.");
        else
            foreach (var row in drinkMaterialGroup.All.Values)
                mDicMaterials[row.Tid] = MaterialData.Create(row.Tid, row.Name);

        var breadMaterialGroup = GameInstance.Table.GetTable<CTable.BreadMaterialRow>();
        if (breadMaterialGroup == null)
            Logger.Warning("[MaterialModel] BreadMaterialGroup을 찾을 수 없습니다.");
        else
            foreach (var row in breadMaterialGroup.All.Values)
                mDicMaterials[row.Tid] = MaterialData.Create(row.Tid, row.Name);
    }

    public MaterialData Get(int tid)
    {
        return mDicMaterials.TryGetValue(tid, out var material) ? material : null;
    }

    public IEnumerable<MaterialData> GetAll() => mDicMaterials.Values;

    public void Gather(int tid, int amount = 1)
    {
        Get(tid)?.Add(amount);
    }

    public bool HasEnough(int tid, int amount)
    {
        var material = Get(tid);
        return material != null && material.Count.Value >= amount;
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
        foreach (var material in mDicMaterials.Values)
            material.Dispose();
        mDicMaterials.Clear();
    }
}
