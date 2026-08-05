using System.Collections.Generic;

public class RecipeBookModel : IModelBase
{
    private readonly HashSet<int> mDiscoveredDrinkTids = new();

    public void Init() { }

    public bool Discover(int drinkTid)
    {
        return mDiscoveredDrinkTids.Add(drinkTid);
    }

    public bool IsDiscovered(int drinkTid)
    {
        return mDiscoveredDrinkTids.Contains(drinkTid);
    }

    public IReadOnlyCollection<int> GetDiscoveredTids()
    {
        return mDiscoveredDrinkTids;
    }

    public void SetDiscovered(IEnumerable<int> drinkTids)
    {
        mDiscoveredDrinkTids.Clear();
        if (drinkTids == null) return;

        foreach (var tid in drinkTids)
            mDiscoveredDrinkTids.Add(tid);
    }

    public void Dispose()
    {
        mDiscoveredDrinkTids.Clear();
    }
}
