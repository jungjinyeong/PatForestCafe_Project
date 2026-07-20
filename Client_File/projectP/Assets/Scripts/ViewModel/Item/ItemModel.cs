using System.Collections.Generic;
using UniRx;
using CTable;

public partial class ItemModel : IModelBase
{
    Dictionary<int, ItemData> mDicItems = new();
    Dictionary<eMoneyType, WealthData> mDicWealths = new();

    CompositeDisposable mDisposables = new CompositeDisposable();

    public void Init()
    {
        var group = GameInstance.Table.GetTable<CTable.ItemRow>();
        if (group == null)
        {
            Logger.Warning("[ItemModel] ItemGroup을 찾을 수 없습니다.");
            return;
        }

        // TODO : 아이템 저장을 하기 시작하면 다 만들 필요없음.
        foreach (var row in group.All.Values)
        {
            if (row.ItemType == CTable.eItemType.Money)
            {
                var moneyRow = GameInstance.Table.Get<CTable.ItemMoneyRow>(row.Tid);
                if(moneyRow == null)
                {
                    Logger.Warning($"[ItemModel] MoneyRow를 찾을 수 없습니다. Tid: {row.Tid}");
                    continue;
                }
                if(mDicWealths.ContainsKey(moneyRow.MoneyType))
                {
                    Logger.Warning($"[ItemModel] 중복된 MoneyType이 존재합니다. MoneyType: {moneyRow.MoneyType}");
                    continue;
                }
                mDicWealths[moneyRow.MoneyType] = WealthData.CreateWealthData(moneyRow);
                continue;
            }

            var item = ItemData.Create(row);
            mDicItems[row.Tid] = item;
        }
    }

    #region Wealth

    public WealthData GetWealth(eMoneyType moneyType)
    {
        return mDicWealths.TryGetValue(moneyType, out var wealth) ? wealth : null;
    }

    public IEnumerable<WealthData> GetAllWealth() => mDicWealths.Values;

    #endregion

    public void SetByTid(int tid, int amount)
    {
        if (mDicItems.TryGetValue(tid, out var item))
        {
            item.Set(amount);
            return;
        }

        foreach (var wealth in mDicWealths.Values)
        {
            if (wealth.Tid == tid)
            {
                wealth.Set(amount);
                return;
            }
        }

        Logger.Warning($"[ItemModel] 저장 데이터 복원 실패, 존재하지 않는 Tid: {tid}");
    }

    public ItemData Get(int tid)
    {
        return mDicItems.TryGetValue(tid, out var item) ? item : null;
    }

    public IEnumerable<ItemData> GetAll() => mDicItems.Values;

    public void Add(int tid, int amount = 1)
    {
        if (mDicItems.TryGetValue(tid, out var item))
            item.Add(amount);
        else
            Logger.Warning($"[ItemModel] 존재하지 않는 아이템 Tid: {tid}");
    }

    public void Consume(int tid, int amount = 1)
    {
        if (mDicItems.TryGetValue(tid, out var item))
            item.Consume(amount);
        else
            Logger.Warning($"[ItemModel] 존재하지 않는 아이템 Tid: {tid}");
    }

    public void Set(int tid, int amount)
    {
        if (mDicItems.TryGetValue(tid, out var item))
            item.Set(amount);
        else
            Logger.Warning($"[ItemModel] 존재하지 않는 아이템 Tid: {tid}");
    }

    public bool HasEnough(int tid, int amount)
    {
        var item = Get(tid);
        return item != null && item.Count.Value >= amount;
    }

    public void Dispose()
    {
        mDisposables.Dispose();
        
        foreach (var item in mDicItems.Values)
            item.Dispose();
        mDicItems.Clear();

        mDicWealths.Clear();
    }
}
