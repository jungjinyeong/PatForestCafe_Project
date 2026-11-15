using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Extension;
using UnityEngine.Serialization;

public class UIRootIngredientIsland : UIWndBase
{
    public struct Param { }

    [Header("Scroll")]
    [SerializeField] private UIScrollEx mScrollEx;
    [SerializeField, FormerlySerializedAs("mMaterialGatherRowPrefab")] private GameObject mIngredientGatherRowPrefab;

    [Header("Buttons")]
    [SerializeField, FormerlySerializedAs("mBtnOpenBreadMinigame")] private UIButtonEx mBtnOpenDessertMinigame;
    [SerializeField, FormerlySerializedAs("mBtnOpenBreadProduction")] private UIButtonEx mBtnOpenDessertProduction;
    [SerializeField] private UIButtonEx mBtnOpenWorkshop;
    [SerializeField] private UIButtonEx mBtnOpenRecipeLab;
    [SerializeField] private UIButtonEx mBtnOpenShop;

    public override eUIType GetUIType() => eUIType.RootIngredientIsland;

    public override void Init()
    {
        base.Init();

        mScrollEx.Init(mIngredientGatherRowPrefab);
        mScrollEx.SetOnSelect(OnSelectIngredient);

        mBtnOpenDessertMinigame.OnSubscribeOnClick(OnClickOpenDessertMinigame).AddTo(this);
        mBtnOpenDessertProduction.OnSubscribeOnClick(OnClickOpenDessertProduction).AddTo(this);
        mBtnOpenWorkshop.OnSubscribeOnClick(OnClickOpenWorkshop).AddTo(this);
        mBtnOpenRecipeLab.OnSubscribeOnClick(OnClickOpenRecipeLab).AddTo(this);
        if (mBtnOpenShop != null) mBtnOpenShop.OnSubscribeOnClick(OnClickOpenShop).AddTo(this);
    }

    public override void Open()
    {
        base.Open();

        RefreshIngredientList();
    }

    public override void Destroy()
    {
        base.Destroy();
    }

    private void OnSelectIngredient(UIScrollRow row)
    {
        if (row is not UIScrollIngredientGather ingredientRow || ingredientRow.CurrentData == null)
            return;

        GameInstance.Model.Ingredient.Gather(ingredientRow.CurrentData.Tid);
        RefreshIngredientList();
    }

    private void OnClickOpenDessertMinigame()
    {
        GameInstance.UI.Open<UIPopupDessertMinigame, UIPopupDessertMinigame.Param>(eUIType.PopupDessertMinigame, new UIPopupDessertMinigame.Param());
    }

    private void OnClickOpenDessertProduction()
    {
        GameInstance.UI.Open<UIPopupDessertProduction, UIPopupDessertProduction.Param>(eUIType.PopupDessertProduction, new UIPopupDessertProduction.Param());
    }

    private void OnClickOpenWorkshop()
    {
        GameInstance.UI.Open<UIPopupWorkshop, UIPopupWorkshop.Param>(eUIType.PopupWorkshop, new UIPopupWorkshop.Param());
    }

    private void OnClickOpenRecipeLab()
    {
        GameInstance.UI.Open<UIPopupRecipeLab, UIPopupRecipeLab.Param>(eUIType.PopupRecipeLab, new UIPopupRecipeLab.Param());
    }

    // 상점가(재료 구입 등)는 로비와 같은 팝업을 연다.
    private void OnClickOpenShop()
    {
        GameInstance.UI.Open<UIPopupShopStreet, UIPopupShopStreet.Param>(eUIType.PopupShopStreet, new UIPopupShopStreet.Param());
    }

    // 클릭 채집 목록은 음료 재료 전용이다. 빵 재료는 UIPopupDessertMinigame으로만 획득한다.
    private void RefreshIngredientList()
    {
        var group = GameInstance.Table.GetTable<CTable.DrinkIngredientRow>();
        if (group == null)
        {
            Logger.Warning("[UIRootIngredientIsland] DrinkIngredientGroup을 찾을 수 없습니다.");
            return;
        }

        var dataList = new List<UIScrollIngredientGatherData>();
        foreach (var row in group.All.Values)
        {
            var ingredient = GameInstance.Model.Ingredient.Get(row.Tid);
            dataList.Add(new UIScrollIngredientGatherData
            {
                Tid = row.Tid,
                Name = row.Name,
                Count = ingredient?.Count.Value ?? 0,
            });
        }

        mScrollEx.SetData(dataList);
    }
}
