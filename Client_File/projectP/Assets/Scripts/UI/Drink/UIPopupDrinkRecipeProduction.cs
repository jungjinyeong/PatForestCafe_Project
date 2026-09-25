using System;
using System.Linq;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;

// 커피머신 제작 팝업(프리팹 UI_Popup_SpecialDrinkProduction).
// - 기본 재료 슬롯(얼음 제외) + 온도(HOT/ICE)로 DrinkRow를 매칭한다. ICE 음료의 얼음은 레시피 개수만큼 자동 소모.
// - 도감에 발견된 레시피만 제대로 만들어진다. 미발견 조합은 "실패한 음료"(발견은 가공섬 레시피 연구소 UIPopupRecipeLab에서만).
// - 커스텀 재료 슬롯은 레시피 매칭에 쓰지 않고, 속성(Tags)/토핑 요청 충족 판정에만 쓴다.
// - 완성 음료는 픽업대(DeliveryModel.PickupSlots)에 올린다. 주문서/배달은 별도 팝업 UIPopupDelivery(UI_Popup_Delivery).
// 2026-08: 클래스/파일명을 UIPopupSpecialDrinkProduction → UIPopupDrinkRecipeProduction으로 변경(.cs.meta guid 유지).
public class UIPopupDrinkRecipeProduction : UIWndBase, IUIParam<UIPopupDrinkRecipeProduction.Param>
{
    public struct Param
    {
    }

    private const float NOTICE_SECONDS = 2f;

    [Header("Recipe List")]
    [SerializeField] private UIScrollEx mRecipeScrollEx;
    [SerializeField] private GameObject mRecipeRowPrefab;

    [Header("Delivery")]
    [SerializeField] private UIButtonEx mBtnOpenDelivery;
    [SerializeField] private TextMeshProUGUI mTextPickupStatus;

    [Header("Temperature")]
    [SerializeField] private UIButtonEx mBtnHot;
    [SerializeField] private UIButtonEx mBtnIce;
    [SerializeField] private GameObject mHotSelectedMark;
    [SerializeField] private GameObject mIceSelectedMark;

    [Header("Material Slots")]
    [SerializeField] private UIDrinkMaterialSlot[] mBaseSlots;
    [SerializeField] private UIDrinkMaterialSlot[] mCustomSlots;
    [SerializeField] private UIDrinkMaterialPicker mPicker;

    [Header("Info")]
    [SerializeField] private GameObject mCupHot;
    [SerializeField] private GameObject mCupIce;
    [SerializeField] private TextMeshProUGUI mTextBaseTags;
    [SerializeField] private TextMeshProUGUI mTextCustomTags;
    [SerializeField] private UIButtonEx mBtnCraft;

    [Header("Result / Notice")]
    [SerializeField] private UIDrinkCraftResult mCraftResult;
    [SerializeField] private TextMeshProUGUI mTextNotice;

    private CTable.eDrinkTempType mTemp = CTable.eDrinkTempType.Ice;
    private bool mEditingBase;
    private int mEditingIndex = -1;
    private bool mIsModelSubscribed;
    private readonly SerialDisposable mNoticeDisposable = new SerialDisposable();

    public override eUIType GetUIType() => eUIType.PopupDrinkRecipeProduction;

    public override void Init()
    {
        base.Init();

        mRecipeScrollEx.Init(mRecipeRowPrefab);
        mRecipeScrollEx.SetOnSelect(OnSelectRecipe);

        for (int i = 0; i < mBaseSlots.Length; i++)
        {
            int index = i;
            mBaseSlots[i].Init(() => OnClickMaterialSlot(true, index));
        }

        for (int i = 0; i < mCustomSlots.Length; i++)
        {
            int index = i;
            mCustomSlots[i].Init(() => OnClickMaterialSlot(false, index));
        }

        mPicker.Init(OnPickMaterial, OnRemoveMaterial);
        mCraftResult.Init(OnClickStoreResult, OnClickDiscardResult);

        mBtnHot.OnSubscribeOnClick(() => SetTemp(CTable.eDrinkTempType.Hot)).AddTo(this);
        mBtnIce.OnSubscribeOnClick(() => SetTemp(CTable.eDrinkTempType.Ice)).AddTo(this);
        mBtnCraft.OnSubscribeOnClick(OnClickCraft).AddTo(this);
        mBtnOpenDelivery.OnSubscribeOnClick(OnClickOpenDelivery).AddTo(this);

        mNoticeDisposable.AddTo(this);
    }

