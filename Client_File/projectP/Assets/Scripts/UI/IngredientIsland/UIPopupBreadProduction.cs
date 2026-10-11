using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;

// "오븐 제작" 팝업(프리팹 UI_Popup_BreadProduction, 가공섬 진입).
// 트레이마다 반죽(발견한 빵 레시피 + 재료 등급 + 수량)을 설정하고 [굽기 시작하기]로 대기 트레이를 한 번에 굽는다.
// 재료는 굽기 시작 시 전체 트레이 합산으로 한 번 차감되고, 굽기가 끝나면 빵 재고(창고)에 품질별로 자동 보관된다(OvenModel).
// 2026-09: 재료 조합 매칭 즉시 생산 → 오븐 트레이/굽기 시간/품질 구조로 전면 개편(.cs.meta guid 유지).
public class UIPopupBreadProduction : UIWndBase, IUIParam<UIPopupBreadProduction.Param>
{
    public struct Param
    {
    }

    private const float NOTICE_SECONDS = 2.5f;
    private const float BAKE_REFRESH_SECONDS = 0.5f;
    private const int STOCK_PAGE_SIZE = 6;

    [Header("Header")]
    [SerializeField] private TextMeshProUGUI mTextStatus;

    [Header("Trays")]
    [SerializeField] private UIOvenTraySlot[] mTraySlots;
    [SerializeField] private TextMeshProUGUI mTextUpgradeNote;
    [SerializeField] private UIButtonEx mBtnBake;
    [SerializeField] private UIBreadDoughPanel mDoughPanel;

    [Header("Stock")]
    [SerializeField] private UIScrollEx mStockScrollEx;
    [SerializeField] private GameObject mStockRowPrefab;
    [SerializeField] private UIButtonEx[] mStockTabs;
    [SerializeField] private GameObject[] mStockTabSelectedMarks;
    [SerializeField] private GameObject mStockDetailRoot;
    [SerializeField] private TextMeshProUGUI mTextStockDetailName;
    [SerializeField] private TextMeshProUGUI mTextStockDetailDesc;
    [SerializeField] private TextMeshProUGUI mTextStockDetailTags;
    [SerializeField] private TextMeshProUGUI mTextStockDetailQuality;
    [SerializeField] private UIButtonEx mBtnStockDetailClose;

    [Header("Help")]
    [SerializeField] private UIButtonEx mBtnHelp;
    [SerializeField] private GameObject mHelpRoot;
    [SerializeField] private GameObject[] mHelpPages;
    [SerializeField] private TextMeshProUGUI mTextHelpPage;
    [SerializeField] private UIButtonEx mBtnHelpPrev;
    [SerializeField] private UIButtonEx mBtnHelpNext;
    [SerializeField] private UIButtonEx mBtnHelpClose;

    [Header("Notice")]
    [SerializeField] private TextMeshProUGUI mTextNotice;

    private int mStockPage;
    private int mHelpPageIndex;
    private bool mIsModelSubscribed;
    private readonly SerialDisposable mNoticeDisposable = new SerialDisposable();

    private OvenModel Oven => GameInstance.Model.Oven;

    public override eUIType GetUIType() => eUIType.PopupBreadProduction;

    public override void Init()
    {
        base.Init();

        for (int i = 0; i < mTraySlots.Length; i++)
        {
            int index = i;
            mTraySlots[i].Init(() => OnClickTray(index));
        }

        mDoughPanel.Init(RefreshAll);
        mBtnBake.OnSubscribeOnClick(OnClickBake).AddTo(this);

        mStockScrollEx.Init(mStockRowPrefab);
        mStockScrollEx.SetOnSelect(OnSelectStock);
        for (int i = 0; i < mStockTabs.Length; i++)
        {
            int page = i;
            mStockTabs[i].OnSubscribeOnClick(() => SetStockPage(page)).AddTo(this);
        }
        mBtnStockDetailClose.OnSubscribeOnClick(() => mStockDetailRoot.SetActive(false)).AddTo(this);

        mBtnHelp.OnSubscribeOnClick(OpenHelp).AddTo(this);
        mBtnHelpPrev.OnSubscribeOnClick(() => ShowHelpPage(mHelpPageIndex - 1)).AddTo(this);
        mBtnHelpNext.OnSubscribeOnClick(() => ShowHelpPage(mHelpPageIndex + 1)).AddTo(this);
        mBtnHelpClose.OnSubscribeOnClick(() => mHelpRoot.SetActive(false)).AddTo(this);

        mNoticeDisposable.AddTo(this);
    }

    public override void Open()
    {
        base.Open();

        SubscribeModelOnce();

        mDoughPanel.Close();
        mHelpRoot.SetActive(false);
        mStockDetailRoot.SetActive(false);
        mTextNotice.SetTextEx(string.Empty);

        SetStockPage(0);
        RefreshAll();
    }

    public void Set(Param param)
    {
    }

