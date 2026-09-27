using System.Collections.Generic;
using System.Linq;
using TMPro;
using UniRx;
using UnityEngine;
using Extension;

// 창고(프리팹 UI_Popup_Warehouse, 로비 하단 [창고]). ForestCafe UI Lab 창고 시안 기준.
// 왼쪽: 분류 탭(가공섬 재료 / 디저트) + 보관 품목 그리드. 오른쪽: 선택한 품목 상세.
// - 가공섬 재료 = MaterialModel(음료 재료 + 빵 재료) 중 보유 수량이 있는 것.
// - 디저트 = 오븐에서 구워 창고에 보관 중인(진열 전) 빵 재고(BreadData.ProducedCount).
// 보관 용량/확장은 기획 미정이라 아직 없다(총 보관 수량만 표시).
public class UIPopupWarehouse : UIWndBase, IUIParam<UIPopupWarehouse.Param>
{
    public struct Param
    {
    }

    private enum eTab
    {
        Materials,
        Desserts,
    }

    private static readonly string[] TAB_NAMES = { "가공섬 재료", "디저트" };

    [Header("Tabs")]
    [SerializeField] private UIButtonEx[] mTabButtons;
    [SerializeField] private TextMeshProUGUI[] mTabTexts;
    [SerializeField] private GameObject[] mTabSelectedMarks;
    [SerializeField] private TextMeshProUGUI mTextTotal;

    [Header("Grid")]
    [SerializeField] private Transform mGridContent;
    [SerializeField] private UIWarehouseCell mCellTemplate;
    [SerializeField] private TextMeshProUGUI mTextEmpty;

    [Header("Detail")]
    [SerializeField] private GameObject mDetailRoot;
    [SerializeField] private TextMeshProUGUI mTextName;
    [SerializeField] private TextMeshProUGUI mTextCategoryStamp;
    [SerializeField] private TextMeshProUGUI mTextGlyph;
    [SerializeField] private TextMeshProUGUI mTextDesc;
    [SerializeField] private GameObject[] mInfoRoots;
    [SerializeField] private TextMeshProUGUI[] mInfoLabels;
    [SerializeField] private TextMeshProUGUI[] mInfoValues;

    private readonly List<UIWarehouseCell> mCells = new List<UIWarehouseCell>();
    private readonly CompositeDisposable mCountDisposables = new CompositeDisposable();
    private eTab mTab = eTab.Materials;
    private int mSelectedTid;

    public override eUIType GetUIType() => eUIType.PopupWarehouse;

    public override void Init()
    {
        base.Init();

        if (mCellTemplate != null)
            mCellTemplate.gameObject.SetActive(false);

        for (int i = 0; i < mTabButtons.Length; i++)
        {
            var tab = (eTab)i;
            mTabButtons[i].OnSubscribeOnClick(() => SelectTab(tab)).AddTo(this);
        }
    }

    public override void Open()
    {
        base.Open();

        mSelectedTid = 0;
        SubscribeCounts();
        SelectTab(eTab.Materials);
    }

    public void Set(Param param)
    {
    }

    private void OnDisable()
    {
        mCountDisposables.Clear();
    }

    private void OnDestroy()
    {
        mCountDisposables.Dispose();
    }

    private void OnSelectCell(UIWarehouseCell cell)
    {
        SelectEntry(cell.Data);
    }

    // 열려 있는 동안 공방 생산/오븐 보관 등으로 수량이 바뀌면 현재 탭을 다시 그린다.
    private void SubscribeCounts()
    {
        mCountDisposables.Clear();

        var changes = GameInstance.Model.Material.GetAll().Select(m => m.Count.Skip(1).AsUnitObservable())
            .Concat(GameInstance.Model.Bread.GetAll().Select(b => b.ProducedCount.Skip(1).AsUnitObservable()));

        Observable.Merge(changes)
            .ThrottleFrame(1)
            .Subscribe(_ => Refresh())
            .AddTo(mCountDisposables);
    }

    private List<UIWarehouseCellData> GetEntries(eTab tab)
    {
        if (tab == eTab.Desserts)
        {
            return GameInstance.Model.Bread.GetAll()
                .Where(b => b.ProducedCount.Value > 0)
                .OrderBy(b => b.TId)
                .Select(b => new UIWarehouseCellData { Tid = b.TId, IsDessert = true, Name = b.MenuItemRow?.Name ?? b.TId.ToString(), Count = b.ProducedCount.Value })
                .ToList();
        }

        return GameInstance.Model.Material.GetAll()
            .Where(m => m.Count.Value > 0)
            .OrderBy(m => m.Tid)
            .Select(m => new UIWarehouseCellData { Tid = m.Tid, IsDessert = false, Name = m.Name, Count = m.Count.Value })
            .ToList();
    }

