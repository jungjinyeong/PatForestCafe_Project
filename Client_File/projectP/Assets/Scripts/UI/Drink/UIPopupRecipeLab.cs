using System;
using System.Linq;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;

// 가공섬 "레시피 연구소" 팝업(프리팹 UI_Popup_RecipeLab). 연구소 직원 꾸리와 함께 새 음료 레시피를 개발한다.
// 흐름: 재료 5칸 + HOT/ICE → [개발하기] → 확인 → 분석 연출(ANALYSIS_SECONDS) → 결과.
// - 아직 발견하지 않은 레시피와 일치할 때만 레시피 개발권(개발북 1002) 1장 + 재료(+ICE 레시피 얼음)를 소모하고 도감에 등록한다.
// - 이미 발견한 조합/일치하는 레시피가 없음은 아무것도 소모하지 않고 결과만 알려 준다.
// - 개발에 성공한 음료는 픽업대로 옮기거나 폐기할 수 있다(UIDrinkCraftResult).
// 2026-09: UIPopupRecipeDevelop(로비 진입) → UIPopupRecipeLab(가공섬 진입)으로 개편(.cs.meta guid 유지).
public class UIPopupRecipeLab : UIWndBase, IUIParam<UIPopupRecipeLab.Param>
{
    public struct Param
    {
    }

    private enum eOutcome
    {
        None,
        Discovered,
        AlreadyKnown,
        NotFound,
    }

    private const int RECIPE_BOOK_ITEM_TID = 1002;
    private const float NOTICE_SECONDS = 2.5f;
    private const float ANALYSIS_SECONDS = 2.2f;
    private const float ANALYSIS_STEP_INTERVAL = 0.3f;

    private const string NPC_LINE_DEFAULT = "“준비가 되면 재료를 올려 주세요. 제가 조합을 분석할게요!”";
    private const string NPC_LINE_ANALYZING = "“좋아, 이제 조합을 하나씩 검증해 볼게!”";

    [Header("NPC")]
    [SerializeField] private TextMeshProUGUI mTextNpcLine;

    [Header("Temperature")]
    [SerializeField] private UIButtonEx mBtnHot;
    [SerializeField] private UIButtonEx mBtnIce;
    [SerializeField] private GameObject mHotSelectedMark;
    [SerializeField] private GameObject mIceSelectedMark;

    [Header("Material Slots")]
    [SerializeField] private UIDrinkMaterialSlot[] mBaseSlots;
    [SerializeField] private UIDrinkMaterialPicker mPicker;
    [SerializeField] private TextMeshProUGUI mTextBaseTags;
    [SerializeField] private UIButtonEx mBtnDevelop;
    [SerializeField] private UIButtonEx mBtnClear;

    [Header("Known Recipes")]
    [SerializeField] private UIScrollEx mKnownScrollEx;
    [SerializeField] private GameObject mKnownRowPrefab;
    [SerializeField] private TextMeshProUGUI mTextRecipeBookCount;
    [SerializeField] private TextMeshProUGUI mTextDiscoveredCount;

    [Header("Confirm")]
    [SerializeField] private GameObject mConfirmRoot;
    [SerializeField] private UIButtonEx mBtnConfirmOk;
    [SerializeField] private UIButtonEx mBtnConfirmCancel;

    [Header("Analysis")]
    [SerializeField] private GameObject mAnalysisRoot;
    [SerializeField] private GameObject[] mAnalysisSteps;

    [Header("Result")]
    [SerializeField] private UIDrinkCraftResult mCraftResult;   // 발견 성공(음료 완성)
    [SerializeField] private GameObject mResultRoot;            // 이미 발견/발견 실패
    [SerializeField] private TextMeshProUGUI mTextResultTitle;
    [SerializeField] private TextMeshProUGUI mTextResultMessage;
    [SerializeField] private UIButtonEx mBtnResultBack;

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

    private CTable.eDrinkTempType mTemp = CTable.eDrinkTempType.Ice;
    private int mEditingIndex = -1;
    private int mHelpPageIndex;
    private bool mIsModelSubscribed;

