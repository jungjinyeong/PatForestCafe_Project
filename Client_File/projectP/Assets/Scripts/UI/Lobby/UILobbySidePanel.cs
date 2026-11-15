using UniRx;
using UnityEngine;
using Extension;

// 로비 오른쪽 열(UI_Root_Lobby/Sidebar): 세로 아이콘 탭 + 내용 슬롯(ContentSlot).
// 로비 UX 개편 2차 — 내용 슬롯: 창고·업그레이드·가구 배치·직원·도감. 상점가는 하위 화면이 많아 당분간 팝업으로 둔다.
// - 내용 교체: 창고·업그레이드·가구 배치·직원·도감(슬롯, 직원·도감 상세는 팝업) / 상점가(팝업) / 의뢰(준비 중)
// - 버튼: 가공섬(화면 전환) / 층 이동(작은 목록) / 설정(준비 중)
// 가구 배치 화면이 보이는 동안 = 배치 모드(PlacementModel.IsEditMode). 다른 내용 탭으로 가면 배치 모드를 끄고,
// 배치 모드가 다른 곳에서 꺼지면(영업 시작, [완료]) 직전 내용 탭으로 돌아간다.
// UIRootLobby.Init()에서 Init()을 호출한다(GameInstance 의존 초기화 규칙).
public class UILobbySidePanel : MonoBehaviour
{
    public enum eSideTab
    {
        Warehouse,
        ShopStreet,
        Quest,
        Staff,
        Placement,
        Collection,
        IngredientIsland,
        Floor,
        Upgrade,
        Settings,
    }

    [Tooltip("eSideTab 순서(위→아래).")]
    [SerializeField] private UIButtonEx[] mTabButtons;
    [Tooltip("탭 선택 표시. eSideTab 순서. 비어 있는 칸은 표시하지 않는다.")]
    [SerializeField] private GameObject[] mTabSelectedMarks;

    [SerializeField] private UISideWarehouseView mWarehouseView;
    [SerializeField] private UISideUpgradeView mUpgradeView;
    [SerializeField] private UISidePlacementView mPlacementView;
    [SerializeField] private UISideStaffView mStaffView;
    [SerializeField] private UISideCollectionView mCollectionView;
    [SerializeField] private UILobbyFloorList mFloorList;

    // 지금 내용 슬롯에 보이는 탭.
    private eSideTab mContentTab = eSideTab.Warehouse;
    // 가구 배치를 끝내면 돌아갈 탭(가구 배치 직전에 보던 내용 탭).
    private eSideTab mReturnTab = eSideTab.Warehouse;
    private bool mIsInitialized;

    // floorCamera: 층 이동 목록이 이동을 맡길 로비 카메라(씬 오브젝트라 UIRootLobby가 넘겨준다).
    public void Init(LobbyFloorCameraController floorCamera)
    {
        if (mIsInitialized) return;
        mIsInitialized = true;

        if (mWarehouseView != null)
            mWarehouseView.Init();
        if (mUpgradeView != null)
            mUpgradeView.Init();
        if (mPlacementView != null)
            mPlacementView.Init();
        if (mStaffView != null)
            mStaffView.Init();
        if (mCollectionView != null)
            mCollectionView.Init();

        if (mFloorList != null)
        {
            mFloorList.Init(floorCamera);
            mFloorList.OpenChanged += _ => RefreshMarks();
        }

        for (int i = 0; i < mTabButtons.Length; i++)
        {
            if (mTabButtons[i] == null) continue;
            var tab = (eSideTab)i;
            mTabButtons[i].OnSubscribeOnClick(() => OnClickTab(tab)).AddTo(this);
        }

        ShowContent(eSideTab.Warehouse);

        GameInstance.Model.Placement.IsEditMode
            .Subscribe(OnEditModeChanged)
            .AddTo(this);
    }

    // 다른 화면(상점가 똘이 "시설 업그레이드" 등)에서 패널의 특정 탭을 열 때 쓴다. 내용 슬롯 탭만 받는다.
    public void ShowTab(eSideTab tab)
    {
        if (IsContentTab(tab))
            OnClickTab(tab);
    }

    private static bool IsContentTab(eSideTab tab)
    {
        return tab == eSideTab.Warehouse || tab == eSideTab.Upgrade || tab == eSideTab.Placement || tab == eSideTab.Staff
            || tab == eSideTab.Collection;
    }