    private void SelectTab(eTab tab)
    {
        mTab = tab;
        mSelectedTid = 0;
        Refresh();
    }

    private void Refresh()
    {
        int total = 0;
        for (int i = 0; i < mTabButtons.Length; i++)
        {
            var entries = GetEntries((eTab)i);
            int sum = entries.Sum(e => e.Count);
            total += sum;
            if (i < mTabTexts.Length)
                mTabTexts[i].SetTextEx($"{TAB_NAMES[i]} <size=80%>{entries.Count}</size>");
            if (i < mTabSelectedMarks.Length)
                mTabSelectedMarks[i].SetActive(i == (int)mTab);
        }
        mTextTotal.SetTextEx($"총 보관 {total}개");

        var list = GetEntries(mTab);
        RebuildGrid(list);
        mTextEmpty.gameObject.SetActive(list.Count == 0);
        mTextEmpty.SetTextEx(mTab == eTab.Desserts
            ? "보관 중인 디저트가 없어요.\n오븐에서 구운 빵이 이곳에 자동으로 보관됩니다."
            : "보관 중인 재료가 없어요.\n가공섬에서 재료를 모아 보세요.");

        // 선택 중인 품목이 사라졌으면 첫 항목을 펼친다.
        SelectEntry(list.FirstOrDefault(e => e.Tid == mSelectedTid) ?? list.FirstOrDefault());
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

    private void SelectEntry(UIWarehouseCellData entry)
    {
        mSelectedTid = entry?.Tid ?? 0;

        foreach (var cell in mCells)
            cell.SetSelected(cell.gameObject.activeSelf && cell.Data != null && cell.Data.Tid == mSelectedTid);

        RefreshDetail(entry);
    }

    private void RefreshDetail(UIWarehouseCellData entry)
    {
        if (mDetailRoot != null)
            mDetailRoot.SetActive(entry != null);
        if (entry == null)
            return;

        mTextName.SetTextEx(entry.Name);
        mTextGlyph.SetTextEx(string.IsNullOrEmpty(entry.Name) ? "?" : entry.Name.Substring(0, 1));

        var infos = new List<(string label, string value)>();
        string category, desc;
        IEnumerable<string> tags;

        if (entry.IsDessert)
        {
            var bread = GameInstance.Model.Bread.Get(entry.Tid);
            var row = GameInstance.Table.Get<CTable.BreadRow>(entry.Tid);
            category = "디저트";
            desc = row?.Desc ?? string.Empty;
            tags = row.GetTags();
            infos.Add(("분류", "오븐 완성 디저트"));
            infos.Add(("보관 수량", $"{entry.Count}개"));
            if (bread != null)
            {
                var qualities = BreadQuality.HIGH_TO_LOW
                    .Where(q => bread.GetProduced(q) > 0)
                    .Select(q => $"<color={BreadQuality.GetColorHex(q)}>{BreadQuality.GetName(q)}</color> {bread.GetProduced(q)}");
                infos.Add(("품질", string.Join(" · ", qualities)));
            }
        }
        else
        {
            var breadMaterial = GameInstance.Table.GetTable<CTable.BreadMaterialRow>()?.Get(entry.Tid);
            if (breadMaterial != null)
            {
                var quality = BreadQuality.FromGrade(breadMaterial.Grade);
                category = "빵 재료";
                desc = breadMaterial.Desc;
                tags = breadMaterial.GetTags();
                infos.Add(("분류", $"빵 재료 · {breadMaterial.Category}"));
                infos.Add(("보관 수량", $"{entry.Count}개"));
                infos.Add(("등급", $"<color={BreadQuality.GetColorHex(quality)}>{BreadQuality.GetName(quality)}</color>"));
            }
            else
            {
                var drinkMaterial = GameInstance.Table.GetTable<CTable.DrinkMaterialRow>()?.Get(entry.Tid);
                category = "음료 재료";
                desc = drinkMaterial?.Desc ?? string.Empty;
                tags = drinkMaterial.GetTags();
                infos.Add(("분류", "음료 재료"));
                infos.Add(("보관 수량", $"{entry.Count}개"));
            }
        }

        var tagList = tags?.ToList() ?? new List<string>();
        if (tagList.Count > 0)
            infos.Add(("속성", string.Join(", ", tagList)));

        mTextCategoryStamp.SetTextEx(category);
        mTextDesc.SetTextEx(string.IsNullOrEmpty(desc) ? (entry.IsDessert ? "오븐으로 만든 완성 디저트입니다." : "가공섬에서 생산한 재료입니다.") : desc);

        for (int i = 0; i < mInfoRoots.Length; i++)
        {
            bool active = i < infos.Count;
            mInfoRoots[i].SetActive(active);
            if (!active) continue;
            mInfoLabels[i].SetTextEx(infos[i].label);
            mInfoValues[i].SetTextEx(infos[i].value);
        }
    }
}