    // 분석 연출이 끝나면 보여줄 결과. 연출 도중 팝업을 닫았다 열어도 결과를 잃지 않도록 필드로 들고 있는다.
    private eOutcome mPendingOutcome = eOutcome.None;
    private int mPendingDrinkTid;
    private PickupDrinkData mPendingDrink;

    private readonly SerialDisposable mNoticeDisposable = new SerialDisposable();
    private readonly SerialDisposable mAnalysisDisposable = new SerialDisposable();

    public override eUIType GetUIType() => eUIType.PopupRecipeLab;

    public override void Init()
    {
        base.Init();

        for (int i = 0; i < mBaseSlots.Length; i++)
        {
            int index = i;
            mBaseSlots[i].Init(() => OnClickMaterialSlot(index));
        }

        mPicker.Init(OnPickMaterial, OnRemoveMaterial);
        mCraftResult.Init(OnClickStoreResult, OnClickDiscardResult);
        mKnownScrollEx.Init(mKnownRowPrefab);

        mBtnHot.OnSubscribeOnClick(() => SetTemp(CTable.eDrinkTempType.Hot)).AddTo(this);
        mBtnIce.OnSubscribeOnClick(() => SetTemp(CTable.eDrinkTempType.Ice)).AddTo(this);
        mBtnDevelop.OnSubscribeOnClick(OnClickDevelop).AddTo(this);
        mBtnClear.OnSubscribeOnClick(ResetSlots).AddTo(this);

        mBtnConfirmOk.OnSubscribeOnClick(OnClickConfirmOk).AddTo(this);
        mBtnConfirmCancel.OnSubscribeOnClick(() => mConfirmRoot.SetActive(false)).AddTo(this);
        mBtnResultBack.OnSubscribeOnClick(OnClickResultBack).AddTo(this);

        mBtnHelp.OnSubscribeOnClick(OpenHelp).AddTo(this);
        mBtnHelpPrev.OnSubscribeOnClick(() => ShowHelpPage(mHelpPageIndex - 1)).AddTo(this);
        mBtnHelpNext.OnSubscribeOnClick(() => ShowHelpPage(mHelpPageIndex + 1)).AddTo(this);
        mBtnHelpClose.OnSubscribeOnClick(() => mHelpRoot.SetActive(false)).AddTo(this);

        mNoticeDisposable.AddTo(this);
        mAnalysisDisposable.AddTo(this);
    }

    public override void Open()
    {
        base.Open();

        SubscribeModelOnce();

        mPicker.Close();
        mConfirmRoot.SetActive(false);
        mHelpRoot.SetActive(false);
        mTextNotice.SetTextEx(string.Empty);

        // 분석 도중 닫았다면 바로 결과를 보여 주고, 결과를 띄운 채 닫았다면 그대로 다시 보여 준다.
        if (mPendingOutcome != eOutcome.None)
        {
            ShowOutcome();
        }
        else if (mCraftResult.Drink != null)
        {
            mCraftResult.Show(mCraftResult.Drink, string.Empty, GameInstance.Model.Delivery.FindEmptyPickupSlot() >= 0);
        }
        else
        {
            mCraftResult.Hide();
            mAnalysisRoot.SetActive(false);
            mResultRoot.SetActive(false);
            mTemp = CTable.eDrinkTempType.Ice;
            ResetSlots();
        }

        RefreshKnownRecipes();
    }

    public void Set(Param param)
    {
    }

    // Init()이 GameInstance 모델 준비 이전에 불릴 수 있어, 모델 구독은 첫 Open에서 1회만 건다.
    private void SubscribeModelOnce()
    {
        if (mIsModelSubscribed) return;
        mIsModelSubscribed = true;

        var recipeBook = GameInstance.Model.Item.Get(RECIPE_BOOK_ITEM_TID);
        if (recipeBook != null)
        {
            recipeBook.Count
                .Subscribe(_ => RefreshRecipeBookCount())
                .AddTo(this);
        }
    }

    #region Material Slots

    private void OnClickMaterialSlot(int index)
    {
        mEditingIndex = index;

        var slot = GetEditingSlot();
        if (slot == null)
            return;

        mPicker.Open($"{index + 1}번 투입칸에 넣을 재료", DrinkCraftHelper.BuildPickerList(mBaseSlots, slot), slot.IsFilled);
    }

