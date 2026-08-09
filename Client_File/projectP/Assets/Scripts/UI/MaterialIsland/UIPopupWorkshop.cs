using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;

// 재배형 공방: 골드로 일꾼을 고용하고, 고용한 일꾼을 슬롯에 배치해 지정한 재료를 방치형으로 자동 생산한다.
public class UIPopupWorkshop : UIWndBase, IUIParam<UIPopupWorkshop.Param>
{
    public struct Param
    {
    }

    [Header("Hire")]
    [SerializeField] private TextMeshProUGUI mTextHiredCount;
    [SerializeField] private TextMeshProUGUI mTextHireCost;
    [SerializeField] private UIButtonEx mBtnHire;

    [Header("Slots")]
    [SerializeField] private UIWorkshopSlotView[] mSlotViews;

    private readonly List<int> mCandidateMaterialTids = new List<int>();
    private int[] mSlotCandidateIndices;

    public override eUIType GetUIType() => eUIType.UIPopupWorkshop;

    public override void Init()
    {
        base.Init();

        mBtnHire.OnSubscribeOnClick(OnClickHire).AddTo(this);

        mSlotCandidateIndices = new int[mSlotViews.Length];
        for (int i = 0; i < mSlotViews.Length; i++)
            mSlotViews[i].Init(i, OnClickCycleMaterial, OnClickToggleAssign);
    }

    public override void Open()
    {
        base.Open();

        RefreshCandidateMaterials();
        RefreshAll();
    }

    public void Set(Param param)
    {
    }

    private void RefreshCandidateMaterials()
    {
        mCandidateMaterialTids.Clear();

        var drinkMaterialGroup = GameInstance.Table.GetTable<CTable.DrinkMaterialRow>();
        if (drinkMaterialGroup != null)
            foreach (var row in drinkMaterialGroup.All.Values)
                mCandidateMaterialTids.Add(row.Tid);

        var breadMaterialGroup = GameInstance.Table.GetTable<CTable.BreadMaterialRow>();
        if (breadMaterialGroup != null)
            foreach (var row in breadMaterialGroup.All.Values)
                mCandidateMaterialTids.Add(row.Tid);
    }

    private void OnClickHire()
    {
        if (!GameInstance.Model.Workshop.TryHireWorker())
        {
            Logger.Log("[UIPopupWorkshop] 골드가 부족해 일꾼을 고용할 수 없습니다.");
            return;
        }

        RefreshAll();
    }

    private void OnClickCycleMaterial(int slotIndex)
    {
        if (mCandidateMaterialTids.Count == 0)
            return;

        mSlotCandidateIndices[slotIndex] = (mSlotCandidateIndices[slotIndex] + 1) % mCandidateMaterialTids.Count;
        RefreshSlot(slotIndex);
    }

    private void OnClickToggleAssign(int slotIndex)
    {
        var workshop = GameInstance.Model.Workshop;
        var slot = workshop.Slots[slotIndex];

        if (slot.MaterialTid.Value > 0)
        {
            workshop.UnassignWorker(slotIndex);
        }
        else
        {
            if (mCandidateMaterialTids.Count == 0)
                return;

            int materialTid = mCandidateMaterialTids[mSlotCandidateIndices[slotIndex]];
            if (!workshop.AssignWorker(slotIndex, materialTid))
            {
                Logger.Log("[UIPopupWorkshop] 배치 가능한 일꾼이 없습니다. 먼저 고용하세요.");
                return;
            }
        }

        RefreshAll();
    }

    private void RefreshAll()
    {
        var workshop = GameInstance.Model.Workshop;

        mTextHiredCount?.SetTextEx($"일꾼 배치: {workshop.AssignedWorkerCount} / {workshop.HiredWorkerCount.Value}");
        mTextHireCost?.SetTextEx($"고용 비용: {workshop.GetNextHireCost():N0} Gold");

        for (int i = 0; i < mSlotViews.Length; i++)
            RefreshSlot(i);
    }

    private void RefreshSlot(int slotIndex)
    {
        var workshop = GameInstance.Model.Workshop;
        var slot = workshop.Slots[slotIndex];

        if (slot.MaterialTid.Value > 0)
        {
            mSlotViews[slotIndex].SetStatusText($"생산 중: {GetMaterialName(slot.MaterialTid.Value)} (해제)");
        }
        else
        {
            string candidateName = mCandidateMaterialTids.Count > 0
                ? GetMaterialName(mCandidateMaterialTids[mSlotCandidateIndices[slotIndex]])
                : "-";
            mSlotViews[slotIndex].SetStatusText($"비어있음 (선택: {candidateName})");
        }
    }

    private string GetMaterialName(int tid)
    {
        var drinkRow = GameInstance.Table.GetTable<CTable.DrinkMaterialRow>()?.Get(tid);
        if (drinkRow != null)
            return drinkRow.Name;

        var breadRow = GameInstance.Table.GetTable<CTable.BreadMaterialRow>()?.Get(tid);
        return breadRow?.Name ?? tid.ToString();
    }
}
