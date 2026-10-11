using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;
using Extension;
using UnityEngine.Serialization;

public class UIScrollDrinkRecipeData
{
    public int DrinkTid;
}

// 커피머신 좌측 레시피 목록 1행("음료 = 재료 + 재료"). 도감에 발견된 레시피만 표시한다.
public class UIScrollDrinkRecipe : UIScrollRow<UIScrollDrinkRecipeData>
{
    [SerializeField] private UIButtonEx mButton;
    [SerializeField] private Image mImgIcon;
    [SerializeField] private TextMeshProUGUI mTextName;
    [SerializeField, FormerlySerializedAs("mTextMaterials")] private TextMeshProUGUI mTextIngredients;
    [SerializeField] private GameObject mSelectedMark;

    public UIScrollDrinkRecipeData CurrentData { get; private set; }

    private void Awake()
    {
        mButton.OnSubscribeOnClick(Select).AddTo(this);
    }

    protected override void OnSetData(UIScrollDrinkRecipeData data)
    {
        CurrentData = data;
        SetSelected(false);
        if (data == null) return;

        var menuRow = GameInstance.Table.Get<CTable.MenuItemRow>(data.DrinkTid);
        mImgIcon.SetSpriteEx(menuRow?.Atlas, menuRow?.Icon);
        mTextName.SetTextEx(GameInstance.Model.Drink.GetName(data.DrinkTid));

        var ingredientTable = GameInstance.Table.GetTable<CTable.DrinkIngredientRow>();
        var names = GameInstance.Model.Drink.GetBaseIngredientTids(data.DrinkTid)
            .Select(tid => ingredientTable?.Get(tid)?.Name ?? tid.ToString());
        mTextIngredients.SetTextEx("= " + string.Join(" + ", names));
    }

    public void SetSelected(bool selected)
    {
        if (mSelectedMark != null)
            mSelectedMark.SetActive(selected);
    }
}