    private void OnPickMaterial(int materialTid)
    {
        GetEditingSlot()?.Set(materialTid);
        CloseEditing();
    }

    private void OnRemoveMaterial()
    {
        GetEditingSlot()?.Set(0);
        CloseEditing();
    }

    private void CloseEditing()
    {
        mEditingIndex = -1;
        mPicker.Close();
        RefreshSlots();
    }

    private UIDrinkMaterialSlot GetEditingSlot()
    {
        return mEditingIndex >= 0 && mEditingIndex < mBaseSlots.Length ? mBaseSlots[mEditingIndex] : null;
    }

    private void ResetSlots()
    {
        foreach (var slot in mBaseSlots) slot.Set(0);
        mEditingIndex = -1;
        mPicker.Close();
        RefreshSlots();
    }

    private void SetTemp(CTable.eDrinkTempType temp)
    {
        mTemp = temp;
        RefreshSlots();
    }

    private void RefreshSlots()
    {
        var baseTids = DrinkCraftHelper.GetFilledTids(mBaseSlots);
        mTextBaseTags.SetTextEx(DrinkCraftHelper.BuildTagText(baseTids));

        bool isHot = mTemp == CTable.eDrinkTempType.Hot;
        if (mHotSelectedMark != null) mHotSelectedMark.SetActive(isHot);
        if (mIceSelectedMark != null) mIceSelectedMark.SetActive(!isHot);

        mBtnDevelop.interactable = baseTids.Count > 0;
        RefreshNpcLine();
    }

    // 이미 개발한 조합을 올려 두면 꾸리가 미리 알려 준다(같은 조합을 다시 시도하지 않도록).
    private void RefreshNpcLine()
    {
        var baseCounts = DrinkCraftHelper.CountMaterials(DrinkCraftHelper.GetFilledTids(mBaseSlots));
        if (GameInstance.Model.Drink.TryGetRecipeMatch(baseCounts, mTemp, out int drinkTid) &&
            GameInstance.Model.RecipeBook.IsDiscovered(drinkTid))
        {
            mTextNpcLine.SetTextEx($"“이건 이미 개발된 {GameInstance.Model.Drink.GetName(drinkTid)}의 레시피야. 다시 검토가 필요하겠는걸?”");
            return;
        }

        mTextNpcLine.SetTextEx(NPC_LINE_DEFAULT);
    }

    #endregion

    #region Known Recipes

    private void RefreshKnownRecipes()
    {
        var discovered = GameInstance.Model.RecipeBook.GetDiscoveredTids()
            .Where(tid => GameInstance.Model.Drink.Get(tid) != null)
            .OrderBy(tid => tid)
            .Select(tid => new UIScrollDrinkRecipeData { DrinkTid = tid })
            .ToList();

        mKnownScrollEx.SetData(discovered);

        int total = GameInstance.Table.GetTable<CTable.DrinkRow>()?.All.Count ?? 0;
        mTextDiscoveredCount.SetTextEx($"발견한 레시피 {discovered.Count}/{total}");

        RefreshRecipeBookCount();
    }

    private void RefreshRecipeBookCount()
    {
        int bookCount = GameInstance.Model.Item.Get(RECIPE_BOOK_ITEM_TID)?.Count.Value ?? 0;
        mTextRecipeBookCount.SetTextEx($"레시피 개발권 {bookCount}장");
    }

    #endregion

    #region Develop

    private void OnClickDevelop()
    {
        if (DrinkCraftHelper.GetFilledTids(mBaseSlots).Count == 0)
            return;

        if (!GameInstance.Model.Item.HasEnough(RECIPE_BOOK_ITEM_TID, 1))
        {
            ShowNotice("레시피 개발권이 없어요.");
            return;
        }

        mConfirmRoot.SetActive(true);
    }

