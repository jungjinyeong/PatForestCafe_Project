using System.Collections.Generic;
using UnityEngine;

public class CommonModelManager : MonoBehaviour, IManager
{
    public BreadModel Bread { get; private set; }
    public ItemModel Item { get; private set; }
    public DrinkModel Drink { get; private set; }
    public PlacementModel Placement { get; private set; }
    public RecipeBookModel RecipeBook { get; private set; }
    public UpgradeModel Upgrade { get; private set; }
    public MaterialModel Material { get; private set; }
    public WorkshopModel Workshop { get; private set; }

    private readonly List<IModelBase> mModels = new List<IModelBase>();

    public void Init()
    {
        Bread = Register(new BreadModel());
        Item  = Register(new ItemModel());
        Drink = Register(new DrinkModel());
        Placement = Register(new PlacementModel());
        RecipeBook = Register(new RecipeBookModel());
        Upgrade = Register(new UpgradeModel());
        Material = Register(new MaterialModel());
        Workshop = Register(new WorkshopModel());
    }

    public void Subscribe() { }

    public void Clear()
    {
        foreach (var model in mModels)
            model?.Dispose();
        mModels.Clear();
        Bread = null;
        Item  = null;
        Drink = null;
        Placement = null;
        RecipeBook = null;
        Upgrade = null;
        Material = null;
        Workshop = null;
    }

    public void Destory()
    {
        Clear();
    }

    private void OnDestroy()
    {
        Clear();
    }

    private T Register<T>(T model) where T : IModelBase
    {
        model.Init();
        mModels.Add(model);
        return model;
    }
}
