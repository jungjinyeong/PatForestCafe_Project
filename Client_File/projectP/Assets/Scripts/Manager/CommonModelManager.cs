using System.Collections.Generic;
using UnityEngine;

public class CommonModelManager : MonoBehaviour, IManager
{
    public BreadModel Bread { get; private set; }
    public ItemModel Item { get; private set; }
    public DrinkModel Drink { get; private set; }
    public PlacementModel Placement { get; private set; }

    private readonly List<IModelBase> mModels = new List<IModelBase>();

    public void Init()
    {
        Bread = Register(new BreadModel());
        Item  = Register(new ItemModel());
        Drink = Register(new DrinkModel());
        Placement = Register(new PlacementModel());
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
