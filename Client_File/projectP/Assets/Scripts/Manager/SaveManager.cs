using System;
using System.Collections.Generic;
using System.IO;
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
        StartAutoSave();
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

        foreach (var bread in GameInstance.Model.Bread.GetAll())
            data.Breads.Add(new BreadSaveEntry { Tid = bread.TId, Count = bread.Count.Value, ProducedCount = bread.ProducedCount.Value });

        foreach (var kvp in GameInstance.Model.Placement.GetAllPlacements())
            data.PlacedFurniture.Add(new PlacedFurnitureSaveEntry { PlacementId = kvp.Key, Tid = kvp.Value.Tid, Position = kvp.Value.Position, AssignedBreadTid = kvp.Value.AssignedBreadTid });

        data.DiscoveredRecipeTids.AddRange(GameInstance.Model.RecipeBook.GetDiscoveredTids());
        data.GoldIncomeUpgradeLevel = GameInstance.Model.Upgrade.Level;

        data.HiredWorkerCount = GameInstance.Model.Workshop.HiredWorkerCount.Value;
        foreach (var slot in GameInstance.Model.Workshop.Slots)
            data.WorkshopSlotMaterialTids.Add(slot.MaterialTid.Value);

        data.HighestUnlockedFloor = GameInstance.Model.Floor.HighestUnlockedFloor;

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
            GameInstance.Model.Placement.RestorePlacement(entry.PlacementId, entry.Tid, entry.Position, entry.AssignedBreadTid);

        GameInstance.Model.RecipeBook.SetDiscovered(data.DiscoveredRecipeTids);
        GameInstance.Model.Upgrade.SetLevel(data.GoldIncomeUpgradeLevel);

        GameInstance.Model.Workshop.SetHiredWorkerCount(data.HiredWorkerCount);
        for (int i = 0; i < data.WorkshopSlotMaterialTids.Count; i++)
            GameInstance.Model.Workshop.SetSlotMaterial(i, data.WorkshopSlotMaterialTids[i]);

        GameInstance.Model.Floor.SetHighestUnlockedFloor(data.HighestUnlockedFloor);

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
            GameInstance.Model.Bread.SetByTid(entry.Tid, entry.Count, entry.ProducedCount);

        mPendingBreadSaveEntries = null;
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
