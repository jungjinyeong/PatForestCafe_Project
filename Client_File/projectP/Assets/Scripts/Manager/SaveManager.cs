using System;
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

        data.DiscoveredRecipeTids.AddRange(GameInstance.Model.RecipeBook.GetDiscoveredTids());
        data.GoldIncomeUpgradeLevel = GameInstance.Model.Upgrade.Level;

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

        GameInstance.Model.RecipeBook.SetDiscovered(data.DiscoveredRecipeTids);
        GameInstance.Model.Upgrade.SetLevel(data.GoldIncomeUpgradeLevel);

        ApplyOfflineIncome(data.LastSaveUnixSeconds);

        Logger.Log($"[SaveManager] 불러오기 완료: {SavePath}");
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
