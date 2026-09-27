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
    // 품질 도입 전 세이브 호환용(합계). 품질별 배열이 비어 있으면 이 값을 하급으로 이관한다.
    public int Count;
    public int ProducedCount;
    // 품질별(인덱스 0=하급 1=중급 2=고급) 진열 수량 / 생산 재고.
    public List<int> CountByQuality = new List<int>();
    public List<int> ProducedByQuality = new List<int>();
}

[Serializable]
public class OvenTraySaveEntry
{
    public eOvenTrayState State;
    public int RecipeTid;
    public int Quantity;
    public List<int> BaseMaterialTids = new List<int>();
    public List<int> ExtraMaterialTids = new List<int>();
    public eBreadQuality Quality;
    public long BakeEndUnixSeconds;
    public int BakeDurationSeconds;
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
    public int CafeLevel;
    public int CafeExp;
    public int BusinessDay;
    public bool BusinessClosed;
    public BusinessStats TodayStats = new BusinessStats();
    public BusinessStats NextDayStats = new BusinessStats();
    public int HiredWorkerCount;
    public List<int> WorkshopSlotMaterialTids = new List<int>();
    public int HighestUnlockedFloor;
    public List<int> HiredStaffTids = new List<int>();
    public List<DeliveryOrderSaveEntry> DeliveryOrders = new List<DeliveryOrderSaveEntry>();
    public List<PickupDrinkSaveEntry> PickupDrinks = new List<PickupDrinkSaveEntry>();
    public long NextOrderRefillUnixSeconds;
    public int NextOrderNo;
    public int OvenLevel;
    public List<OvenTraySaveEntry> OvenTrays = new List<OvenTraySaveEntry>();
    public long LastSaveUnixSeconds;
}
