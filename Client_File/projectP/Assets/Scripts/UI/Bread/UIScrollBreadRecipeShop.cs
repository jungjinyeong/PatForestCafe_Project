using UnityEngine;
using TMPro;
using UniRx;
using Extension;

public class UIScrollBreadRecipeShopData
{
    public int BreadTid;
    public long Price;
    public bool Owned;
}

// 빵 레시피 상점 1행 — 이름·설명·가격, 이미 가진 레시피는 "보유 중".
public class UIScrollBreadRecipeShop : UIScrollRow<UIScrollBreadRecipeShopData>
{
    [SerializeField] private UIButtonEx mBtnBuy;
    [SerializeField] private TextMeshProUGUI mTextName;
    [SerializeField] private TextMeshProUGUI mTextDesc;
    [SerializeField] private TextMeshProUGUI mTextPrice;

    public UIScrollBreadRecipeShopData CurrentData { get; private set; }

    private void Awake()
    {
        mBtnBuy.OnSubscribeOnClick(Select).AddTo(this);
    }

    protected override void OnSetData(UIScrollBreadRecipeShopData data)
    {
        CurrentData = data;
        if (data == null) return;

        var breadRow = GameInstance.Table.Get<CTable.BreadRow>(data.BreadTid);
        mTextName.SetTextEx(GameInstance.Table.Get<CTable.MenuItemRow>(data.BreadTid)?.Name ?? data.BreadTid.ToString());
        mTextDesc.SetTextEx(breadRow?.Desc ?? string.Empty);
        mTextPrice.SetTextEx(data.Owned ? "보유 중" : $"{data.Price:N0} G");
        mBtnBuy.interactable = !data.Owned;
    }
}
