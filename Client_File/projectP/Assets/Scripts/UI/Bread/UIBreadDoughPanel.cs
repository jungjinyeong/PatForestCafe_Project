using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;

// 오븐 트레이 "반죽 설정" 모달(UIPopupBreadProduction 내부 패널).
// 좌: 발견한 빵 레시피 / 중: 기본 재료 5칸(레시피 재료, 같은 분류 안에서 등급 교체) + 추가 재료 5칸(속성) /
// 우: 선택 레시피·예상 품질(기본 재료 등급 평균)·레시피/추가 재료 속성·제작 수량.
public class UIBreadDoughPanel : UIBase
{
    // 수량 버튼 순서: MIN, -10, -5, -1, +1, +5, +10, MAX
    private static readonly int[] QUANTITY_DELTAS = { int.MinValue, -10, -5, -1, 1, 5, 10, int.MaxValue };

    [SerializeField] private TextMeshProUGUI mTextTitle;

    [Header("Recipes")]
    [SerializeField] private UIScrollEx mRecipeScrollEx;
    [SerializeField] private GameObject mRecipeRowPrefab;

    [Header("Materials")]
    [SerializeField] private UIBreadMaterialSlot[] mBaseSlots;
    [SerializeField] private UIBreadMaterialSlot[] mExtraSlots;
    [SerializeField] private UIBreadMaterialPicker mPicker;

    [Header("Info")]
    [SerializeField] private TextMeshProUGUI mTextRecipeName;
    [SerializeField] private TextMeshProUGUI mTextRecipeDesc;
    [SerializeField] private TextMeshProUGUI mTextExpectedQuality;
    [SerializeField] private TextMeshProUGUI mTextRecipeTags;
    [SerializeField] private TextMeshProUGUI mTextExtraTags;
    [SerializeField] private TextMeshProUGUI mTextQuantity;
    [SerializeField] private UIButtonEx[] mQuantityButtons;

    [Header("Actions")]
    [SerializeField] private UIButtonEx mBtnSave;
    [SerializeField] private UIButtonEx mBtnClear;
    [SerializeField] private UIButtonEx mBtnClose;

    private Action mOnChanged;
    private int mTrayIndex;
    private int mRecipeTid;
    private int mQuantity = 1;
    private bool mEditingBase;
    private int mEditingIndex = -1;

    private OvenModel Oven => GameInstance.Model.Oven;

    public void Init(Action onChanged)
    {
        mOnChanged = onChanged;

        mRecipeScrollEx.Init(mRecipeRowPrefab);
        mRecipeScrollEx.SetOnSelect(OnSelectRecipe);

        for (int i = 0; i < mBaseSlots.Length; i++)
        {
            int index = i;
            mBaseSlots[i].Init(() => OnClickSlot(true, index));
        }

        for (int i = 0; i < mExtraSlots.Length; i++)
        {
            int index = i;
            mExtraSlots[i].Init(() => OnClickSlot(false, index));
        }

        mPicker.Init(OnPickMaterial, OnClearMaterial);

        for (int i = 0; i < mQuantityButtons.Length && i < QUANTITY_DELTAS.Length; i++)
        {
            int delta = QUANTITY_DELTAS[i];
            mQuantityButtons[i].OnSubscribeOnClick(() => ChangeQuantity(delta)).AddTo(this);
        }

        mBtnSave.OnSubscribeOnClick(OnClickSave).AddTo(this);
        mBtnClear.OnSubscribeOnClick(OnClickClearTray).AddTo(this);
        mBtnClose.OnSubscribeOnClick(Close).AddTo(this);

        Deative();
    }

    // 트레이의 현재 설정을 불러와 연다. 비어 있으면 첫 번째 발견 레시피로 시작한다.
    public void Open(int trayIndex)
    {
        mTrayIndex = trayIndex;
        Active();
        mPicker.Close();

        mTextTitle.SetTextEx($"트레이 {trayIndex + 1} - 반죽 설정");

        var recipes = GetDiscoveredBreadTids();
        mRecipeScrollEx.SetData(recipes.Select(tid => new UIScrollBreadRecipeData { BreadTid = tid }).ToList());

        var tray = Oven.Trays[trayIndex];
        if (!tray.IsEmpty)
        {
            mRecipeTid = tray.RecipeTid;
            mQuantity = tray.Quantity;
            FillSlots(mBaseSlots, tray.BaseMaterialTids);
            FillSlots(mExtraSlots, tray.ExtraMaterialTids);
        }
        else
        {
            SelectRecipe(recipes.Count > 0 ? recipes[0] : 0);
            mQuantity = 1;
            FillSlots(mExtraSlots, null);
        }

        mBtnClear.interactable = !tray.IsEmpty;
        Refresh();
    }

    public void Close()
    {
        mPicker.Close();
        Deative();
    }

    #region Recipe

    private static List<int> GetDiscoveredBreadTids()
    {
        var breadTable = GameInstance.Table.GetTable<CTable.BreadRow>();
        return GameInstance.Model.RecipeBook.GetDiscoveredTids()
            .Where(tid => breadTable != null && breadTable.Get(tid) != null)
            .OrderBy(tid => tid)
            .ToList();
    }

    private void OnSelectRecipe(UIScrollRow row)
    {
        if (row is not UIScrollBreadRecipe recipeRow || recipeRow.CurrentData == null)
            return;

        SelectRecipe(recipeRow.CurrentData.BreadTid);
        Refresh();
    }

