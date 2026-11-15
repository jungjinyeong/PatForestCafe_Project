using UnityEngine;
using TMPro;
using UniRx;
using Extension;

public class UIScrollDessertRecipeShopData
{
    public int DessertTid;
    public long Price;
    public bool Owned;
}

// 빵 레시피 상점 1행 — 이름·설명·가격, 이미 가진 레시피는 "보유 중".
public class UIScrollDessertRecipeShop : UIScrollRow<UIScrollDessertRecipeShopData>
{
    [SerializeField] private UIButtonEx mBtnBuy;
    [SerializeField] private TextMeshProUGUI mTextName;
    [SerializeField] private TextMeshProUGUI mTextDesc;
    [SerializeField] private TextMeshProUGUI mTextPrice;

    public UIScrollDessertRecipeShopData CurrentData { get; private set; }

    private void Awake()
    {
        mBtnBuy.OnSubscribeOnClick(Select).AddTo(this);
    }

    protected override void OnSetData(UIScrollDessertRecipeShopData data)
    {
        CurrentData = data;
        if (data == null) return;

        var dessertRow = GameInstance.Table.Get<CTable.DessertRow>(data.DessertTid);
        mTextName.SetTextEx(GameInstance.Table.Get<CTable.MenuItemRow>(data.DessertTid)?.Name ?? data.DessertTid.ToString());
        mTextDesc.SetTextEx(dessertRow?.Desc ?? string.Empty);
        mTextPrice.SetTextEx(data.Owned ? "보유 중" : $"{data.Price:N0} G");
        mBtnBuy.interactable = !data.Owned;
    }
}