    private void OnClickTab(eSideTab tab)
    {
        // 다른 탭을 누르면 층 이동 목록은 닫는다.
        if (tab != eSideTab.Floor && mFloorList != null)
            mFloorList.SetOpen(false);

        switch (tab)
        {
            case eSideTab.Warehouse:
            case eSideTab.Upgrade:
            case eSideTab.Staff:
            case eSideTab.Collection:
                ShowContent(tab);
                break;
            case eSideTab.Placement:
                OnClickPlacement();
                break;
            case eSideTab.ShopStreet:
                GameInstance.UI.Open<UIPopupShopStreet, UIPopupShopStreet.Param>(eUIType.PopupShopStreet, new UIPopupShopStreet.Param());
                break;
            case eSideTab.IngredientIsland:
                // 배치 모드인 채로 화면을 바꾸지 않는다(들고 있던 가구는 취소). OnEditModeChanged가 직전 탭으로 돌려 둔다.
                GameInstance.Model.Placement.SetEditMode(false);
                // 메뉴 레이어 창이라 열면 로비 루트는 닫힌다.
                GameInstance.UI.Open<UIRootIngredientIsland, UIRootIngredientIsland.Param>(eUIType.RootIngredientIsland, new UIRootIngredientIsland.Param());
                break;
            case eSideTab.Floor:
                if (mFloorList != null)
                    mFloorList.SetOpen(!mFloorList.IsOpen);
                break;
            case eSideTab.Quest:
                OpenComingSoon("의뢰는 준비 중이에요");
                break;
            case eSideTab.Settings:
                OpenComingSoon("설정은 준비 중이에요");
                break;
        }
    }

    // 가구 배치는 영업 정산 후 ~ 영업 시작 전에만 켤 수 있다. 영업 중에 누르면 안내만 한다.
    // 이미 가구 배치 화면이면 다시 눌러 끝낸다(OnEditModeChanged가 직전 탭으로 돌려 준다).
    private void OnClickPlacement()
    {
        var placement = GameInstance.Model.Placement;
        if (placement.IsEditMode.Value)
        {
            if (mContentTab == eSideTab.Placement)
                placement.SetEditMode(false);
            else
                ShowContent(eSideTab.Placement);
            return;
        }

        if (!GameInstance.Model.Business.CanEditLayout)
        {
            GameInstance.Model.Notice.Push(eNoticeType.Business, "영업 중에는 가구를 배치할 수 없어요 · 영업 정산 후에 배치해 주세요");
            return;
        }

        placement.SetEditMode(true);
    }

    // 배치 모드가 켜지면 가구 배치 화면을, 꺼지면 직전 내용 탭을 보여 준다(다른 곳에서 켜고 끈 경우 포함).
    private void OnEditModeChanged(bool isEditMode)
    {
        if (isEditMode && mContentTab != eSideTab.Placement)
            ShowContent(eSideTab.Placement);
        else if (!isEditMode && mContentTab == eSideTab.Placement)
            ShowContent(mReturnTab);
    }

    private static void OpenComingSoon(string message)
    {
        GameInstance.UI.Open<UIPopupComingSoon, UIPopupComingSoon.Param>(eUIType.PopupComingSoon, new UIPopupComingSoon.Param { Message = message });
    }

    private void ShowContent(eSideTab tab)
    {
        if (tab != eSideTab.Placement)
            mReturnTab = tab;
        mContentTab = tab;

        if (mWarehouseView != null)
            mWarehouseView.gameObject.SetActive(tab == eSideTab.Warehouse);
        if (mUpgradeView != null)
            mUpgradeView.gameObject.SetActive(tab == eSideTab.Upgrade);
        if (mPlacementView != null)
            mPlacementView.gameObject.SetActive(tab == eSideTab.Placement);
        if (mStaffView != null)
            mStaffView.gameObject.SetActive(tab == eSideTab.Staff);
        if (mCollectionView != null)
            mCollectionView.gameObject.SetActive(tab == eSideTab.Collection);

        // 가구 배치 화면을 떠나면 배치 모드도 끝낸다(들고 있던 가구는 PlacementModel이 정리한다).
        var placement = GameInstance.Model.Placement;
        if (tab != eSideTab.Placement && placement.IsEditMode.Value)
            placement.SetEditMode(false);

        RefreshMarks();
    }

    // 선택 표시: 내용 슬롯의 탭 + 켜져 있는 층 이동 목록.
    private void RefreshMarks()
    {
        for (int i = 0; i < mTabSelectedMarks.Length; i++)
        {
            if (mTabSelectedMarks[i] == null) continue;

            var tab = (eSideTab)i;
            bool on = tab == mContentTab
                || (tab == eSideTab.Floor && mFloorList != null && mFloorList.IsOpen);
            mTabSelectedMarks[i].SetActive(on);
        }
    }
}