    public override void Open()
    {
        base.Open();

        SubscribeModelOnce();

        mTemp = CTable.eDrinkTempType.Ice;
        ClearMaterialSlots();

        mPicker.Close();
        mTextNotice.SetTextEx(string.Empty);

        // 결과 팝업을 띄운 채 닫았다면 완성 음료를 잃지 않도록 다시 보여준다.
        if (mCraftResult.Drink != null)
            mCraftResult.Show(mCraftResult.Drink, string.Empty, GameInstance.Model.Delivery.FindEmptyPickupSlot() >= 0);
        else
            mCraftResult.Hide();

        SetupRecipeScroll();
        RefreshMaterialSlots();
        RefreshPickupStatus();
    }

    public void Set(Param param)
    {
    }

    // Init()이 GameInstance 모델 준비 이전에 불릴 수 있어, 모델 구독은 첫 Open에서 1회만 건다.
    private void SubscribeModelOnce()
    {
        if (mIsModelSubscribed) return;
        mIsModelSubscribed = true;

        foreach (var slot in GameInstance.Model.Delivery.PickupSlots)
        {
            slot.Skip(1)
                .Subscribe(_ => RefreshPickupStatus())
                .AddTo(this);
        }
    }

    #region Recipe List

    private void SetupRecipeScroll()
    {
        var discovered = GameInstance.Model.RecipeBook.GetDiscoveredTids()
            .Where(tid => GameInstance.Model.Drink.Get(tid) != null)
            .OrderBy(tid => tid)
            .Select(tid => new UIScrollDrinkRecipeData { DrinkTid = tid })
            .ToList();

        mRecipeScrollEx.SetData(discovered);
    }

    private void OnSelectRecipe(UIScrollRow row)
    {
        if (row is not UIScrollDrinkRecipe recipeRow || recipeRow.CurrentData == null)
            return;

        int drinkTid = recipeRow.CurrentData.DrinkTid;
        var materials = GameInstance.Model.Drink.GetBaseMaterialTids(drinkTid);

        for (int i = 0; i < mBaseSlots.Length; i++)
            mBaseSlots[i].Set(i < materials.Count ? materials[i] : 0);

        var drinkRow = GameInstance.Model.Drink.Get(drinkTid)?.Row;
        if (drinkRow != null)
            mTemp = drinkRow.DrinkTempType;

        mPicker.Close();
        RefreshMaterialSlots();
    }

    private void RefreshRecipeHighlight()
    {
        var baseCounts = DrinkCraftHelper.CountMaterials(DrinkCraftHelper.GetFilledTids(mBaseSlots));
        bool matched = GameInstance.Model.Drink.TryGetRecipeMatch(baseCounts, mTemp, out int matchedTid);

        for (int i = 0; i < mRecipeScrollEx.ActiveCount; i++)
        {
            if (mRecipeScrollEx.GetRow(i) is UIScrollDrinkRecipe recipeRow)
                recipeRow.SetSelected(matched && recipeRow.CurrentData?.DrinkTid == matchedTid);
        }
    }

    #endregion

    #region Material Slots

    private void OnClickMaterialSlot(bool isBase, int index)
    {
        mEditingBase = isBase;
        mEditingIndex = index;

        var slot = GetEditingSlot();
        if (slot == null)
            return;

        var list = DrinkCraftHelper.BuildPickerList(mBaseSlots.Concat(mCustomSlots), slot);
        mPicker.Open(isBase ? "기본 재료 선택" : "커스텀 재료 선택", list, slot.IsFilled);
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
        RefreshMaterialSlots();
    }

    private UIDrinkMaterialSlot GetEditingSlot()
    {
        var slots = mEditingBase ? mBaseSlots : mCustomSlots;
        return mEditingIndex >= 0 && mEditingIndex < slots.Length ? slots[mEditingIndex] : null;
    }

