using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniRx;

// 재배형 공방: 골드로 고용한 일꾼을 슬롯에 배치하면, 배치 시 지정한 재료를 주기적으로 자동 생산한다(방치형).
// 일꾼은 종류 구분 없이 전부 동일하며, 어떤 재료를 생산할지는 슬롯에 배치할 때 플레이어가 정한다.
// 슬롯 수/고용 비용/생산 주기 수치는 기획 확정 전까지 코드 내 고정값으로 관리한다(UpgradeModel과 동일 방식, 사전 협의됨).
public class WorkshopModel : IModelBase
{
    public const int SlotCount = 3;
    private const int BaseHireCost = 100;
    private const float HireCostGrowthRate = 1.5f;
    private const float ProductionIntervalSeconds = 5f;
    private const int ProductionAmountPerTick = 1;

    public IReadOnlyReactiveProperty<int> HiredWorkerCount => mHiredWorkerCount;
    private readonly ReactiveProperty<int> mHiredWorkerCount = new ReactiveProperty<int>(0);

    public IReadOnlyList<WorkshopSlotData> Slots => mSlots;
    private readonly List<WorkshopSlotData> mSlots = new List<WorkshopSlotData>();

    public int AssignedWorkerCount => mSlots.Count(slot => slot.MaterialTid.Value > 0);
    public bool CanAssignWorker => AssignedWorkerCount < mHiredWorkerCount.Value;

    private CompositeDisposable mDisposables;

    public void Init()
    {
        for (int i = 0; i < SlotCount; i++)
            mSlots.Add(new WorkshopSlotData { SlotIndex = i });

        mDisposables = new CompositeDisposable();
        Observable.Interval(TimeSpan.FromSeconds(ProductionIntervalSeconds))
            .Subscribe(_ => ProduceAll())
            .AddTo(mDisposables);
    }

    public int GetNextHireCost()
    {
        return Mathf.RoundToInt(BaseHireCost * Mathf.Pow(HireCostGrowthRate, mHiredWorkerCount.Value));
    }

    public bool TryHireWorker()
    {
        var gold = GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold);
        int cost = GetNextHireCost();
        if (gold == null || gold.Count.Value < cost)
            return false;

        gold.Consume(cost);
        mHiredWorkerCount.Value++;
        return true;
    }

    public bool AssignWorker(int slotIndex, int materialTid)
    {
        var slot = GetSlot(slotIndex);
        if (slot == null || materialTid <= 0)
            return false;

        if (slot.MaterialTid.Value == 0 && !CanAssignWorker)
            return false;

        slot.MaterialTid.Value = materialTid;
        return true;
    }

    public void UnassignWorker(int slotIndex)
    {
        var slot = GetSlot(slotIndex);
        if (slot == null) return;

        slot.MaterialTid.Value = 0;
    }

    public void SetHiredWorkerCount(int count)
    {
        mHiredWorkerCount.Value = Mathf.Max(0, count);
    }

    public void SetSlotMaterial(int slotIndex, int materialTid)
    {
        var slot = GetSlot(slotIndex);
        if (slot == null) return;

        slot.MaterialTid.Value = materialTid;
    }

    private WorkshopSlotData GetSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < mSlots.Count ? mSlots[slotIndex] : null;
    }

    private void ProduceAll()
    {
        foreach (var slot in mSlots)
        {
            if (slot.MaterialTid.Value > 0)
                GameInstance.Model.Material.Gather(slot.MaterialTid.Value, ProductionAmountPerTick);
        }
    }

    public void Dispose()
    {
        mDisposables?.Dispose();
        mHiredWorkerCount.Value = 0;

        foreach (var slot in mSlots)
            slot.Dispose();
        mSlots.Clear();
    }
}
