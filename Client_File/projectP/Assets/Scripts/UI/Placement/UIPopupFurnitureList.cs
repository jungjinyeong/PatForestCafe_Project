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
            mCategoryTabs.Init(_ => RefreshList());

        if (mBtnResetPlacement != null)
            mBtnResetPlacement.OnSubscribeOnClick(OnClickReset).AddTo(this);
    }

    public override void Open()
    {
        base.Open();

        if (mCategoryTabs != null)
            mCategoryTabs.Select(CTable.eFurnitureCategory.All);
        else
            RefreshList();
    }

    public void Set(Param param)
    {
    }

    private void RefreshList()
    {
        var category = mCategoryTabs != null ? mCategoryTabs.Selected : CTable.eFurnitureCategory.All;
        mScrollEx.SetData(GameInstance.Model.Placement.BuildFurnitureShopData(category));
        // 탭을 바꾸거나 다시 열면 목록 처음부터 보이게 한다.
        mScrollEx.ScrollToTop();
    }

    private void OnClickAdd(UIScrollRow row)
    {
        if (row is not UIScrollFurniture furnitureRow || furnitureRow.CurrentData == null)
            return;

        GameInstance.Model.Placement.TryPurchaseAndBeginPlacement(furnitureRow.CurrentData);
    }

    private void OnClickReset()
    {
        GameInstance.Model.Placement.ResetAllPlacedFurniture();
    }
}
