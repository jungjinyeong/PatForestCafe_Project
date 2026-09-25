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
    public int AssignedBreadTid;
    // true면 CTable.SubFurnitureRow, false면 CTable.FurnitureRow 소속 Tid.
    public bool IsSub;
}

[Serializable]
public class DeliveryOrderSaveEntry
{
    public int OrderNo;
    public int DrinkTid;
    public int ToppingMaterialTid;
    public string IncludeTag;
    public string ExcludeTag;
}

[Serializable]
public class PickupDrinkSaveEntry
{
    // false면 빈 칸(JsonUtility는 리스트 원소 null을 저장하지 못해 칸 위치 유지를 위해 플래그로 둔다).
    public bool HasDrink;
    public int DrinkTid;
    public CTable.eDrinkTempType Temp;
    public List<int> BaseMaterialTids = new List<int>();
    public List<int> CustomMaterialTids = new List<int>();
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
    public List<int> HiredStaffTids = new List<int>();
    public List<DeliveryOrderSaveEntry> DeliveryOrders = new List<DeliveryOrderSaveEntry>();
    public List<PickupDrinkSaveEntry> PickupDrinks = new List<PickupDrinkSaveEntry>();
    public long NextOrderRefillUnixSeconds;
    public int NextOrderNo;
    public long LastSaveUnixSeconds;
}
