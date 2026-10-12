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
    public List<int> BaseIngredientTids = new List<int>();
    public List<int> ExtraIngredientTids = new List<int>();
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
    // 층(WaypointGroup.Order) + footprint 기준 셀. 0층이면 Floor/Cell이 없던 구 세이브.
    public int Floor;
    public Vector2Int Cell;
    public int AssignedBreadTid;
    // true면 CTable.SubFurnitureRow, false면 CTable.FurnitureRow 소속 Tid.
    public bool IsSub;
    // 가구 방향(CTable.eFurnitureDirection). 구 세이브는 0(Down).
    public int Direction;
}

// 보유 가구 수(구매한 총 개수, 배치된 것 포함). 보관 중 = Count − 배치 수.
[Serializable]
public class OwnedFurnitureSaveEntry
{
    public int Tid;
    public bool IsSub;
    public int Count;
}

[Serializable]
public class DeliveryOrderSaveEntry
{
    public int OrderNo;
    public int DrinkTid;
    public int ToppingIngredientTid;
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
    public List<int> BaseIngredientTids = new List<int>();
    public List<int> CustomIngredientTids = new List<int>();
}

[Serializable]
public class SaveData
{
    public List<ItemSaveEntry> Items = new List<ItemSaveEntry>();
    public List<ItemSaveEntry> Ingredients = new List<ItemSaveEntry>();
    public List<BreadSaveEntry> Breads = new List<BreadSaveEntry>();
    public List<PlacedFurnitureSaveEntry> PlacedFurniture = new List<PlacedFurnitureSaveEntry>();
    // 없던 구 세이브는 배치된 가구 수로 채운다(PlacementModel.EnsureOwnedCoversPlacements).
    public List<OwnedFurnitureSaveEntry> OwnedFurniture = new List<OwnedFurnitureSaveEntry>();
    public List<int> DiscoveredRecipeTids = new List<int>();
    public int GoldIncomeUpgradeLevel;
    public int CafeLevel;
    public int CafeExp;
    public int BusinessDay;
    public bool BusinessClosed;
    public BusinessStats TodayStats = new BusinessStats();
    public BusinessStats NextDayStats = new BusinessStats();
    public int HiredWorkerCount;
    public List<int> WorkshopSlotIngredientTids = new List<int>();
    // 구 세이브(FloorLayoutVersion < 2) 읽기 전용 — "Order 1~N층까지 열림". 새 세이브는 UnlockedFloors를 쓴다.
    public int HighestUnlockedFloor;
    // 열린 층 ID 목록(Floor.csv Tid = WaypointGroup.Order).
    public List<int> UnlockedFloors = new List<int>();
    // 층 ID 체계 버전. 2 = 아래→위 1~7(B3 B2 B1 1F T1 T2 T3). 0/1 = 그 이전(B2가 Order 1이던 6층 구성).
    public int FloorLayoutVersion;
    public List<int> HiredStaffTids = new List<int>();
    public List<DeliveryOrderSaveEntry> DeliveryOrders = new List<DeliveryOrderSaveEntry>();
    public List<PickupDrinkSaveEntry> PickupDrinks = new List<PickupDrinkSaveEntry>();
    public long NextOrderRefillUnixSeconds;
    public int NextOrderNo;
    public int OvenLevel;
    public List<OvenTraySaveEntry> OvenTrays = new List<OvenTraySaveEntry>();
    public long LastSaveUnixSeconds;
}