    // 레시피를 고르면 기본 재료 칸을 레시피 재료(하급)로 채운다. 등급은 칸을 눌러 같은 분류 안에서 교체한다.
    private void SelectRecipe(int breadTid)
    {
        mRecipeTid = breadTid;
        FillSlots(mBaseSlots, GameInstance.Table.Get<CTable.BreadRow>(breadTid).GetMaterialTids());
        mPicker.Close();
    }

    #endregion

    #region Materials

    private void OnClickSlot(bool isBase, int index)
    {
        mEditingBase = isBase;
        mEditingIndex = index;

        var slot = GetEditingSlot();
        if (slot == null)
            return;

        var table = GameInstance.Table.GetTable<CTable.BreadMaterialRow>();
        if (table == null)
            return;

        if (isBase)
        {
            // 기본 재료는 레시피가 정한 분류 안에서 등급만 바꿀 수 있다.
            var current = table.Get(slot.MaterialTid);
            if (current == null)
                return;

            var rows = table.All.Values.Where(r => r.Category == current.Category).OrderBy(r => r.Grade);
            mPicker.Open("재료 선택", $"{current.Category}류만 교체할 수 있어요.", ToPickerData(rows), false);
        }
        else
        {
            var rows = table.All.Values.OrderBy(r => r.Category).ThenBy(r => r.Grade);
            mPicker.Open("재료 선택", "추가 재료를 골라 주세요. 속성이 더해져요.", ToPickerData(rows), slot.IsFilled);
        }
    }

    private static List<UIScrollBreadMaterialData> ToPickerData(IEnumerable<CTable.BreadMaterialRow> rows)
    {
        return rows.Select(r => new UIScrollBreadMaterialData
        {
            Tid = r.Tid,
            OwnedCount = GameInstance.Model.Material.Get(r.Tid)?.Count.Value ?? 0,
        }).ToList();
    }

    private void OnPickMaterial(int materialTid)
    {
        GetEditingSlot()?.Set(materialTid);
        mPicker.Close();
        Refresh();
    }

    private void OnClearMaterial()
    {
        if (!mEditingBase)
            GetEditingSlot()?.Set(0);

        mPicker.Close();
        Refresh();
    }

    private UIBreadMaterialSlot GetEditingSlot()
    {
        var slots = mEditingBase ? mBaseSlots : mExtraSlots;
        return mEditingIndex >= 0 && mEditingIndex < slots.Length ? slots[mEditingIndex] : null;
    }

    private static void FillSlots(UIBreadMaterialSlot[] slots, IList<int> tids)
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i].Set(tids != null && i < tids.Count ? tids[i] : 0);
    }

    #endregion

    private void ChangeQuantity(int delta)
    {
        if (delta == int.MinValue) mQuantity = 1;
        else if (delta == int.MaxValue) mQuantity = Oven.MaxQuantity;
        else mQuantity = Mathf.Clamp(mQuantity + delta, 1, Oven.MaxQuantity);

        Refresh();
    }

    private void Refresh()
    {
        var breadRow = GameInstance.Table.Get<CTable.BreadRow>(mRecipeTid);
        var menuRow = GameInstance.Table.Get<CTable.MenuItemRow>(mRecipeTid);

        for (int i = 0; i < mRecipeScrollEx.ActiveCount; i++)
        {
            if (mRecipeScrollEx.GetRow(i) is UIScrollBreadRecipe recipeRow)
                recipeRow.SetSelected(recipeRow.CurrentData?.BreadTid == mRecipeTid);
        }

        // 레시피 재료 수보다 뒤의 기본 칸은 비활성(빈 칸).
        for (int i = 0; i < mBaseSlots.Length; i++)
            mBaseSlots[i].SetInteractable(mBaseSlots[i].IsFilled);

        var baseTids = mBaseSlots.Where(s => s.IsFilled).Select(s => s.MaterialTid).ToList();
        var extraTids = mExtraSlots.Where(s => s.IsFilled).Select(s => s.MaterialTid).ToList();
        var quality = OvenModel.CalcQuality(baseTids);

        mTextRecipeName.SetTextEx(menuRow?.Name ?? "레시피 없음");
        mTextRecipeDesc.SetTextEx(breadRow?.Desc ?? string.Empty);
        mTextExpectedQuality.SetTextEx($"예상 품질 <color={BreadQuality.GetColorHex(quality)}>{BreadQuality.GetName(quality)}</color>");
        mTextRecipeTags.SetTextEx(breadRow != null ? string.Join(", ", breadRow.GetTags()) : string.Empty);

        var materialTable = GameInstance.Table.GetTable<CTable.BreadMaterialRow>();
        var extraTags = extraTids.SelectMany(tid => materialTable?.Get(tid).GetTags() ?? Enumerable.Empty<string>()).Distinct().ToList();
        mTextExtraTags.SetTextEx(extraTags.Count > 0 ? string.Join(", ", extraTags) : "추가 재료 없음");

        mQuantity = Mathf.Clamp(mQuantity, 1, Oven.MaxQuantity);
        mTextQuantity.SetTextEx($"{mQuantity}개 <size=70%>(최대 {Oven.MaxQuantity})</size>");

        mBtnSave.interactable = mRecipeTid != 0 && baseTids.Count > 0;
    }

    private void OnClickSave()
    {
        var baseTids = mBaseSlots.Where(s => s.IsFilled).Select(s => s.MaterialTid);
        var extraTids = mExtraSlots.Where(s => s.IsFilled).Select(s => s.MaterialTid);

        if (Oven.SetTray(mTrayIndex, mRecipeTid, mQuantity, baseTids, extraTids))
        {
            Close();
            mOnChanged?.Invoke();
        }
    }

    private void OnClickClearTray()
    {
        if (Oven.ClearTray(mTrayIndex))
        {
            Close();
            mOnChanged?.Invoke();
        }
    }
}