    // Init()이 GameInstance 모델 준비 이전에 불릴 수 있어, 모델 구독은 첫 Open에서 1회만 건다.
    private void SubscribeModelOnce()
    {
        if (mIsModelSubscribed) return;
        mIsModelSubscribed = true;

        Oven.OnTraysChanged
            .Subscribe(_ => RefreshAll())
            .AddTo(this);

        Oven.OnBakeCompleted
            .Where(_ => gameObject.activeInHierarchy)
            .Subscribe(result =>
            {
                string name = GameInstance.Table.Get<CTable.MenuItemRow>(result.recipeTid)?.Name ?? result.recipeTid.ToString();
                ShowNotice($"{name} {result.quantity}개({BreadQuality.GetName(result.quality)}) 굽기 완료! 창고에 보관했어요.");
                RefreshStock();
            })
            .AddTo(this);

        // 굽는 중 남은 시간/게이지 표시.
        Observable.Interval(TimeSpan.FromSeconds(BAKE_REFRESH_SECONDS))
            .Where(_ => gameObject.activeInHierarchy)
            .Subscribe(_ => RefreshTrays())
            .AddTo(this);
    }

    private void RefreshAll()
    {
        RefreshTrays();
        RefreshStock();
    }

    #region Trays

    private void RefreshTrays()
    {
        for (int i = 0; i < mTraySlots.Length; i++)
            mTraySlots[i].Refresh(i, Oven);

        mTextStatus.SetTextEx($"활성 트레이 {Oven.ActiveTrayCount} / {Oven.UnlockedTrayCount}   |   굽기 {Oven.BakeSeconds}초   |   창고 자동 보관");

        string next = Oven.GetNextUpgradeSummary();
        mTextUpgradeNote.SetTextEx(next != null ? $"다음 업그레이드: {next}" : "오븐 최대 레벨");

        mBtnBake.interactable = Oven.ReadyTrayCount > 0;
    }

    private void OnClickTray(int index)
    {
        if (!Oven.IsTrayUnlocked(index))
        {
            ShowNotice("오븐 업그레이드 후 해금됩니다. (상점가 똘이 - 시설 업그레이드)");
            return;
        }

        if (Oven.Trays[index].State == eOvenTrayState.Baking)
            return;

        mDoughPanel.Open(index);
    }

    private void OnClickBake()
    {
        if (Oven.TryStartBake(out string shortage))
        {
            ShowNotice($"굽기 시작! {Oven.BakeSeconds}초 뒤 창고에 자동 보관돼요.");
            return;
        }

        ShowNotice(shortage != null ? $"{shortage}이(가) 부족해요." : "반죽을 설정한 트레이가 없어요.");
    }

    #endregion

    #region Stock

    // 발견한 레시피이거나 재고가 있는 빵을 Tid 순으로 페이지(창고 1/2)마다 STOCK_PAGE_SIZE개씩 보여준다.
    private void RefreshStock()
    {
        var breads = GameInstance.Model.Bread.GetAll()
            .Where(b => b.ProducedCount.Value > 0 || GameInstance.Model.RecipeBook.IsDiscovered(b.TId))
            .OrderBy(b => b.TId)
            .Skip(mStockPage * STOCK_PAGE_SIZE)
            .Take(STOCK_PAGE_SIZE)
            .Select(b => new UIScrollBreadStockData { BreadTid = b.TId })
            .ToList();

        mStockScrollEx.SetData(breads);

        for (int i = 0; i < mStockTabSelectedMarks.Length; i++)
            mStockTabSelectedMarks[i].SetActive(i == mStockPage);
    }

    private void SetStockPage(int page)
    {
        mStockPage = Mathf.Clamp(page, 0, Mathf.Max(0, mStockTabs.Length - 1));
        mStockDetailRoot.SetActive(false);
        RefreshStock();
    }

    private void OnSelectStock(UIScrollRow row)
    {
        if (row is not UIScrollBreadStock stockRow || stockRow.CurrentData == null)
            return;

        var bread = GameInstance.Model.Bread.Get(stockRow.CurrentData.BreadTid);
        if (bread == null)
            return;

        mTextStockDetailName.SetTextEx(bread.MenuItemRow?.Name ?? bread.TId.ToString());
        mTextStockDetailDesc.SetTextEx(bread.Row?.Desc ?? "정성껏 구운 빵이에요.");
        mTextStockDetailTags.SetTextEx(bread.Row != null ? string.Join(", ", bread.Row.GetTags()) : string.Empty);

        string quality = UIScrollBreadStock.BuildQualityText(bread);
        mTextStockDetailQuality.SetTextEx(string.IsNullOrEmpty(quality) ? "보관 중인 빵이 없어요." : quality);
        mStockDetailRoot.SetActive(true);
    }

    #endregion

    #region Help

    private void OpenHelp()
    {
        mHelpRoot.SetActive(true);
        ShowHelpPage(0);
    }

    private void ShowHelpPage(int index)
    {
        if (mHelpPages == null || mHelpPages.Length == 0) return;

        mHelpPageIndex = Mathf.Clamp(index, 0, mHelpPages.Length - 1);
        for (int i = 0; i < mHelpPages.Length; i++)
            mHelpPages[i].SetActive(i == mHelpPageIndex);

        mTextHelpPage.SetTextEx($"{mHelpPageIndex + 1} / {mHelpPages.Length}");
        mBtnHelpPrev.interactable = mHelpPageIndex > 0;
        mBtnHelpNext.interactable = mHelpPageIndex < mHelpPages.Length - 1;
    }

    #endregion

    private void ShowNotice(string message)
    {
        if (mTextNotice == null) return;

        mTextNotice.SetTextEx(message);
        mNoticeDisposable.Disposable = Observable.Timer(TimeSpan.FromSeconds(NOTICE_SECONDS))
            .Subscribe(_ => mTextNotice.SetTextEx(string.Empty));
    }
}