    private void ClearMaterialSlots()
    {
        foreach (var slot in mBaseSlots) slot.Set(0);
        foreach (var slot in mCustomSlots) slot.Set(0);
        mEditingIndex = -1;
    }

    private void SetTemp(CTable.eDrinkTempType temp)
    {
        mTemp = temp;
        RefreshMaterialSlots();
    }

    private void RefreshMaterialSlots()
    {
        var baseTids = DrinkCraftHelper.GetFilledTids(mBaseSlots);
        var customTids = DrinkCraftHelper.GetFilledTids(mCustomSlots);

        mTextBaseTags.SetTextEx(DrinkCraftHelper.BuildTagText(baseTids));
        mTextCustomTags.SetTextEx(DrinkCraftHelper.BuildTagText(customTids));

        bool isHot = mTemp == CTable.eDrinkTempType.Hot;
        if (mHotSelectedMark != null) mHotSelectedMark.SetActive(isHot);
        if (mIceSelectedMark != null) mIceSelectedMark.SetActive(!isHot);
        if (mCupHot != null) mCupHot.SetActive(isHot);
        if (mCupIce != null) mCupIce.SetActive(!isHot);

        mBtnCraft.interactable = baseTids.Count > 0;

        RefreshRecipeHighlight();
    }

    #endregion

    #region Craft

    private void OnClickCraft()
    {
        var baseTids = DrinkCraftHelper.GetFilledTids(mBaseSlots);
        if (baseTids.Count == 0)
            return;

        // 완성 음료는 픽업대에만 보관되므로, 빈 칸이 없으면 재료를 쓰기 전에 막는다.
        if (GameInstance.Model.Delivery.FindEmptyPickupSlot() < 0)
        {
            ShowNotice("픽업대가 가득 찼어요. 배달하거나 폐기해 주세요.");
            return;
        }

        var customTids = DrinkCraftHelper.GetFilledTids(mCustomSlots);

        // 미발견 레시피는 커피머신에서 만들 수 없다 → 실패한 음료(얼음도 넣지 않음).
        bool matched = GameInstance.Model.Drink.TryGetRecipeMatch(DrinkCraftHelper.CountMaterials(baseTids), mTemp, out int drinkTid);
        bool known = matched && GameInstance.Model.RecipeBook.IsDiscovered(drinkTid);
        int iceCount = known ? GameInstance.Model.Drink.GetIceCount(drinkTid) : 0;

        var required = DrinkCraftHelper.BuildRequiredMaterials(baseTids.Concat(customTids), iceCount);
        if (DrinkCraftHelper.TryFindShortage(required, out string shortage))
        {
            ShowNotice($"{shortage}이(가) 부족해요.");
            return;
        }

        DrinkCraftHelper.Consume(required);

        string message = known ? "맛있게 완성되었어요."
            : matched ? "아직 개발하지 않은 레시피예요. 가공섬 레시피 연구소에서 먼저 개발해 주세요."
            : "등록 레시피와 다른 조합이에요.";

        var drink = PickupDrinkData.Create(known ? drinkTid : 0, mTemp, baseTids, customTids);

        ClearMaterialSlots();
        mPicker.Close();
        RefreshMaterialSlots();

        mCraftResult.Show(drink, message, GameInstance.Model.Delivery.FindEmptyPickupSlot() >= 0);
    }

    private void OnClickStoreResult()
    {
        if (!GameInstance.Model.Delivery.TryStorePickup(mCraftResult.Drink))
        {
            ShowNotice("픽업대에 빈 자리가 없어요.");
            return;
        }

        mCraftResult.Hide();
    }

    private void OnClickDiscardResult()
    {
        mCraftResult.Hide();
    }

    #endregion

    #region Delivery

    private void OnClickOpenDelivery()
    {
        UIMgr.Open<UIPopupDelivery, UIPopupDelivery.Param>(eUIType.PopupDelivery, new UIPopupDelivery.Param());
    }

    private void RefreshPickupStatus()
    {
        var slots = GameInstance.Model.Delivery.PickupSlots;
        int filled = slots.Count(slot => slot.Value != null);
        mTextPickupStatus.SetTextEx($"픽업대 {filled}/{slots.Length}");
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
