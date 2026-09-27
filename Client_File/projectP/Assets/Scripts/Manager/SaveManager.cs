using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UniRx;

public class SaveManager : MonoBehaviour
{
    private const string SaveFileName = "save.json";
    private const float AutoSaveIntervalSeconds = 30f;

    [Header("Offline Income")]
    [Tooltip("카운터 캐릭터 능력치 시스템이 도입되기 전까지 사용하는 임시 고정 초당 코인 수익")]
    [SerializeField] private float mOfflineCoinPerSecond = 1f;
    [Tooltip("오프라인 수익으로 인정하는 최대 경과 시간(초). 기본 8시간")]
    [SerializeField] private float mOfflineMaxSeconds = 8 * 60 * 60;

    private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    private IDisposable mAutoSaveDisposable;

    private bool mHasPendingOfflineIncome;
    private int mPendingOfflineGold;
    private double mPendingOfflineSeconds;

    // BreadModel 항목은 GameInstance.Init() 시점(Load 호출 시점)이 아니라
    // GameModeLobby+FSM.InitBreadStands()에서 씬의 진열대가 Register()한 뒤에야 존재하므로,
    // 로드 시점엔 바로 적용하지 못하고 캐싱해뒀다가 InitBreadStands() 이후 ApplyPendingBreadData()로 적용한다.
    private List<BreadSaveEntry> mPendingBreadSaveEntries;

    public void Init()
    {
        Load();
        EnsureDefaultRecipes();
        StartAutoSave();
    }

    // 세이브가 없거나(신규) 예전 세이브라도 기본 빵 레시피(ConfigData.DefaultBreadRecipeTid)는 항상 발견된 상태로 시작한다.
    // RecipeBook.SetDiscovered()가 로드 시 목록을 통째로 덮어쓰므로 Load() 이후에 보장해야 한다.
    private void EnsureDefaultRecipes()
    {
        int defaultBreadTid = GameInstance.Config != null ? GameInstance.Config.GetValue(eConfigType.DefaultBreadRecipeTid) : 0;
        if (defaultBreadTid > 0)
            GameInstance.Model.RecipeBook.Discover(defaultBreadTid);
    }

    public void StartAutoSave()
    {
        StopAutoSave();
        mAutoSaveDisposable = Observable.Interval(TimeSpan.FromSeconds(AutoSaveIntervalSeconds))
            .Subscribe(_ => Save())
            .AddTo(this);
    }

