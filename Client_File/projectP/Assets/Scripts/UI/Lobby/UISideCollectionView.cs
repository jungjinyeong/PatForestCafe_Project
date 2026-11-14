using System.Collections.Generic;
using System.Linq;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Extension;

// 로비 오른쪽 패널의 도감 화면(UI_Root_Lobby/Sidebar/TabArea/ContentSlot/CollectionView). 오른쪽 세로 탭 [도감]이 켠다.
// 필터(전체 / 음료 / 디저트) + 발견 수 + 4열 레시피 그리드. 칸을 누르면 도감 상세 팝업(UIPopupCollection)을 그 레시피를 펼친 채로 연다.
// 발견 상태는 켤 때와 필터를 바꿀 때 다시 읽는다. UILobbySidePanel.Init()에서 Init()을 호출한다.
public class UISideCollectionView : MonoBehaviour
{
    [Header("Filter (전체 / 음료 / 디저트 순)")]
    [SerializeField] private UIButtonEx[] mFilterButtons;
    [SerializeField] private Image[] mFilterBgs;
    [SerializeField] private TextMeshProUGUI[] mFilterTexts;
    [SerializeField] private Color mFilterOnColor = new Color(0.45f, 0.62f, 0.35f);
    [SerializeField] private Color mFilterOffColor = Color.white;
    [SerializeField] private Color mFilterTextOnColor = Color.white;
    [SerializeField] private Color mFilterTextOffColor = new Color(0.35f, 0.22f, 0.12f);

    [Header("Grid")]
    [SerializeField] private TextMeshProUGUI mTextFound;
    [SerializeField] private ScrollRect mScrollRect;
    [SerializeField] private Transform mGridContent;
    [SerializeField] private UISideCollectionCell mCellTemplate;

    private readonly List<UISideCollectionCell> mCells = new List<UISideCollectionCell>();
    private readonly List<UIScrollCollectionData> mAllEntries = new List<UIScrollCollectionData>();
    private CollectionEntries.eFilter mFilter = CollectionEntries.eFilter.All;
    private bool mIsInitialized;

    private void OnEnable()
    {
        if (mIsInitialized)
            Rebuild();
    }

    public void Init()
    {
        if (mIsInitialized) return;
        mIsInitialized = true;

        if (mCellTemplate != null)
            mCellTemplate.gameObject.SetActive(false);

        int count = Mathf.Min(mFilterButtons.Length, CollectionEntries.FILTER_NAMES.Length);
        for (int i = 0; i < count; i++)
        {
            if (mFilterButtons[i] == null) continue;
            var filter = (CollectionEntries.eFilter)i;
            mFilterButtons[i].OnSubscribeOnClick(() => SelectFilter(filter)).AddTo(this);
            if (i < mFilterTexts.Length && mFilterTexts[i] != null)
                mFilterTexts[i].SetTextEx(CollectionEntries.FILTER_NAMES[i]);
        }

        if (isActiveAndEnabled)
            Rebuild();
    }

    // 발견 상태를 다시 읽고 지금 필터로 그린다(레시피 연구·구매는 다른 화면에서 일어나므로 켤 때마다).
    private void Rebuild()
    {
        mAllEntries.Clear();
        mAllEntries.AddRange(CollectionEntries.Build());
        Refresh(true);
    }

    private void SelectFilter(CollectionEntries.eFilter filter)
    {
        mFilter = filter;
        Refresh(true);
    }

    private void OnSelectCell(UISideCollectionCell cell)
    {
        if (cell.Data == null) return;
        GameInstance.UI.Open<UIPopupCollection, UIPopupCollection.Param>(eUIType.PopupCollection,
            new UIPopupCollection.Param { Filter = mFilter, SelectTid = cell.Data.Tid });
    }

    private void Refresh(bool scrollToTop)
    {
        for (int i = 0; i < mFilterButtons.Length; i++)
        {
            bool on = i == (int)mFilter;
            if (i < mFilterBgs.Length && mFilterBgs[i] != null)
                mFilterBgs[i].color = on ? mFilterOnColor : mFilterOffColor;
            if (i < mFilterTexts.Length && mFilterTexts[i] != null)
                mFilterTexts[i].color = on ? mFilterTextOnColor : mFilterTextOffColor;
        }

        var list = CollectionEntries.Filter(mAllEntries, mFilter).ToList();
        mTextFound?.SetTextEx($"발견 {list.Count(e => e.Discovered)} / {list.Count}");

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

        if (scrollToTop && mScrollRect != null)
            mScrollRect.verticalNormalizedPosition = 1f;
    }
}
