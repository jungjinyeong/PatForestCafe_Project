using UniRx;
using UnityEngine;
using Extension;

public class UIRootLobby : UIWndBase
{
    public struct Param { }

    [SerializeField] private WaypointGroup[] mWaypointGroups;

    [Header("Floor")]
    [SerializeField] private LobbyFloorCameraController mFloorCamera;
    // 월드를 둘러싼 액자. 카메라가 월드 중심을 액자 안쪽 가운데에 맞춘다.
    [SerializeField] private LobbyViewFrame mViewFrame;

    // 오른쪽 열(세로 탭 + 창고 패널 + 층 이동 목록). 메뉴 버튼(창고/상점가/직원/배치/도감/업그레이드)은 모두 여기로 옮겼다(로비 UX 개편).
    [Header("Side Panel")]
    [SerializeField] private UILobbySidePanel mSidePanel;

    public override eUIType GetUIType() => eUIType.RootLobby;

    public override void Init()
    {
        base.Init();

        RegisterWaypointGroups();

        this.GetComponentsInChildren<UIDayNightBg>().Each(x => x.Init());
        this.GetComponentsInChildren<UILobbyNotice>(true).Each(x => x.Init());

        // 가구 구매 시 "지금 보고 있는 층"(FloorModel.ViewingFloor)을 알아야 하므로 가구 복원/배치보다 먼저 초기화한다.
        if (mFloorCamera != null)
        {
            mFloorCamera.SetViewFrame(mViewFrame);
            mFloorCamera.Init();
        }

        GameInstance.Model.Placement.RespawnSavedFurniture();
        SubscribePlacementPopups();
        // 배치 모드 키 입력(R 회전, RMB 취소 등). 프리팹 편집 없이 런타임에 붙인다.
        if (GetComponent<PlacementInputController>() == null)
            gameObject.AddComponent<PlacementInputController>().Init();
        // G키 그리드 오버레이 표시(지금 보고 있는 층 영역에만). 층 영역이 모두 등록된 뒤라 여기서 초기화한다.
        if (GetComponent<PlacementGridOverlay>() == null)
            gameObject.AddComponent<PlacementGridOverlay>().Init();

        if (mSidePanel != null)
            mSidePanel.Init(mFloorCamera);
    }

    // 가구를 들고 있는 동안 → 확정/취소 팝업. 가구 목록은 오른쪽 패널의 [가구 배치] 화면(UISidePlacementView)이 맡는다.
    private void SubscribePlacementPopups()
    {
        var placement = GameInstance.Model.Placement;

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

    // 오른쪽 패널의 내용 탭을 연다(상점가 "시설 업그레이드" → 업그레이드 탭 등).
    public void ShowSideTab(UILobbySidePanel.eSideTab tab)
    {
        if (mSidePanel != null)
            mSidePanel.ShowTab(tab);
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