    public void StopAutoSave()
    {
        mAutoSaveDisposable?.Dispose();
        mAutoSaveDisposable = null;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            Save();
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    public void Save()
    {
        var data = new SaveData();

        foreach (var item in GameInstance.Model.Item.GetAll())
            data.Items.Add(new ItemSaveEntry { Tid = item.Tid, Count = item.Count.Value });

        foreach (var wealth in GameInstance.Model.Item.GetAllWealth())
            data.Items.Add(new ItemSaveEntry { Tid = wealth.Tid, Count = wealth.Count.Value });

        foreach (var material in GameInstance.Model.Material.GetAll())
            data.Materials.Add(new ItemSaveEntry { Tid = material.Tid, Count = material.Count.Value });

        // 빵 세이브가 아직 적용 전(로비 FSM의 ApplyPendingBreadData() 이전)에 저장되면 — 자동 저장/OnApplicationPause —
        // BreadModel엔 복원 전 값(0 또는 그 사이 오븐 완료분)만 있어 저장된 재고가 통째로 사라진다.
        // 복원은 "더하기"이므로 대기 중인 세이브 값을 현재 값에 더해 저장하면 적용 후 결과와 같다.
        var pendingBreads = new Dictionary<int, BreadSaveEntry>();
        if (mPendingBreadSaveEntries != null)
        {
            foreach (var pending in mPendingBreadSaveEntries)
                pendingBreads[pending.Tid] = pending;
        }

        foreach (var bread in GameInstance.Model.Bread.GetAll())
        {
            int[] pendingCounts = null, pendingProduced = null;
            if (pendingBreads.TryGetValue(bread.TId, out var pendingEntry))
            {
                GetQualityCounts(pendingEntry, out pendingCounts, out pendingProduced);
                pendingBreads.Remove(bread.TId);
            }

            var entry = new BreadSaveEntry();
            entry.Tid = bread.TId;
            for (int i = 0; i < BreadQuality.COUNT; i++)
            {
                var quality = (eBreadQuality)(i + 1);
                entry.CountByQuality.Add(bread.GetCount(quality) + (pendingCounts?[i] ?? 0));
                entry.ProducedByQuality.Add(bread.GetProduced(quality) + (pendingProduced?[i] ?? 0));
            }
            entry.Count = entry.CountByQuality.Sum();
            entry.ProducedCount = entry.ProducedByQuality.Sum();
            data.Breads.Add(entry);
        }

        // BreadModel에 없는 Tid(테이블에서 빠진 빵 등)의 대기 세이브도 그대로 보존한다.
        data.Breads.AddRange(pendingBreads.Values);

        foreach (var kvp in GameInstance.Model.Placement.GetAllPlacements())
            data.PlacedFurniture.Add(new PlacedFurnitureSaveEntry { PlacementId = kvp.Key, Tid = kvp.Value.Tid, Position = kvp.Value.Position, AssignedBreadTid = kvp.Value.AssignedBreadTid, IsSub = kvp.Value.IsSub });

        data.DiscoveredRecipeTids.AddRange(GameInstance.Model.RecipeBook.GetDiscoveredTids());
        data.GoldIncomeUpgradeLevel = GameInstance.Model.Upgrade.Level;
        data.CafeLevel = GameInstance.Model.Cafe.Level.Value;
        data.CafeExp = GameInstance.Model.Cafe.Exp.Value;

        var business = GameInstance.Model.Business;
        data.BusinessDay = business.Day.Value;
        data.BusinessClosed = business.State.Value == eBusinessState.Closed;
        data.TodayStats = business.GetTodayStats();
        data.NextDayStats = business.NextDayStats;

        data.HiredWorkerCount = GameInstance.Model.Workshop.HiredWorkerCount.Value;
        foreach (var slot in GameInstance.Model.Workshop.Slots)
            data.WorkshopSlotMaterialTids.Add(slot.MaterialTid.Value);

        data.HighestUnlockedFloor = GameInstance.Model.Floor.HighestUnlockedFloor;

        foreach (var staff in GameInstance.Model.Staff.HiredStaff)
            data.HiredStaffTids.Add(staff.Tid);

        SaveDelivery(data);
        SaveOven(data);

        data.LastSaveUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        File.WriteAllText(SavePath, JsonUtility.ToJson(data));
        Logger.Log($"[SaveManager] 저장 완료: {SavePath}");
    }

    public void Load()
    {
        if (!File.Exists(SavePath))
        {
            Logger.Log("[SaveManager] 저장 파일이 없어 기본값으로 시작합니다.");
            return;
        }

        var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        if (data?.Items == null) return;

        foreach (var entry in data.Items)
            GameInstance.Model.Item.SetByTid(entry.Tid, entry.Count);

        foreach (var entry in data.Materials)
            GameInstance.Model.Material.SetByTid(entry.Tid, entry.Count);

        mPendingBreadSaveEntries = data.Breads;

        foreach (var entry in data.PlacedFurniture)
            GameInstance.Model.Placement.RestorePlacement(entry.PlacementId, entry.Tid, entry.Position, entry.AssignedBreadTid, entry.IsSub);

        GameInstance.Model.RecipeBook.SetDiscovered(data.DiscoveredRecipeTids);
        GameInstance.Model.Upgrade.SetLevel(data.GoldIncomeUpgradeLevel);
        GameInstance.Model.Cafe.Restore(data.CafeLevel, data.CafeExp);
        GameInstance.Model.Business.Restore(data.BusinessDay, data.TodayStats, data.BusinessClosed, data.NextDayStats);

        GameInstance.Model.Workshop.SetHiredWorkerCount(data.HiredWorkerCount);
        for (int i = 0; i < data.WorkshopSlotMaterialTids.Count; i++)
            GameInstance.Model.Workshop.SetSlotMaterial(i, data.WorkshopSlotMaterialTids[i]);

        GameInstance.Model.Floor.SetHighestUnlockedFloor(data.HighestUnlockedFloor);

        if (data.HiredStaffTids != null)
        {
            foreach (var tid in data.HiredStaffTids)
                GameInstance.Model.Staff.RestoreHiredStaff(tid);
        }

        LoadDelivery(data);
        LoadOven(data);

        ApplyOfflineIncome(data.LastSaveUnixSeconds);

        Logger.Log($"[SaveManager] 불러오기 완료: {SavePath}");
    }

    /// <summary>
    /// 로드해둔 빵 진열/생산 재고를 BreadModel에 적용한다.
    /// BreadModel 항목은 씬의 Intaraction_BreadStand가 Register()해야 생기므로,
    /// GameModeLobby+FSM.InitBreadStands()에서 모든 진열대를 등록한 직후 호출해야 한다.
    /// </summary>
    public void ApplyPendingBreadData()
    {
        if (mPendingBreadSaveEntries == null) return;

        foreach (var entry in mPendingBreadSaveEntries)
        {
            GetQualityCounts(entry, out var counts, out var produced);
            GameInstance.Model.Bread.RestoreFromSave(entry.Tid, counts, produced);
        }

        mPendingBreadSaveEntries = null;
    }

    // 품질 도입 전 세이브는 품질별 배열이 비어 있다 — 합계를 전부 하급으로 이관한다.
    private static void GetQualityCounts(BreadSaveEntry entry, out int[] counts, out int[] produced)
    {
        bool hasQuality = entry.CountByQuality != null && entry.CountByQuality.Count == BreadQuality.COUNT;
        counts = hasQuality ? entry.CountByQuality.ToArray() : new[] { entry.Count, 0, 0 };
        produced = hasQuality && entry.ProducedByQuality != null && entry.ProducedByQuality.Count == BreadQuality.COUNT
            ? entry.ProducedByQuality.ToArray()
            : new[] { entry.ProducedCount, 0, 0 };
    }

    /// <summary>
    /// 대기 중인 오프라인 수익을 1회 소비한다. GameModeLobby의 로비 UI 초기화 단계에서 호출한다.
    /// </summary>
    public bool TryConsumePendingOfflineIncome(out int gold, out double offlineSeconds)
    {
        gold = mPendingOfflineGold;
        offlineSeconds = mPendingOfflineSeconds;

        if (!mHasPendingOfflineIncome)
            return false;

        mHasPendingOfflineIncome = false;
        mPendingOfflineGold = 0;
        mPendingOfflineSeconds = 0;
        return true;
    }

    private void SaveDelivery(SaveData data)
    {
        var delivery = GameInstance.Model.Delivery;

        foreach (var order in delivery.Orders)
        {
            data.DeliveryOrders.Add(new DeliveryOrderSaveEntry
            {
                OrderNo = order.OrderNo,
                DrinkTid = order.DrinkTid,
                ToppingMaterialTid = order.ToppingMaterialTid,
                IncludeTag = order.IncludeTag,
                ExcludeTag = order.ExcludeTag,
            });
        }

        foreach (var slot in delivery.PickupSlots)
        {
            var drink = slot.Value;
            var entry = new PickupDrinkSaveEntry { HasDrink = drink != null };
            if (drink != null)
            {
                entry.DrinkTid = drink.DrinkTid;
                entry.Temp = drink.Temp;
                entry.BaseMaterialTids.AddRange(drink.BaseMaterialTids);
                entry.CustomMaterialTids.AddRange(drink.CustomMaterialTids);
            }
            data.PickupDrinks.Add(entry);
        }

        data.NextOrderRefillUnixSeconds = delivery.NextRefillUnixSeconds;
        data.NextOrderNo = delivery.NextOrderNo;
    }

    private void LoadDelivery(SaveData data)
    {
        var orders = new List<DeliveryOrderData>();
        if (data.DeliveryOrders != null)
        {
            foreach (var entry in data.DeliveryOrders)
                orders.Add(DeliveryOrderData.Create(entry.OrderNo, entry.DrinkTid, entry.ToppingMaterialTid, entry.IncludeTag, entry.ExcludeTag));
        }

        var pickups = new List<PickupDrinkData>();
        if (data.PickupDrinks != null)
        {
            foreach (var entry in data.PickupDrinks)
            {
                pickups.Add(entry.HasDrink
                    ? PickupDrinkData.Create(entry.DrinkTid, entry.Temp, entry.BaseMaterialTids, entry.CustomMaterialTids)
                    : null);
            }
        }

        GameInstance.Model.Delivery.Restore(orders, pickups, data.NextOrderRefillUnixSeconds, data.NextOrderNo);
    }

    private void SaveOven(SaveData data)
    {
        var oven = GameInstance.Model.Oven;
        data.OvenLevel = oven.Level.Value;

        foreach (var tray in oven.Trays)
        {
            data.OvenTrays.Add(new OvenTraySaveEntry
            {
                State = tray.State,
                RecipeTid = tray.RecipeTid,
                Quantity = tray.Quantity,
                BaseMaterialTids = new List<int>(tray.BaseMaterialTids),
                ExtraMaterialTids = new List<int>(tray.ExtraMaterialTids),
                Quality = tray.Quality,
                BakeEndUnixSeconds = tray.BakeEndUnixSeconds,
                BakeDurationSeconds = tray.BakeDurationSeconds,
            });
        }
    }

    private void LoadOven(SaveData data)
    {
        var trays = new List<OvenTrayData>();
        if (data.OvenTrays != null)
        {
            foreach (var entry in data.OvenTrays)
            {
                trays.Add(new OvenTrayData
                {
                    State = entry.State,
                    RecipeTid = entry.RecipeTid,
                    Quantity = entry.Quantity,
                    BaseMaterialTids = entry.BaseMaterialTids ?? new List<int>(),
                    ExtraMaterialTids = entry.ExtraMaterialTids ?? new List<int>(),
                    Quality = entry.Quality == 0 ? eBreadQuality.Low : entry.Quality,
                    BakeEndUnixSeconds = entry.BakeEndUnixSeconds,
                    BakeDurationSeconds = entry.BakeDurationSeconds,
                });
            }
        }

        GameInstance.Model.Oven.Restore(Mathf.Max(1, data.OvenLevel), trays);
    }

    private void ApplyOfflineIncome(long lastSaveUnixSeconds)
    {
        if (lastSaveUnixSeconds <= 0) return;

        long nowUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        double elapsedSeconds = nowUnixSeconds - lastSaveUnixSeconds;
        double offlineSeconds = Math.Clamp(elapsedSeconds, 0d, (double)mOfflineMaxSeconds);

        int gold = (int)(offlineSeconds * mOfflineCoinPerSecond);
        if (gold <= 0) return;

        GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold)?.Add(gold);

        mHasPendingOfflineIncome = true;
        mPendingOfflineGold = gold;
        mPendingOfflineSeconds = offlineSeconds;

        Logger.Log($"[SaveManager] 오프라인 수익 정산: {gold} Gold ({offlineSeconds:F0}초)");
    }
}
