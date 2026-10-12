using UnityEngine;
using UniRx;
using Extension;

// 로비 가구배치 목록 팝업(프리팹 UI_Popup_FurnitureList). 배치 모드(PlacementModel.IsEditMode)가 켜지면
// UIRootLobby가 열고, 꺼지면 닫는다. 구매/스폰/리셋 로직은 PlacementModel+Ctrl에 있다.
// 2026-09: UI_Root_Lobby에 붙어 있던 UIFurnitureList를 팝업으로 분리(.cs.meta guid 유지).
public class UIPopupFurnitureList : UIWndBase, IUIParam<UIPopupFurnitureList.Param>
{
    public struct Param
    {
    }

    [SerializeField] private UIScrollEx mScrollEx;
    [SerializeField] private GameObject mFurnitureRowPrefab;
    [SerializeField] private UIFurnitureCategoryTabs mCategoryTabs;

    [Header("Reset")]
    [SerializeField] private UIButtonEx mBtnResetPlacement;

    public override eUIType GetUIType() => eUIType.PopupFurnitureList;

    public override void Init()
    {
        base.Init();

        mScrollEx.Init(mFurnitureRowPrefab);
        mScrollEx.SetOnSelect(OnClickAdd);

        if (mCategoryTabs != null)
            mCategoryTabs.Init(_ => RefreshList(true));

        if (mBtnResetPlacement != null)
            mBtnResetPlacement.OnSubscribeOnClick(OnClickReset).AddTo(this);

        // 구매·배치·리셋으로 보관 수가 바뀌면 열려 있는 목록의 가격/보관 표시를 다시 그린다(스크롤 위치 유지).
        GameInstance.Model.Placement.InventoryVersion
            .Skip(1)
            .Where(_ => gameObject.activeInHierarchy)
            .Subscribe(_ => RefreshList(false))
            .AddTo(this);
    }

    public override void Open()
    {
        base.Open();

        if (mCategoryTabs != null)
            mCategoryTabs.Select(CTable.eFurnitureCategory.All);
        else
            RefreshList(true);
    }

    public void Set(Param param)
    {
    }

    private void RefreshList(bool scrollToTop)
    {
        var category = mCategoryTabs != null ? mCategoryTabs.Selected : CTable.eFurnitureCategory.All;
        mScrollEx.SetData(GameInstance.Model.Placement.BuildFurnitureShopData(category));
        // 탭을 바꾸거나 다시 열면 목록 처음부터 보이게 한다.
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
