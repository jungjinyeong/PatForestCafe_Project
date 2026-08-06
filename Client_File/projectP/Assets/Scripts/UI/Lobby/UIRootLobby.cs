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

    public override eUIType GetUIType() => eUIType.UIRootLobby;

    public override void Init()
    {
        base.Init();

        RegisterWaypointGroups();

        this.GetComponentsInChildren<UIDayNightBg>().Each(x => x.Init());

        // mPlacementConfirm은 별도 프리팹(UI_PlacementConfirm)이라 로비 UI에 수동으로 붙이기 전까지 비어있을 수 있음.
        if (mPlacementConfirm != null)
            mPlacementConfirm.Init();

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
