using UnityEngine;
using TMPro;
using Extension;
using UniRx;

public enum eShopEntryType
{
    Ingredient,
    Item,
}

public class UIScrollIngredientShopData
{
    public int Tid;
    public string Name;
    public long Price;
    public int OwnedCount;
    public eShopEntryType EntryType;
}

public class UIScrollIngredientShop : UIScrollRow<UIScrollIngredientShopData>
{
    [SerializeField] private TextMeshProUGUI mName;
    [SerializeField] private TextMeshProUGUI mOwned;
    [SerializeField] private TextMeshProUGUI mPrice;
    [SerializeField] private UIButtonEx mBtnBuy;

    public UIScrollIngredientShopData CurrentData { get; private set; }

    private void Awake()
    {
        mBtnBuy.OnSubscribeOnClick(Select).AddTo(this);
    }

    protected override void OnSetData(UIScrollIngredientShopData data)
    {
        CurrentData = data;
        if (data == null) return;

        mName.SetTextEx(data.Name);
        if (mOwned != null) mOwned.SetTextEx($"보유 {data.OwnedCount}");
        mPrice.SetTextEx($"{data.Price} G");
    }
}
