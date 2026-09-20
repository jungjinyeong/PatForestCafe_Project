using UnityEngine;
using UniRx;
using Extension;

// 상점가 허브 — 뼈대 단계라 3명의 상인을 각자 별도 서브 패널로 분리했다(프로토타입처럼 하나의 동적 상세창을
// 전환하는 대신). 서비스 버튼 중 기존 기능이 있는 것(가구구입/시설업그레이드/층별설비/직원채용)은 그대로 연결하고,
// 아직 대응 시스템이 없는 것(가공섬 계약/공방 협약/가공섬 업그레이드/연구권 구입)은 빈 상태 안내만 띄운다.
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

    [Header("찍찍이 (Placeholder)")]
    [SerializeField] private UIButtonEx mBtnIslandContract;
    [SerializeField] private UIButtonEx mBtnWorkshopDeal;
    [SerializeField] private UIButtonEx mBtnIslandUpgrade;
    [SerializeField] private UIButtonEx mBtnResearchTicket;
    [SerializeField] private GameObject mPanelMousePlaceholder;

    [Header("똘이 (기존 기능 연결)")]
    [SerializeField] private UIButtonEx mBtnBuyFurniture;
    [SerializeField] private UIButtonEx mBtnFacilityUpgrade;
    [SerializeField] private UIButtonEx mBtnFloorFacility;

    [Header("직업사무소")]
    [SerializeField] private UIButtonEx mBtnResumeCheck;

    public override eUIType GetUIType() => eUIType.UIPopupShopStreet;

    // 뼈대 단계라 에디터에서 아직 배선되지 않은 참조가 있을 수 있다 — UIRootLobby의 나머지 Init() 체인이
    // NRE로 끊기지 않도록 필드마다 null 체크 후 연결한다(mBtnFloorUnlock 등 기존 패턴과 동일).
    public override void Init()
    {
        base.Init();

        if (mPanelMouse != null) mPanelMouse.SetActive(false);
        if (mPanelDdol != null) mPanelDdol.SetActive(false);
        if (mPanelJob != null) mPanelJob.SetActive(false);
        if (mPanelMousePlaceholder != null) mPanelMousePlaceholder.SetActive(false);

        if (mBtnMouse != null) mBtnMouse.OnSubscribeOnClick(() => OpenMerchant(mPanelMouse)).AddTo(this);
        if (mBtnDdol != null) mBtnDdol.OnSubscribeOnClick(() => OpenMerchant(mPanelDdol)).AddTo(this);
        if (mBtnJob != null) mBtnJob.OnSubscribeOnClick(() => OpenMerchant(mPanelJob)).AddTo(this);

        if (mBtnIslandContract != null) mBtnIslandContract.OnSubscribeOnClick(OpenMousePlaceholder).AddTo(this);
        if (mBtnWorkshopDeal != null) mBtnWorkshopDeal.OnSubscribeOnClick(OpenMousePlaceholder).AddTo(this);
        if (mBtnIslandUpgrade != null) mBtnIslandUpgrade.OnSubscribeOnClick(OpenMousePlaceholder).AddTo(this);
        if (mBtnResearchTicket != null) mBtnResearchTicket.OnSubscribeOnClick(OpenMousePlaceholder).AddTo(this);

        if (mBtnBuyFurniture != null) mBtnBuyFurniture.OnSubscribeOnClick(OnClickBuyFurniture).AddTo(this);
        if (mBtnFacilityUpgrade != null) mBtnFacilityUpgrade.OnSubscribeOnClick(OnClickFacilityUpgrade).AddTo(this);
        if (mBtnFloorFacility != null) mBtnFloorFacility.OnSubscribeOnClick(OnClickFloorFacility).AddTo(this);

        if (mBtnResumeCheck != null) mBtnResumeCheck.OnSubscribeOnClick(OnClickResumeCheck).AddTo(this);
    }

    public override void Open()
    {
        base.Open();

        // 매번 "상인 선택" 첫 화면부터 시작하도록 서브 패널을 초기화한다.
        if (mPanelMouse != null) mPanelMouse.SetActive(false);
        if (mPanelDdol != null) mPanelDdol.SetActive(false);
        if (mPanelJob != null) mPanelJob.SetActive(false);
    }

    public void Set(Param param)
    {
    }

    private void OpenMousePlaceholder()
    {
        if (mPanelMousePlaceholder != null)
            mPanelMousePlaceholder.SetActive(true);
    }

    private void OpenMerchant(GameObject panel)
    {
        if (mPanelMouse != null) mPanelMouse.SetActive(panel == mPanelMouse);
        if (mPanelDdol != null) mPanelDdol.SetActive(panel == mPanelDdol);
        if (mPanelJob != null) mPanelJob.SetActive(panel == mPanelJob);
    }

    private void OnClickBuyFurniture()
    {
        GameInstance.UI.Open<UIPopupShopFurniture, UIPopupShopFurniture.Param>(eUIType.UIPopupShopFurniture, new UIPopupShopFurniture.Param());
    }

    private void OnClickFacilityUpgrade()
    {
        GameInstance.UI.Open<UIPopupUpgrade, UIPopupUpgrade.Param>(eUIType.UIPopupUpgrade, new UIPopupUpgrade.Param());
    }

    private void OnClickFloorFacility()
    {
        GameInstance.UI.Open<UIFloorUnlock, UIFloorUnlock.Param>(eUIType.UIFloorUnlock, new UIFloorUnlock.Param());
    }

    private void OnClickResumeCheck()
    {
        GameInstance.UI.Open<UIPopupJobOffice, UIPopupJobOffice.Param>(eUIType.UIPopupJobOffice, new UIPopupJobOffice.Param());
    }
}
