using System.Collections.Generic;
using UnityEngine;

// 음료/빵 재료를 골드로 구매하는 상점. 가공섬 클릭 채집(무료)과 별개의 유료 획득 경로다.
public class UIPopupMaterialShop : UIWndBase, IUIParam<UIPopupMaterialShop.Param>
{
    public struct Param
    {
    }

    [Header("Scroll")]
    [SerializeField] private UIScrollEx mScrollEx;
    [SerializeField] private GameObject mMaterialShopRowPrefab;

    public override eUIType GetUIType() => eUIType.UIPopupMaterialShop;

    public override void Init()
    {
        base.Init();

        mScrollEx.Init(mMaterialShopRowPrefab);
        mScrollEx.SetOnSelect(OnSelectMaterial);
    }

    public override void Open()
    {
        base.Open();

        RefreshList();
    }

    public void Set(Param param)
    {
    }

    private void OnSelectMaterial(UIScrollRow row)
    {
        if (row is not UIScrollMaterialShop shopRow || shopRow.CurrentData == null)
            return;

        var gold = GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold);
        if (gold == null || gold.Count.Value < shopRow.CurrentData.Price)
        {
            Logger.Log("[UIPopupMaterialShop] 골드가 부족합니다.");
            return;
        }

        gold.Consume((int)shopRow.CurrentData.Price);

        if (shopRow.CurrentData.EntryType == eShopEntryType.Item)
            GameInstance.Model.Item.Add(shopRow.CurrentData.Tid);
        else
            GameInstance.Model.Material.Gather(shopRow.CurrentData.Tid);

        RefreshList();
    }

    private void RefreshList()
    {
        var dataList = new List<UIScrollMaterialShopData>();

        var drinkMaterialGroup = GameInstance.Table.GetTable<CTable.DrinkMaterialRow>();
        if (drinkMaterialGroup != null)
        {
            foreach (var row in drinkMaterialGroup.All.Values)
                dataList.Add(new UIScrollMaterialShopData { Tid = row.Tid, Name = row.Name, Price = row.Price, EntryType = eShopEntryType.Material });
        }

        var breadMaterialGroup = GameInstance.Table.GetTable<CTable.BreadMaterialRow>();
        if (breadMaterialGroup != null)
        {
            foreach (var row in breadMaterialGroup.All.Values)
                dataList.Add(new UIScrollMaterialShopData { Tid = row.Tid, Name = row.Name, Price = row.Price, EntryType = eShopEntryType.Material });
        }

        // Money(골드)는 골드로 사는 게 의미가 없으므로 제외 — 그 외 일반 아이템(레시피 개발북 등)만 상점에 노출한다.
        var itemGroup = GameInstance.Table.GetTable<CTable.ItemRow>();
        if (itemGroup != null)
        {
            foreach (var row in itemGroup.All.Values)
            {
                if (row.ItemType == CTable.eItemType.Money)
                    continue;

                dataList.Add(new UIScrollMaterialShopData { Tid = row.Tid, Name = row.ItemName, Price = row.Price, EntryType = eShopEntryType.Item });
            }
        }

        mScrollEx.SetData(dataList);
    }
}
