using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class HiredStaffRecord
{
    public int Tid;
}

// 직업사무소 채용: 골드로 StaffRow를 채용해 명단에 등록한다. 배치(층/업무 지정)는 이번 범위 밖 — 채용까지만.
// 채용 비용 수치는 기획 확정 전까지 코드 내 고정값으로 관리한다(UpgradeModel/WorkshopModel과 동일한 임시 패턴).
public class StaffModel : IModelBase
{
    private const int BaseHireCost = 150;
    private const float HireCostGrowthRate = 1.4f;

    public IReadOnlyReactiveProperty<int> HiredCount => mHiredCount;
    private readonly ReactiveProperty<int> mHiredCount = new ReactiveProperty<int>(0);

    private readonly List<HiredStaffRecord> mHiredStaff = new List<HiredStaffRecord>();
    public IReadOnlyList<HiredStaffRecord> HiredStaff => mHiredStaff;

    public void Init() { }

    public int GetNextHireCost()
    {
        return Mathf.RoundToInt(BaseHireCost * Mathf.Pow(HireCostGrowthRate, mHiredStaff.Count));
    }

    public bool TryHireStaff(int tid)
    {
        var row = GameInstance.Table.Get<CTable.StaffRow>(tid);
        if (row == null)
            return false;

        var gold = GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold);
        int cost = GetNextHireCost();
        if (gold == null || gold.Count.Value < cost)
            return false;

        gold.Consume(cost);
        mHiredStaff.Add(new HiredStaffRecord { Tid = tid });
        mHiredCount.Value = mHiredStaff.Count;
        return true;
    }

    // 세이브 로드 복원 전용 — 골드 소모 없이 명단만 채운다.
    public void RestoreHiredStaff(int tid)
    {
        mHiredStaff.Add(new HiredStaffRecord { Tid = tid });
        mHiredCount.Value = mHiredStaff.Count;
    }

    public void Dispose()
    {
        mHiredStaff.Clear();
        mHiredCount.Value = 0;
        mHiredCount.Dispose();
    }
}
