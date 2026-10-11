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

    private readonly List<int> mCandidateIngredientTids = new List<int>();
    private int[] mSlotCandidateIndices;

    public override eUIType GetUIType() => eUIType.PopupWorkshop;

    public override void Init()
    {
        base.Init();

        mBtnHire.OnSubscribeOnClick(OnClickHire).AddTo(this);

        mSlotCandidateIndices = new int[mSlotViews.Length];
        for (int i = 0; i < mSlotViews.Length; i++)
            mSlotViews[i].Init(i, OnClickCycleIngredient, OnClickToggleAssign);
    }

    public override void Open()
    {
        base.Open();

        RefreshCandidateIngredients();
        RefreshAll();
    }

    public void Set(Param param)
    {
    }

    private void RefreshCandidateIngredients()
    {
        mCandidateIngredientTids.Clear();

        var drinkIngredientGroup = GameInstance.Table.GetTable<CTable.DrinkIngredientRow>();
        if (drinkIngredientGroup != null)
            foreach (var row in drinkIngredientGroup.All.Values)
                mCandidateIngredientTids.Add(row.Tid);

        var breadIngredientGroup = GameInstance.Table.GetTable<CTable.BreadIngredientRow>();
        if (breadIngredientGroup != null)
            foreach (var row in breadIngredientGroup.All.Values)
                mCandidateIngredientTids.Add(row.Tid);
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

    private void OnClickCycleIngredient(int slotIndex)
    {
        if (mCandidateIngredientTids.Count == 0)
            return;

        mSlotCandidateIndices[slotIndex] = (mSlotCandidateIndices[slotIndex] + 1) % mCandidateIngredientTids.Count;
        RefreshSlot(slotIndex);
    }

    private void OnClickToggleAssign(int slotIndex)
    {
        var workshop = GameInstance.Model.Workshop;
        var slot = workshop.Slots[slotIndex];

        if (slot.IngredientTid.Value > 0)
        {
            workshop.UnassignWorker(slotIndex);
        }
        else
        {
            if (mCandidateIngredientTids.Count == 0)
                return;

            int ingredientTid = mCandidateIngredientTids[mSlotCandidateIndices[slotIndex]];
            if (!workshop.AssignWorker(slotIndex, ingredientTid))
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

        if (slot.IngredientTid.Value > 0)
        {
            mSlotViews[slotIndex].SetStatusText($"생산 중: {GetIngredientName(slot.IngredientTid.Value)} (해제)");
        }
        else
        {
            string candidateName = mCandidateIngredientTids.Count > 0
                ? GetIngredientName(mCandidateIngredientTids[mSlotCandidateIndices[slotIndex]])
                : "-";
            mSlotViews[slotIndex].SetStatusText($"비어있음 (선택: {candidateName})");
        }
    }

    private string GetIngredientName(int tid)
    {
        var drinkRow = GameInstance.Table.GetTable<CTable.DrinkIngredientRow>()?.Get(tid);
        if (drinkRow != null)
            return drinkRow.Name;

        var breadRow = GameInstance.Table.GetTable<CTable.BreadIngredientRow>()?.Get(tid);
        return breadRow?.Name ?? tid.ToString();
    }
}
