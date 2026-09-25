using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;
using Extension;

// 커피머신 제작 결과 팝업(UI_Popup_SpecialDrinkProduction 내부 패널). [픽업대로 이동] / [폐기].
public class UIDrinkCraftResult : UIBase
{
    private const string FAILED_TITLE = "음… 실패한 음료";
    private const string NO_TAG_TEXT = "속성 없음";

    [SerializeField] private TextMeshProUGUI mTextTitle;
    [SerializeField] private TextMeshProUGUI mTextMessage;
    [SerializeField] private TextMeshProUGUI mTextTags;
    [SerializeField] private Image mImgDrink;
    [SerializeField] private GameObject mSuccessRoot;
    [SerializeField] private GameObject mFailedRoot;
    [SerializeField] private UIButtonEx mBtnStore;
    [SerializeField] private UIButtonEx mBtnDiscard;

    public PickupDrinkData Drink { get; private set; }

    public void Init(Action onStore, Action onDiscard)
    {
        mBtnStore.OnSubscribeOnClick(onStore).AddTo(this);
        mBtnDiscard.OnSubscribeOnClick(onDiscard).AddTo(this);

        Deative();
    }

    public void Show(PickupDrinkData drink, string message, bool canStore)
    {
        Drink = drink;
        Active();

        bool success = !drink.IsFailed;
        var menuRow = success ? GameInstance.Table.Get<CTable.MenuItemRow>(drink.DrinkTid) : null;

        mTextTitle.SetTextEx(success ? $"{GameInstance.Model.Drink.GetName(drink.DrinkTid)} 완성!" : FAILED_TITLE);
        mTextMessage.SetTextEx(message);
        mTextTags.SetTextEx(success && drink.Tags.Count > 0 ? string.Join(", ", drink.Tags) : NO_TAG_TEXT);
        mImgDrink.SetSpriteEx(menuRow?.Atlas, menuRow?.Icon);

        if (mSuccessRoot != null) mSuccessRoot.SetActive(success);
        if (mFailedRoot != null) mFailedRoot.SetActive(!success);

        mBtnStore.interactable = canStore;
    }

    public void Hide()
    {
        Drink = null;
        Deative();
    }
}
