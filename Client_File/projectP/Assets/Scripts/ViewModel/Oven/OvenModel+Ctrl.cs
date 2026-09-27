using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

public partial class OvenModel
{
    // TODO(기획): 임시 레벨표. 인덱스 = 레벨-1. 확정 시 테이블로 이동.
    private static readonly int[] TRAY_COUNT_BY_LEVEL = { 2, 3, 3 };
    private static readonly int[] MAX_QUANTITY_BY_LEVEL = { 10, 20, 30 };
    private static readonly int[] BAKE_SECONDS_BY_LEVEL = { 15, 12, 10 };
    private static readonly int[] UPGRADE_COST_BY_LEVEL = { 500, 1500 };   // 다음 레벨로 올리는 비용

    public int MaxLevel => TRAY_COUNT_BY_LEVEL.Length;
    public bool IsMaxLevel => mLevel.Value >= MaxLevel;

    public int UnlockedTrayCount => GetTrayCount(mLevel.Value);
    public int MaxQuantity => GetMaxQuantity(mLevel.Value);
    public int BakeSeconds => GetBakeSeconds(mLevel.Value);
    public int NextUpgradeCost => IsMaxLevel ? 0 : UPGRADE_COST_BY_LEVEL[mLevel.Value - 1];

    public static int GetTrayCount(int level) => TRAY_COUNT_BY_LEVEL[ClampLevelIndex(level)];
    public static int GetMaxQuantity(int level) => MAX_QUANTITY_BY_LEVEL[ClampLevelIndex(level)];
    public static int GetBakeSeconds(int level) => BAKE_SECONDS_BY_LEVEL[ClampLevelIndex(level)];

    private static int ClampLevelIndex(int level) => Mathf.Clamp(level - 1, 0, TRAY_COUNT_BY_LEVEL.Length - 1);

    public bool IsTrayUnlocked(int index) => index >= 0 && index < UnlockedTrayCount;

    public int ReadyTrayCount => Trays.Take(UnlockedTrayCount).Count(t => t.State == eOvenTrayState.Ready);
    public int ActiveTrayCount => Trays.Take(UnlockedTrayCount).Count(t => !t.IsEmpty);

    // 다음 업그레이드로 바뀌는 점 요약(예: "트레이 1칸 해금"). 최대 레벨이면 null.
    public string GetNextUpgradeSummary()
    {
        if (IsMaxLevel)
            return null;

        int next = mLevel.Value + 1;
        var parts = new List<string>();
        if (GetTrayCount(next) > UnlockedTrayCount) parts.Add($"트레이 {GetTrayCount(next) - UnlockedTrayCount}칸 해금");
        if (GetMaxQuantity(next) > MaxQuantity) parts.Add($"최대 수량 {GetMaxQuantity(next)}개");
        if (GetBakeSeconds(next) < BakeSeconds) parts.Add($"굽기 {GetBakeSeconds(next)}초");
        return string.Join(", ", parts);
    }

    public bool TryUpgrade()
    {
        if (IsMaxLevel)
            return false;

        var gold = GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold);
        int cost = NextUpgradeCost;
        if (gold == null || gold.Count.Value < cost)
            return false;

