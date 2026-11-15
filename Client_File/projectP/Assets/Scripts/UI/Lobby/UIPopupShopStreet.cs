using UnityEngine;
using UniRx;
using Extension;
using UnityEngine.Serialization;

// 상점가 허브 — 뼈대 단계라 3명의 상인을 각자 별도 서브 패널로 분리했다(프로토타입처럼 하나의 동적 상세창을
// 전환하는 대신). 서비스 버튼 중 기존 기능이 있는 것(가구구입/시설업그레이드/층별설비/직원채용)은 그대로 연결하고,
// 아직 대응 시스템이 없는 것(가공섬 계약/공방 협약/가공섬 업그레이드/연구권 구입)은 준비 중 팝업(UIPopupComingSoon)만 띄운다.
// 재료 구입은 가공섬에 따로 있던 재료 상점을 찍찍이 서비스로 통합한 것으로, 별도 팝업 없이 상점가 안의 서브 패널(UIShopStreetIngredientPanel)로 연다.
public class UIPopupShopStreet : UIWndBase, IUIParam<UIPopupShopStreet.Param>
{
    public struct Param
    {
    }

    [Header("Merchant Select")]
    [SerializeField] private UIButtonEx mBtnMouse;
    [SerializeField] private UIButtonEx mBtnDdol;
    [SerializeField] private UIButtonEx mBtnJob;

    [Header("Merchant Sub Panels")]
    [SerializeField] private GameObject mPanelMouse;
    [SerializeField] private GameObject mPanelDdol;
    [SerializeField] private GameObject mPanelJob;
    [SerializeField, FormerlySerializedAs("mPanelMaterialShop")] private UIShopStreetIngredientPanel mPanelIngredientShop;

    [Header("찍찍이 (Placeholder)")]
    [SerializeField] private UIButtonEx mBtnIslandContract;
    [SerializeField] private UIButtonEx mBtnWorkshopDeal;
    [SerializeField] private UIButtonEx mBtnIslandUpgrade;
    [SerializeField] private UIButtonEx mBtnResearchTicket;
    [SerializeField, FormerlySerializedAs("mBtnBreadRecipe")] private UIButtonEx mBtnDessertRecipe;
    [SerializeField, FormerlySerializedAs("mBtnMaterialShop")] private UIButtonEx mBtnIngredientShop;

    [Header("똘이 (기존 기능 연결)")]
    [SerializeField] private UIButtonEx mBtnBuyFurniture;
    [SerializeField] private UIButtonEx mBtnFacilityUpgrade;
    [SerializeField] private UIButtonEx mBtnFloorFacility;

    [Header("직업사무소")]
    [SerializeField] private UIButtonEx mBtnResumeCheck;

    public override eUIType GetUIType() => eUIType.PopupShopStreet;

    // 뼈대 단계라 에디터에서 아직 배선되지 않은 참조가 있을 수 있다 — UIRootLobby의 나머지 Init() 체인이
    // NRE로 끊기지 않도록 필드마다 null 체크 후 연결한다(mBtnFloorUnlock 등 기존 패턴과 동일).
    public override void Init()
    {
        base.Init();

        if (mPanelIngredientShop != null) mPanelIngredientShop.Init();
        OpenMerchant(null);

        if (mBtnMouse != null) mBtnMouse.OnSubscribeOnClick(() => OpenMerchant(mPanelMouse)).AddTo(this);
        if (mBtnDdol != null) mBtnDdol.OnSubscribeOnClick(() => OpenMerchant(mPanelDdol)).AddTo(this);
        if (mBtnJob != null) mBtnJob.OnSubscribeOnClick(() => OpenMerchant(mPanelJob)).AddTo(this);

        if (mBtnIslandContract != null) mBtnIslandContract.OnSubscribeOnClick(OpenMousePlaceholder).AddTo(this);
        if (mBtnWorkshopDeal != null) mBtnWorkshopDeal.OnSubscribeOnClick(OpenMousePlaceholder).AddTo(this);
        if (mBtnIslandUpgrade != null) mBtnIslandUpgrade.OnSubscribeOnClick(OpenMousePlaceholder).AddTo(this);
        if (mBtnResearchTicket != null) mBtnResearchTicket.OnSubscribeOnClick(OpenMousePlaceholder).AddTo(this);
        if (mBtnDessertRecipe != null) mBtnDessertRecipe.OnSubscribeOnClick(OnClickDessertRecipe).AddTo(this);
        if (mBtnIngredientShop != null) mBtnIngredientShop.OnSubscribeOnClick(OnClickIngredientShop).AddTo(this);

        if (mBtnBuyFurniture != null) mBtnBuyFurniture.OnSubscribeOnClick(OnClickBuyFurniture).AddTo(this);
        if (mBtnFacilityUpgrade != null) mBtnFacilityUpgrade.OnSubscribeOnClick(OnClickFacilityUpgrade).AddTo(this);
        if (mBtnFloorFacility != null) mBtnFloorFacility.OnSubscribeOnClick(OnClickFloorFacility).AddTo(this);

        if (mBtnResumeCheck != null) mBtnResumeCheck.OnSubscribeOnClick(OnClickResumeCheck).AddTo(this);
    }

