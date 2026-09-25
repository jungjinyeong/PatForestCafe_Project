using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;
using Extension;

// 완성 음료 픽업대 1칸.
public class UIPickupSlot : UIBase
{
    private const string FAILED_DRINK_NAME = "실패한 음료";

    [SerializeField] private UIButtonEx mButton;
    [SerializeField] private GameObject mDrinkRoot;
    [SerializeField] private GameObject mEmptyRoot;
    [SerializeField] private GameObject mSelectedMark;
    [SerializeField] private Image mImgDrink;
    [SerializeField] private TextMeshProUGUI mTextName;
    [SerializeField] private TextMeshProUGUI mTextDetail;

    public PickupDrinkData Drink { get; private set; }

    public void Init(Action onClick)
    {
        mButton.OnSubscribeOnClick(onClick).AddTo(this);
    }

    public void Set(PickupDrinkData drink)
    {
        Drink = drink;

        mButton.interactable = drink != null;
        if (mDrinkRoot != null) mDrinkRoot.SetActive(drink != null);
        if (mEmptyRoot != null) mEmptyRoot.SetActive(drink == null);
        SetSelected(false);

        if (drink == null)
            return;

        var menuRow = drink.IsFailed ? null : GameInstance.Table.Get<CTable.MenuItemRow>(drink.DrinkTid);
        mImgDrink.SetSpriteEx(menuRow?.Atlas, menuRow?.Icon);
        mTextName.SetTextEx(drink.IsFailed ? FAILED_DRINK_NAME : GameInstance.Model.Drink.GetName(drink.DrinkTid));
        mTextDetail.SetTextEx(!drink.IsFailed && drink.Tags.Count > 0 ? string.Join(", ", drink.Tags) : string.Empty);
    }

    public void SetSelected(bool selected)
    {
        if (mSelectedMark != null)
            mSelectedMark.SetActive(selected && Drink != null);
    }
}
