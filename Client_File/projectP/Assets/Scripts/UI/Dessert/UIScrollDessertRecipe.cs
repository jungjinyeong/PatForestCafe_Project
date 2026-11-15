using System.Linq;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;
using UnityEngine.Serialization;

public class UIScrollDessertRecipeData
{
    public int DessertTid;
}

// 오븐 반죽 설정의 "해금된 디저트 레시피" 1행(발견한 빵 레시피만 표시).
public class UIScrollDessertRecipe : UIScrollRow<UIScrollDessertRecipeData>
{
    [SerializeField] private UIButtonEx mButton;
    [SerializeField] private TextMeshProUGUI mTextName;
    [SerializeField, FormerlySerializedAs("mTextMaterials")] private TextMeshProUGUI mTextIngredients;
    [SerializeField] private GameObject mSelectedMark;

    public UIScrollDessertRecipeData CurrentData { get; private set; }

    private void Awake()
    {
        mButton.OnSubscribeOnClick(Select).AddTo(this);
    }

    protected override void OnSetData(UIScrollDessertRecipeData data)
    {
        CurrentData = data;
        SetSelected(false);
        if (data == null) return;

        mTextName.SetTextEx(GameInstance.Table.Get<CTable.MenuItemRow>(data.DessertTid)?.Name ?? data.DessertTid.ToString());

        var ingredientTable = GameInstance.Table.GetTable<CTable.DessertIngredientRow>();
        var names = GameInstance.Table.Get<CTable.DessertRow>(data.DessertTid).GetIngredientTids()
            .Select(tid => ingredientTable?.Get(tid)?.Name ?? tid.ToString());
        mTextIngredients.SetTextEx(string.Join(" + ", names));
    }

    public void SetSelected(bool selected)
    {
        if (mSelectedMark != null)
            mSelectedMark.SetActive(selected);
    }
}
