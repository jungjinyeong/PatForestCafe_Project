using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemSaveEntry
{
    public int Tid;
    public int Count;
}

[Serializable]
public class BreadSaveEntry
{
    public int Tid;
    public int Count;
    public int ProducedCount;
}

[Serializable]
public class PlacedFurnitureSaveEntry
{
    public int PlacementId;
    public int Tid;
    public Vector3 Position;
}

[Serializable]
public class SaveData
{
    public List<ItemSaveEntry> Items = new List<ItemSaveEntry>();
    public List<ItemSaveEntry> Materials = new List<ItemSaveEntry>();
    public List<BreadSaveEntry> Breads = new List<BreadSaveEntry>();
    public List<PlacedFurnitureSaveEntry> PlacedFurniture = new List<PlacedFurnitureSaveEntry>();
    public List<int> DiscoveredRecipeTids = new List<int>();
    public int GoldIncomeUpgradeLevel;
    public int HiredWorkerCount;
    public List<int> WorkshopSlotMaterialTids = new List<int>();
    public int HighestUnlockedFloor;
    public long LastSaveUnixSeconds;
}
