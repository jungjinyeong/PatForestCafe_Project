using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;

// 오븐 재료 선택 모달. 기본 재료 칸은 같은 분류(Category) 안에서 등급만 교체하고, 추가 재료 칸은 모든 빵 재료를 고른다.
// 창고(UIPopupWarehouse)처럼 왼쪽 재료 그리드(UIWarehouseCell) + 오른쪽 선택 재료 상세, [이 재료 사용]으로 확정한다.
public class UIBreadIngredientPicker : UIBase
{
    [SerializeField] private TextMeshProUGUI mTextTitle;
    [SerializeField] private TextMeshProUGUI mTextNote;
    [SerializeField] private UIButtonEx mBtnClear;
    [SerializeField] private UIButtonEx mBtnClose;
    [SerializeField] private UIButtonEx mBtnShade;

    [Header("Grid")]
    [SerializeField] private Transform mGridContent;
    [SerializeField] private UIWarehouseCell mCellTemplate;

    [Header("Detail")]
    [SerializeField] private GameObject mDetailRoot;
    [SerializeField] private TextMeshProUGUI mTextGlyph;
    [SerializeField] private TextMeshProUGUI mTextName;
    [SerializeField] private TextMeshProUGUI mTextGrade;
    [SerializeField] private TextMeshProUGUI mTextCount;
    [SerializeField] private TextMeshProUGUI mTextTags;
    [SerializeField] private TextMeshProUGUI mTextDesc;
    [SerializeField] private UIButtonEx mBtnUse;

    private readonly List<UIWarehouseCell> mCells = new List<UIWarehouseCell>();
    private Action<int> mOnPick;
    private int mSelectedTid;

    public void Init(Action<int> onPick, Action onClear)
    {
        mOnPick = onPick;

        if (mCellTemplate != null)
            mCellTemplate.gameObject.SetActive(false);

        mBtnUse.OnSubscribeOnClick(OnClickUse).AddTo(this);
        mBtnClear.OnSubscribeOnClick(onClear).AddTo(this);
        mBtnClose.OnSubscribeOnClick(Close).AddTo(this);
        if (mBtnShade != null)
            mBtnShade.OnSubscribeOnClick(Close).AddTo(this);

        Deative();
    }

    // ingredientTids 순서대로 그리드를 채우고 selectedTid(현재 칸의 재료)를 먼저 펼친다.
    public void Open(string title, string note, IEnumerable<int> ingredientTids, int selectedTid, bool canClear)
    {
        Active();

        mTextTitle.SetTextEx(title);
        mTextNote.SetTextEx(note);
        mBtnClear.gameObject.SetActive(canClear);

        var list = ingredientTids.Select(ToCellData).Where(d => d != null).ToList();
        RebuildGrid(list);
        Select(list.FirstOrDefault(d => d.Tid == selectedTid) ?? list.FirstOrDefault());
    }

    public void Close()
    {
        Deative();
    }

    private void OnSelectCell(UIWarehouseCell cell)
    {
        Select(cell.Data);
    }

    private void OnClickUse()
    {
        if (mSelectedTid != 0)
            mOnPick?.Invoke(mSelectedTid);
    }

    private static UIWarehouseCellData ToCellData(int tid)
    {
        var row = GameInstance.Table.Get<CTable.BreadIngredientRow>(tid);
        if (row == null)
            return null;

        return new UIWarehouseCellData
        {
            Tid = tid,
            Name = row.Name,
            Count = GameInstance.Model.Ingredient.Get(tid)?.Count.Value ?? 0,
            Badge = BreadQuality.GetGradeText(row.Grade),
        };
    }

    private void RebuildGrid(List<UIWarehouseCellData> list)
    {
        while (mCells.Count < list.Count)
        {
            var cell = Instantiate(mCellTemplate, mGridContent);
            cell.Bind(OnSelectCell);
            mCells.Add(cell);
        }

        for (int i = 0; i < mCells.Count; i++)
        {
            bool active = i < list.Count;
            mCells[i].gameObject.SetActive(active);
            if (active)
                mCells[i].SetData(list[i]);
        }
    }

    private void Select(UIWarehouseCellData data)
    {
        mSelectedTid = data?.Tid ?? 0;

        foreach (var cell in mCells)
            cell.SetSelected(cell.gameObject.activeSelf && cell.Data != null && cell.Data.Tid == mSelectedTid);

        if (mDetailRoot != null)
            mDetailRoot.SetActive(data != null);
        mBtnUse.interactable = data != null;
        if (data == null)
            return;

        var row = GameInstance.Table.Get<CTable.BreadIngredientRow>(data.Tid);
        string grade = BreadQuality.GetGradeText(row.Grade);

        mTextGlyph.SetTextEx(string.IsNullOrEmpty(data.Name) ? "?" : data.Name.Substring(0, 1));
        mTextName.SetTextEx(data.Name);
        mTextGrade.SetTextEx(string.IsNullOrEmpty(grade) ? $"{row.Category}" : $"{row.Category} · {grade}");
        mTextCount.SetTextEx($"보유 {data.Count}개");
        mTextTags.SetTextEx(string.Join(", ", row.GetTags()));
        mTextDesc.SetTextEx(row.Desc);
    }
}
