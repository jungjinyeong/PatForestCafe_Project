using UniRx;
using UnityEngine;
using Extension;

public class UIRootLobby : UIWndBase
{
    public struct Param { }

    [SerializeField] private WaypointGroup[] mWaypointGroups;

    [Header("Placement")]
    [SerializeField] private UIButtonEx mBtnTogglePlacementMode;
    [SerializeField] private UIPlacementConfirm mPlacementConfirm;
    [SerializeField] private UIFurnitureList mFurnitureList;

    [Header("Floor")]
    [SerializeField] private LobbyFloorCameraController mFloorCamera;
    [SerializeField] private UIButtonEx mBtnFloorUnlock;

    [Header("Menu")]
    [SerializeField] private UIButtonEx mBtnRecipeBook;
    [SerializeField] private UIButtonEx mBtnUpgrade;

    public override eUIType GetUIType() => eUIType.UIRootLobby;

    public override void Init()
    {
        base.Init();

        RegisterWaypointGroups();

        this.GetComponentsInChildren<UIDayNightBg>().Each(x => x.Init());

        // 가구 구매 시 "지금 보고 있는 층"을 알아야 하므로 UIFurnitureList보다 먼저 초기화한다.
        if (mFloorCamera != null)
            mFloorCamera.Init();

        // mPlacementConfirm은 별도 프리팹(UI_PlacementConfirm)이라 로비 UI에 수동으로 붙이기 전까지 비어있을 수 있음.
        if (mPlacementConfirm != null)
            mPlacementConfirm.Init();

        if (mFurnitureList != null)
        {
            mFurnitureList.Init(mFloorCamera);
            mFurnitureList.RespawnSavedFurniture();
        }

        mBtnTogglePlacementMode.OnSubscribeOnClick(OnClickTogglePlacementMode).AddTo(this);

        // mBtnFloorUnlock/mBtnRecipeBook/mBtnUpgrade는 UI_Root_Lobby 프리팹에 버튼을 배치하기 전까지 비어있을 수 있음.
        if (mBtnFloorUnlock != null)
            mBtnFloorUnlock.OnSubscribeOnClick(OnClickOpenFloorUnlock).AddTo(this);

        if (mBtnRecipeBook != null)
            mBtnRecipeBook.OnSubscribeOnClick(OnClickOpenRecipeBook).AddTo(this);

        if (mBtnUpgrade != null)
            mBtnUpgrade.OnSubscribeOnClick(OnClickOpenUpgrade).AddTo(this);
    }

    private void OnClickTogglePlacementMode()
    {
        GameInstance.Model.Placement.ToggleEditMode();
    }

    private void OnClickOpenFloorUnlock()
    {
        GameInstance.UI.Open<UIFloorUnlock, UIFloorUnlock.Param>(eUIType.UIFloorUnlock, new UIFloorUnlock.Param());
    }

    private void OnClickOpenRecipeBook()
    {
        GameInstance.UI.Open<UIPopupRecipeBook, UIPopupRecipeBook.Param>(eUIType.UIPopupRecipeBook, new UIPopupRecipeBook.Param());
    }

    private void OnClickOpenUpgrade()
    {
        GameInstance.UI.Open<UIPopupUpgrade, UIPopupUpgrade.Param>(eUIType.UIPopupUpgrade, new UIPopupUpgrade.Param());
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
