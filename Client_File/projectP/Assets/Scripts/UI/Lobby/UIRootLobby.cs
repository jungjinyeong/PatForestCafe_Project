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
    }

    private void OnClickTogglePlacementMode()
    {
        GameInstance.Model.Placement.ToggleEditMode();
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
