using UnityEngine;
using UniRx;

public class MaterialData
{
    public int Tid { get; private set; }
    public string Name { get; private set; }

    public IReadOnlyReactiveProperty<int> Count => mCount;
    private readonly ReactiveProperty<int> mCount = new ReactiveProperty<int>(0);

    public static MaterialData Create(int tid, string name)
    {
        return new MaterialData
        {
            Tid = tid,
            Name = name,
        };
    }

    public void Add(int amount)
    {
        mCount.Value += amount;
    }

    public void Consume(int amount)
    {
        mCount.Value = Mathf.Max(0, mCount.Value - amount);
    }

    public void Set(int amount)
    {
        mCount.Value = Mathf.Max(0, amount);
    }

    public void Dispose()
    {
        mCount.Dispose();
    }
}