    private void OnClickConfirmOk()
    {
        mConfirmRoot.SetActive(false);

        var baseTids = DrinkCraftHelper.GetFilledTids(mBaseSlots);
        bool matched = GameInstance.Model.Drink.TryGetRecipeMatch(DrinkCraftHelper.CountMaterials(baseTids), mTemp, out int drinkTid);

        if (!matched)
        {
            StartAnalysis(eOutcome.NotFound, 0, null);
            return;
        }

        if (GameInstance.Model.RecipeBook.IsDiscovered(drinkTid))
        {
            StartAnalysis(eOutcome.AlreadyKnown, drinkTid, null);
            return;
        }

        // 새 레시피 — 이 시점에 소모/등록까지 확정하고, 결과는 분석 연출이 끝난 뒤 보여 준다.
        var required = DrinkCraftHelper.BuildRequiredMaterials(baseTids, GameInstance.Model.Drink.GetIceCount(drinkTid));
        if (DrinkCraftHelper.TryFindShortage(required, out string shortage))
        {
            ShowNotice($"새 레시피의 기운이 느껴지지만 {shortage}이(가) 부족해요.");
            return;
        }

        if (!GameInstance.Model.Item.HasEnough(RECIPE_BOOK_ITEM_TID, 1))
        {
            ShowNotice("레시피 개발권이 없어요.");
            return;
        }

        DrinkCraftHelper.Consume(required);
        GameInstance.Model.Item.Consume(RECIPE_BOOK_ITEM_TID, 1);
        GameInstance.Model.RecipeBook.Discover(drinkTid);

        StartAnalysis(eOutcome.Discovered, drinkTid, PickupDrinkData.Create(drinkTid, mTemp, baseTids, Array.Empty<int>()));
    }

    private void StartAnalysis(eOutcome outcome, int drinkTid, PickupDrinkData drink)
    {
        mPendingOutcome = outcome;
        mPendingDrinkTid = drinkTid;
        mPendingDrink = drink;

        mTextNpcLine.SetTextEx(NPC_LINE_ANALYZING);
        mAnalysisRoot.SetActive(true);
        foreach (var step in mAnalysisSteps) step.SetActive(false);

        var disposables = new CompositeDisposable();
        for (int i = 0; i < mAnalysisSteps.Length; i++)
        {
            var step = mAnalysisSteps[i];
            Observable.Timer(TimeSpan.FromSeconds(ANALYSIS_STEP_INTERVAL * i))
                .Subscribe(_ => step.SetActive(true))
                .AddTo(disposables);
        }

        Observable.Timer(TimeSpan.FromSeconds(ANALYSIS_SECONDS))
            .Subscribe(_ => ShowOutcome())
            .AddTo(disposables);

        mAnalysisDisposable.Disposable = disposables;
    }

    private void ShowOutcome()
    {
        mAnalysisDisposable.Disposable = null;
        mAnalysisRoot.SetActive(false);

        var outcome = mPendingOutcome;
        var drink = mPendingDrink;
        string drinkName = GameInstance.Model.Drink.GetName(mPendingDrinkTid);

        mPendingOutcome = eOutcome.None;
        mPendingDrink = null;

        switch (outcome)
        {
            case eOutcome.Discovered:
                ResetSlots();
                RefreshKnownRecipes();
                mCraftResult.Show(drink, "짜잔! 새로운 레시피를 발견했어요!\n도감과 커피머신 제작 목록에 등록돼요.",
                    GameInstance.Model.Delivery.FindEmptyPickupSlot() >= 0);
                break;

            case eOutcome.AlreadyKnown:
                ShowResult($"{drinkName}은(는) 이미 등록된 레시피예요.", "도감과 제작 목록에서 다시 확인할 수 있어요.");
                break;

            case eOutcome.NotFound:
                ShowResult("아직 레시피를 발견하지 못했어요.", "다른 재료 조합으로 다시 연구해 보세요.");
                break;
        }
    }

    private void ShowResult(string title, string message)
    {
        mTextResultTitle.SetTextEx(title);
        mTextResultMessage.SetTextEx(message);
        mResultRoot.SetActive(true);
    }

    private void OnClickResultBack()
    {
        mResultRoot.SetActive(false);
        ResetSlots();
    }

    private void OnClickStoreResult()
    {
        if (!GameInstance.Model.Delivery.TryStorePickup(mCraftResult.Drink))
        {
            ShowNotice("픽업대에 빈 자리가 없어요.");
            return;
        }

        mCraftResult.Hide();
        RefreshNpcLine();
    }

    private void OnClickDiscardResult()
    {
        mCraftResult.Hide();
        RefreshNpcLine();
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
