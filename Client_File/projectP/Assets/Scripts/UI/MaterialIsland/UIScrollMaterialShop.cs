using UnityEngine;
using TMPro;
using Extension;
using UniRx;

public enum eShopEntryType
{
    Material,
    Item,
}

public class UIScrollMaterialShopData
{
    public int Tid;
    public string Name;
    public long Price;
    public eShopEntryType EntryType;
}

public class UIScrollMaterialShop : UIScrollRow<UIScrollMaterialShopData>
{
    [SerializeField] private TextMeshProUGUI mName;
    [SerializeField] private TextMeshProUGUI mPrice;
    [SerializeField] private UIButtonEx mBtnBuy;

    public UIScrollMaterialShopData CurrentData { get; private set; }

    private void Awake()
    {
        mBtnBuy.OnSubscribeOnClick(Select).AddTo(this);
    }

    protected override void OnSetData(UIScrollMaterialShopData data)
    {
        CurrentData = data;
        if (data == null) return;

        mName.SetTextEx(data.Name);
        mPrice.SetTextEx(data.Price.ToString());
    }
}
