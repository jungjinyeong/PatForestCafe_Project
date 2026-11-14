using System.Collections.Generic;
using System.Linq;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Extension;

// 로비 오른쪽 패널의 창고 화면(UI_Root_Lobby/Sidebar/ContentSlot/WarehouseView). 오른쪽 세로 탭 [창고]가 켠다.
// 필터(전체 / 재료 / 디저트) + 4열 그리드. 폭이 좁아 상세 페이지 대신 아래 한 줄에 고른 품목 이름·수량을 보여 준다.
// 창고 1개 기준(창고1/2/+ 탭은 용량·확장 기획이 정해지면 추가). UILobbySidePanel.Init()에서 Init()을 호출한다.
public class UISideWarehouseView : MonoBehaviour
{
    private static readonly string[] FILTER_NAMES = { "전체", "재료", "디저트" };

    [Header("Filter (전체 / 재료 / 디저트 순)")]
    [SerializeField] private UIButtonEx[] mFilterButtons;
    [SerializeField] private Image[] mFilterBgs;
    [SerializeField] private TextMeshProUGUI[] mFilterTexts;
    [SerializeField] private Color mFilterOnColor = new Color(0.45f, 0.62f, 0.35f);
    [SerializeField] private Color mFilterOffColor = Color.white;
    [SerializeField] private Color mFilterTextOnColor = Color.white;
    [SerializeField] private Color mFilterTextOffColor = new Color(0.35f, 0.22f, 0.12f);

    [Header("Grid")]
    [SerializeField] private Transform mGridContent;
    [SerializeField] private UIWarehouseCell mCellTemplate;
    [SerializeField] private TextMeshProUGUI mTextEmpty;
    [Tooltip("고른 품목 이름·수량 한 줄. 비어 있어도 된다.")]
    [SerializeField] private TextMeshProUGUI mTextSelected;

    private readonly List<UIWarehouseCell> mCells = new List<UIWarehouseCell>();
    private readonly CompositeDisposable mWatchDisposables = new CompositeDisposable();
    private WarehouseEntries.eFilter mFilter = WarehouseEntries.eFilter.All;
    private UIWarehouseCellData mSelected;
    private bool mIsInitialized;

    private void OnEnable()
    {
        if (mIsInitialized)
            StartWatching();
    }

    private void OnDisable()
    {
        mWatchDisposables.Clear();
    }

    private void OnDestroy()
    {
        mWatchDisposables.Dispose();
    }

    public void Init()
    {
        if (mIsInitialized) return;
        mIsInitialized = true;

        if (mCellTemplate != null)
            mCellTemplate.gameObject.SetActive(false);

        // 버튼 수가 필터 종류보다 많아도(창고 탭 추가 등) 넘치는 버튼은 건너뛴다.
        int count = Mathf.Min(mFilterButtons.Length, FILTER_NAMES.Length);
        for (int i = 0; i < count; i++)
        {
            if (mFilterButtons[i] == null) continue;
            var filter = (WarehouseEntries.eFilter)i;
            mFilterButtons[i].OnSubscribeOnClick(() => SelectFilter(filter)).AddTo(this);
            if (i < mFilterTexts.Length && mFilterTexts[i] != null)
                mFilterTexts[i].SetTextEx(FILTER_NAMES[i]);
        }

        if (isActiveAndEnabled)
            StartWatching();
    }

    // 켜져 있는 동안만 수량 변화를 구독한다(가공섬 생산/오븐 보관 등).
    private void StartWatching()
    {
        mWatchDisposables.Clear();
        WarehouseEntries.ObserveChanges()
            .Subscribe(_ => Refresh())
            .AddTo(mWatchDisposables);
        Refresh();
    }

    private void SelectFilter(WarehouseEntries.eFilter filter)
    {
        mFilter = filter;
        mSelected = null;
        Refresh();
    }

    private void OnSelectCell(UIWarehouseCell cell)
    {
        mSelected = cell.Data;
        RefreshSelection();
    }

    private void Refresh()
    {
        for (int i = 0; i < mFilterButtons.Length; i++)
        {
            bool on = i == (int)mFilter;
            if (i < mFilterBgs.Length && mFilterBgs[i] != null)
                mFilterBgs[i].color = on ? mFilterOnColor : mFilterOffColor;
            if (i < mFilterTexts.Length && mFilterTexts[i] != null)
                mFilterTexts[i].color = on ? mFilterTextOnColor : mFilterTextOffColor;
        }

        var list = WarehouseEntries.Get(mFilter);
        RebuildGrid(list);

        if (mTextEmpty != null)
        {
            mTextEmpty.gameObject.SetActive(list.Count == 0);
            mTextEmpty.SetTextEx(mFilter == WarehouseEntries.eFilter.Desserts
                ? "보관 중인 디저트가 없어요"
                : mFilter == WarehouseEntries.eFilter.Ingredients ? "보관 중인 재료가 없어요" : "창고가 비어 있어요");
        }

        // 고른 품목이 사라졌거나 수량이 바뀌었으면 새 값으로 다시 잡는다. 재료와 디저트는 Tid가 겹칠 수 있어 종류도 같이 비교한다.
        mSelected = mSelected == null ? null : list.FirstOrDefault(e => e.Tid == mSelected.Tid && e.IsDessert == mSelected.IsDessert);
        RefreshSelection();
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

    private void RefreshSelection()
    {
        foreach (var cell in mCells)
        {
            bool isSelected = mSelected != null && cell.gameObject.activeSelf && cell.Data != null
                && cell.Data.Tid == mSelected.Tid && cell.Data.IsDessert == mSelected.IsDessert;
            cell.SetSelected(isSelected);
        }

        if (mTextSelected != null)
            mTextSelected.SetTextEx(mSelected != null ? $"{mSelected.Name} ×{mSelected.Count}" : string.Empty);
    }
}