        gold.Consume(cost);
        mLevel.Value++;
        mOnTraysChanged.OnNext(Unit.Default);
        return true;
    }

    #region Tray

    // 반죽 설정 저장. 굽는 중이거나 잠긴 트레이는 거부.
    public bool SetTray(int index, int recipeTid, int quantity, IEnumerable<int> baseMaterialTids, IEnumerable<int> extraMaterialTids)
    {
        if (!IsTrayUnlocked(index) || Trays[index].State == eOvenTrayState.Baking || recipeTid == 0)
            return false;

        var tray = Trays[index];
        tray.RecipeTid = recipeTid;
        tray.Quantity = Mathf.Clamp(quantity, 1, MaxQuantity);
        tray.BaseMaterialTids = baseMaterialTids?.Where(t => t != 0).ToList() ?? new List<int>();
        tray.ExtraMaterialTids = extraMaterialTids?.Where(t => t != 0).ToList() ?? new List<int>();
        tray.Quality = CalcQuality(tray.BaseMaterialTids);
        tray.State = eOvenTrayState.Ready;
        tray.BakeEndUnixSeconds = 0;

        mOnTraysChanged.OnNext(Unit.Default);
        return true;
    }

    public bool ClearTray(int index)
    {
        if (index < 0 || index >= Trays.Length || Trays[index].State == eOvenTrayState.Baking)
            return false;

        Trays[index].Clear();
        mOnTraysChanged.OnNext(Unit.Default);
        return true;
    }

    // 기본 재료 등급(BreadMaterial.Grade) 평균을 반올림한 완성 품질.
    public static eBreadQuality CalcQuality(IEnumerable<int> baseMaterialTids)
    {
        var table = GameInstance.Table.GetTable<CTable.BreadMaterialRow>();
        var grades = baseMaterialTids.Select(tid => table?.Get(tid)?.Grade ?? 1);
        return BreadQuality.FromMaterialGrades(grades);
    }

    #endregion

    #region Bake

    // 굽기 대기(Ready) 트레이 전체의 필요 재료 합계 = (기본 + 추가 재료) × 수량.
    public Dictionary<int, int> GetRequiredMaterials()
    {
        var required = new Dictionary<int, int>();
        foreach (var tray in Trays.Take(UnlockedTrayCount))
        {
            if (tray.State != eOvenTrayState.Ready)
                continue;

            foreach (int tid in tray.BaseMaterialTids.Concat(tray.ExtraMaterialTids))
            {
                required.TryGetValue(tid, out int count);
                required[tid] = count + tray.Quantity;
            }
        }
        return required;
    }

    // 대기 중인 모든 트레이의 재료를 한 번에 확인·차감하고 굽기를 시작한다. 부족하면 부족한 재료 이름을 돌려준다.
    public bool TryStartBake(out string shortageName)
    {
        shortageName = null;
        if (ReadyTrayCount == 0)
            return false;

        var required = GetRequiredMaterials();
        foreach (var pair in required)
        {
            if (!GameInstance.Model.Material.HasEnough(pair.Key, pair.Value))
            {
                shortageName = GameInstance.Table.Get<CTable.BreadMaterialRow>(pair.Key)?.Name ?? pair.Key.ToString();
                return false;
            }
        }

        foreach (var pair in required)
            GameInstance.Model.Material.Consume(pair.Key, pair.Value);

        long endTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + BakeSeconds;
        foreach (var tray in Trays.Take(UnlockedTrayCount))
        {
            if (tray.State != eOvenTrayState.Ready)
                continue;

            tray.State = eOvenTrayState.Baking;
            tray.BakeEndUnixSeconds = endTime;
            tray.BakeDurationSeconds = BakeSeconds;
        }

        mOnTraysChanged.OnNext(Unit.Default);
        return true;
    }

    public long GetRemainingSeconds(int index)
    {
        var tray = Trays[index];
        if (tray.State != eOvenTrayState.Baking)
            return 0;

        return Math.Max(0, tray.BakeEndUnixSeconds - DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    // 굽기가 끝난 트레이를 창고(빵 생산 재고, 품질별)로 자동 보관하고 비운다.
    private void TickBake()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        bool changed = false;

        foreach (var tray in Trays)
        {
            if (tray.State != eOvenTrayState.Baking || tray.BakeEndUnixSeconds > now)
                continue;

            GameInstance.Model.Bread.AddProduced(tray.RecipeTid, tray.Quality, tray.Quantity);
            mOnBakeCompleted.OnNext((tray.RecipeTid, tray.Quality, tray.Quantity));

            tray.Clear();
            changed = true;
        }

        if (changed)
            mOnTraysChanged.OnNext(Unit.Default);
    }

    #endregion

    // 세이브 복원. SaveManager.Load()에서 호출.
    public void Restore(int level, IReadOnlyList<OvenTrayData> trays)
    {
        mLevel.Value = Mathf.Clamp(level, 1, MaxLevel);

        for (int i = 0; i < Trays.Length; i++)
        {
            Trays[i].Clear();
            if (trays == null || i >= trays.Count || trays[i] == null)
                continue;

            var src = trays[i];
            Trays[i].State = src.State;
            Trays[i].RecipeTid = src.RecipeTid;
            Trays[i].Quantity = src.Quantity;
            Trays[i].BaseMaterialTids = new List<int>(src.BaseMaterialTids);
            Trays[i].ExtraMaterialTids = new List<int>(src.ExtraMaterialTids);
            Trays[i].Quality = src.Quality;
            Trays[i].BakeEndUnixSeconds = src.BakeEndUnixSeconds;
            Trays[i].BakeDurationSeconds = src.BakeDurationSeconds > 0 ? src.BakeDurationSeconds : BakeSeconds;
        }

        mOnTraysChanged.OnNext(Unit.Default);
    }
}
