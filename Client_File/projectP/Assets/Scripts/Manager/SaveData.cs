using System;
using System.Collections.Generic;

[Serializable]
public class ItemSaveEntry
{
    public int Tid;
    public int Count;
}

[Serializable]
public class SaveData
{
    public List<ItemSaveEntry> Items = new List<ItemSaveEntry>();
    public List<ItemSaveEntry> Materials = new List<ItemSaveEntry>();
    public List<int> DiscoveredRecipeTids = new List<int>();
    public int GoldIncomeUpgradeLevel;
    public int HiredWorkerCount;
    public List<int> WorkshopSlotMaterialTids = new List<int>();
    public long LastSaveUnixSeconds;
}