    public override void Open()
    {
        base.Open();

        // 매번 "상인 선택" 첫 화면부터 시작하도록 서브 패널을 초기화한다.
        OpenMerchant(null);
    }

    public void Set(Param param)
    {
    }

    private void OpenMousePlaceholder()
    {
        GameInstance.UI.Open<UIPopupComingSoon, UIPopupComingSoon.Param>(eUIType.PopupComingSoon, new UIPopupComingSoon.Param());
    }

    private void OnClickDessertRecipe()
    {
        GameInstance.UI.Open<UIPopupDessertRecipeShop, UIPopupDessertRecipeShop.Param>(eUIType.PopupDessertRecipeShop, new UIPopupDessertRecipeShop.Param());
    }

    // 찍찍이 패널 대신 재료 구입 패널을 보여준다. 찍찍이 버튼을 다시 누르면 찍찍이 패널로 돌아간다.
    private void OnClickIngredientShop()
    {
        OpenMerchant(null);
        if (mPanelIngredientShop != null) mPanelIngredientShop.Show(true);
    }

    // panel이 null이면 모든 서브 패널을 닫는다(상인 선택 첫 화면).
    private void OpenMerchant(GameObject panel)
    {
        if (mPanelMouse != null) mPanelMouse.SetActive(panel == mPanelMouse);
        if (mPanelDdol != null) mPanelDdol.SetActive(panel == mPanelDdol);
        if (mPanelJob != null) mPanelJob.SetActive(panel == mPanelJob);
        if (mPanelIngredientShop != null) mPanelIngredientShop.gameObject.SetActive(false);
    }

    private void OnClickBuyFurniture()
    {
        GameInstance.UI.Open<UIPopupShopFurniture, UIPopupShopFurniture.Param>(eUIType.PopupShopFurniture, new UIPopupShopFurniture.Param());
    }

    // 업그레이드는 로비 오른쪽 패널로 옮겼다. 상점가를 닫고 패널의 [업그레이드] 탭을 연다.
    // 가공섬에서 연 상점가라면 먼저 로비로 돌아간다(숨은 로비 패널에서 탭만 바뀌지 않게).
    private void OnClickFacilityUpgrade()
    {
        SelfClose();

        var island = GameInstance.UI.Get<UIRootIngredientIsland>(eUIType.RootIngredientIsland);
        if (island != null && island.gameObject.activeInHierarchy)
            GameInstance.UI.Open<UIRootLobby, UIRootLobby.Param>(eUIType.RootLobby, new UIRootLobby.Param());

        var root = GameInstance.UI.Get<UIRootLobby>(eUIType.RootLobby);
        if (root != null)
            root.ShowSideTab(UILobbySidePanel.eSideTab.Upgrade);
    }

    private void OnClickFloorFacility()
    {
        GameInstance.UI.Open<UIFloorUnlock, UIFloorUnlock.Param>(eUIType.PopupFloorUnlock, new UIFloorUnlock.Param());
    }

    private void OnClickResumeCheck()
    {
        GameInstance.UI.Open<UIPopupJobOffice, UIPopupJobOffice.Param>(eUIType.PopupJobOffice, new UIPopupJobOffice.Param());
    }
}
