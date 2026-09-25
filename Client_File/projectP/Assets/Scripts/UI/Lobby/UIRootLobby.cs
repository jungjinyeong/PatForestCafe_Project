using UniRx;
using UnityEngine;
using Extension;

public class UIRootLobby : UIWndBase
{
    public struct Param { }

    [SerializeField] private WaypointGroup[] mWaypointGroups;

    [Header("Placement")]
    [SerializeField] private UIButtonEx mBtnTogglePlacementMode;

    [Header("Floor")]
    [SerializeField] private LobbyFloorCameraController mFloorCamera;
    [SerializeField] private UIButtonEx mBtnFloorUnlock;

    [Header("Menu")]
    [SerializeField] private UIButtonEx mBtnRecipeBook;
    [SerializeField] private UIButtonEx mBtnUpgrade;

    [Header("Menu (Placeholder)")]
    [SerializeField] private UIButtonEx mBtnWarehouse;
    [SerializeField] private UIButtonEx mBtnStaff;

    [Header("Shop")]
    [SerializeField] private UIButtonEx mBtnShop;

    public override eUIType GetUIType() => eUIType.RootLobby;

    public override void Init()
    {
        base.Init();

        RegisterWaypointGroups();

        this.GetComponentsInChildren<UIDayNightBg>().Each(x => x.Init());

        // 가구 구매 시 "지금 보고 있는 층"(FloorModel.ViewingFloor)을 알아야 하므로 가구 복원/배치보다 먼저 초기화한다.
        if (mFloorCamera != null)
            mFloorCamera.Init();

        GameInstance.Model.Placement.RespawnSavedFurniture();
        SubscribePlacementPopups();

        mBtnTogglePlacementMode.OnSubscribeOnClick(OnClickTogglePlacementMode).AddTo(this);

        // mBtnFloorUnlock/mBtnRecipeBook/mBtnUpgrade는 UI_Root_Lobby 프리팹에 버튼을 배치하기 전까지 비어있을 수 있음.
        if (mBtnFloorUnlock != null)
            mBtnFloorUnlock.OnSubscribeOnClick(OnClickOpenFloorUnlock).AddTo(this);

        if (mBtnRecipeBook != null)
            mBtnRecipeBook.OnSubscribeOnClick(OnClickOpenRecipeBook).AddTo(this);

        if (mBtnUpgrade != null)
            mBtnUpgrade.OnSubscribeOnClick(OnClickOpenUpgrade).AddTo(this);

        if (mBtnWarehouse != null)
            mBtnWarehouse.OnSubscribeOnClick(OnClickOpenWarehouse).AddTo(this);

        if (mBtnStaff != null)
            mBtnStaff.OnSubscribeOnClick(OnClickOpenStaff).AddTo(this);

        if (mBtnShop != null)
            mBtnShop.OnSubscribeOnClick(OnClickOpenShopStreet).AddTo(this);
    }

    private void OnClickTogglePlacementMode()
    {
        GameInstance.Model.Placement.ToggleEditMode();
    }

    // 배치 모드 → 가구배치 목록 팝업, 드래그 배치 중 → 확정/취소 팝업. 둘 다 PlacementModel 상태에 맞춰 열고 닫는다.
    private void SubscribePlacementPopups()
    {
        var placement = GameInstance.Model.Placement;

        placement.IsEditMode
            .Subscribe(isEditMode => SetPopupOpen<UIPopupFurnitureList, UIPopupFurnitureList.Param>(eUIType.PopupFurnitureList, isEditMode))
            .AddTo(this);

        placement.IsPlacing
            .Subscribe(isPlacing => SetPopupOpen<UIPopupPlacementConfirm, UIPopupPlacementConfirm.Param>(eUIType.PopupPlacementConfirm, isPlacing))
            .AddTo(this);
    }

    private void SetPopupOpen<T, TParam>(eUIType uiType, bool open) where T : UIWndBase where TParam : struct
    {
        if (open)
            GameInstance.UI.Open<T, TParam>(uiType, default);
        else
            GameInstance.UI.Close(uiType);
    }

    private void OnClickOpenFloorUnlock()
    {
        GameInstance.UI.Open<UIFloorUnlock, UIFloorUnlock.Param>(eUIType.PopupFloorUnlock, new UIFloorUnlock.Param());
    }

    private void OnClickOpenRecipeBook()
    {
        GameInstance.UI.Open<UIPopupRecipeBook, UIPopupRecipeBook.Param>(eUIType.PopupRecipeBook, new UIPopupRecipeBook.Param());
    }

    private void OnClickOpenUpgrade()
    {
        GameInstance.UI.Open<UIPopupUpgrade, UIPopupUpgrade.Param>(eUIType.PopupUpgrade, new UIPopupUpgrade.Param());
    }

    private void OnClickOpenWarehouse()
    {
        GameInstance.UI.Open<UIPopupWarehouse, UIPopupWarehouse.Param>(eUIType.PopupWarehouse, new UIPopupWarehouse.Param());
    }

    private void OnClickOpenStaff()
    {
        GameInstance.UI.Open<UIPopupStaff, UIPopupStaff.Param>(eUIType.PopupStaff, new UIPopupStaff.Param());
    }

    private void OnClickOpenShopStreet()
    {
        GameInstance.UI.Open<UIPopupShopStreet, UIPopupShopStreet.Param>(eUIType.PopupShopStreet, new UIPopupShopStreet.Param());
    }

    public override void Open()
    {
        base.Open();
    }

    public override void Destroy()
    {
        base.Destroy();

    }

    private void RegisterWaypointGroups()
    {
        if (mWaypointGroups == null)
            return;

        foreach (var group in mWaypointGroups)
        {
            if (group == null) continue;

            group.Init();
            GameInstance.WayPoint.RegisterWaypointGroup(group);
        }
    }
}
