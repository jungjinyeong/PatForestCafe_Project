using System.Collections.Generic;
using UnityEngine;

public class CommonModelManager : MonoBehaviour, IManager
{
    public BreadModel Bread { get; private set; }
    public ItemModel Item { get; private set; }

    private readonly List<IModelBase> _models = new List<IModelBase>();

    public void Init()
    {
        Bread = Register(new BreadModel());
        Item  = Register(new ItemModel());
    }

    public void Subscribe() { }

    public void Clear()
    {
        foreach (var model in _models)
            model?.Dispose();
        _models.Clear();
        Bread = null;
        Item  = null;
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
        _models.Add(model);
        return model;
    }
}
