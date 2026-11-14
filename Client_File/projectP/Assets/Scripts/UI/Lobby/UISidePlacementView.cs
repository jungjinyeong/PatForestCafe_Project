using UniRx;
using UnityEngine;
using Extension;

// 로비 오른쪽 패널의 가구 배치 화면(UI_Root_Lobby/Sidebar/TabArea/ContentSlot/PlacementView). 오른쪽 세로 탭 [가구 배치]가 켠다.
// 이 화면이 보이는 동안이 배치 모드(PlacementModel.IsEditMode)다 — 켜고 끄는 건 UILobbySidePanel이 맡는다.
// 카테고리 탭(2줄) + 가구 목록(줄 전체가 버튼) + [모두 회수] / [완료]. 구매/스폰/리셋 로직은 PlacementModel+Ctrl에 있다(예전 UIPopupFurnitureList와 같다).
// UILobbySidePanel.Init()에서 Init()을 호출한다.
public class UISidePlacementView : MonoBehaviour
{
    [SerializeField] private UIScrollEx mScrollEx;
    [Tooltip("목록 줄 템플릿(UIScrollFurniture). 스크롤 Content 안에 꺼 둔다.")]
    [SerializeField] private GameObject mRowTemplate;
    [SerializeField] private UIFurnitureCategoryTabs mCategoryTabs;
    [SerializeField] private UIButtonEx mBtnReset;
    [SerializeField] private UIButtonEx mBtnDone;

    private bool mIsInitialized;

    private void OnEnable()
    {
        // 열 때마다 [전체] 탭, 목록 처음부터.
        if (mIsInitialized)
            SelectAll();
    }

    public void Init()
    {
        if (mIsInitialized) return;
        mIsInitialized = true;

        mScrollEx.Init(mRowTemplate);
        mScrollEx.SetOnSelect(OnClickAdd);

        if (mCategoryTabs != null)
            mCategoryTabs.Init(_ => RefreshList(true));

        if (mBtnReset != null)
            mBtnReset.OnSubscribeOnClick(OnClickReset).AddTo(this);
        if (mBtnDone != null)
            mBtnDone.OnSubscribeOnClick(() => GameInstance.Model.Placement.SetEditMode(false)).AddTo(this);

        // 구매·배치·리셋으로 보관 수가 바뀌면 가격/보관 표시를 다시 그린다(스크롤 위치 유지).
        GameInstance.Model.Placement.InventoryVersion
            .Skip(1)
            .Where(_ => gameObject.activeInHierarchy)
            .Subscribe(_ => RefreshList(false))
            .AddTo(this);

        if (isActiveAndEnabled)
            SelectAll();
    }

    private void SelectAll()
    {
        if (mCategoryTabs != null)
            mCategoryTabs.Select(CTable.eFurnitureCategory.All);
        else
            RefreshList(true);
    }

    private void RefreshList(bool scrollToTop)
    {
        var category = mCategoryTabs != null ? mCategoryTabs.Selected : CTable.eFurnitureCategory.All;
        mScrollEx.SetData(GameInstance.Model.Placement.BuildFurnitureShopData(category));
        if (scrollToTop)
            mScrollEx.ScrollToTop();
    }

    private void OnClickAdd(UIScrollRow row)
    {
        if (row is not UIScrollFurniture furnitureRow || furnitureRow.CurrentData == null)
            return;

        // 보관 중인 가구가 있으면 골드 없이 꺼내 놓고, 없으면 산다.
        GameInstance.Model.Placement.TryPlaceOrPurchase(furnitureRow.CurrentData);
    }

    private void OnClickReset()
    {
        GameInstance.Model.Placement.ResetAllPlacedFurniture();
    }
}
