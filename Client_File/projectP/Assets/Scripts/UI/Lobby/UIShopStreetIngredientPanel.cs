using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;
using UnityEngine.Serialization;

// 상점가 찍찍이 [재료 구입] 서브 패널. 음료/빵 재료와 일반 아이템을 골드로 구매한다.
// 가공섬 클릭 채집(무료)과 별개의 유료 획득 경로다. 예전 별도 팝업(UIPopupIngredientShop)을 상점가 안으로 옮긴 것이다.
public class UIShopStreetIngredientPanel : MonoBehaviour
{
    private const float NOTICE_SECONDS = 2f;

    [SerializeField] private UIScrollEx mScrollEx;
    [SerializeField, FormerlySerializedAs("mMaterialShopRowPrefab")] private GameObject mIngredientShopRowPrefab;
    [SerializeField] private TextMeshProUGUI mTextNotice;

    private readonly SerialDisposable mNoticeDisposable = new SerialDisposable();

    public void Init()
    {
        mScrollEx.Init(mIngredientShopRowPrefab);
        mScrollEx.SetOnSelect(OnSelectIngredient);
        mNoticeDisposable.AddTo(this);
    }

    public void Show(bool show)
    {
        gameObject.SetActive(show);
        if (!show)
            return;

        mNoticeDisposable.Disposable = null;
        mTextNotice.SetTextEx(string.Empty);
        RefreshList();
    }

    private void OnSelectIngredient(UIScrollRow row)
    {
        if (row is not UIScrollIngredientShop shopRow || shopRow.CurrentData == null)
            return;

        var gold = GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold);
        if (gold == null || gold.Count.Value < shopRow.CurrentData.Price)
        {
            ShowNotice("골드가 부족해요.");
            return;
        }

        gold.Consume((int)shopRow.CurrentData.Price);

        if (shopRow.CurrentData.EntryType == eShopEntryType.Item)
            GameInstance.Model.Item.Add(shopRow.CurrentData.Tid);
        else
            GameInstance.Model.Ingredient.Gather(shopRow.CurrentData.Tid);

        ShowNotice($"{shopRow.CurrentData.Name} 1개를 샀어요.");
        RefreshList();
    }

    private void RefreshList()
    {
        var dataList = new List<UIScrollIngredientShopData>();

        var drinkIngredientGroup = GameInstance.Table.GetTable<CTable.DrinkIngredientRow>();
        if (drinkIngredientGroup != null)
        {
            foreach (var row in drinkIngredientGroup.All.Values)
                dataList.Add(new UIScrollIngredientShopData { Tid = row.Tid, Name = row.Name, Price = row.Price, OwnedCount = GetIngredientCount(row.Tid), EntryType = eShopEntryType.Ingredient });
        }

        var breadIngredientGroup = GameInstance.Table.GetTable<CTable.BreadIngredientRow>();
        if (breadIngredientGroup != null)
        {
            // 빵 재료는 등급(하/중/고급)별로 모두 판매한다 — 오븐 반죽 설정에서 같은 분류 안에서 등급을 교체해 품질을 올린다.
            foreach (var row in breadIngredientGroup.All.Values)
            {
                var quality = BreadQuality.FromGrade(row.Grade);
                string name = $"<color={BreadQuality.GetColorHex(quality)}>[{BreadQuality.GetName(quality)}]</color> {row.Name}";
                dataList.Add(new UIScrollIngredientShopData { Tid = row.Tid, Name = name, Price = row.Price, OwnedCount = GetIngredientCount(row.Tid), EntryType = eShopEntryType.Ingredient });
            }
        }

        // Money(골드)는 골드로 사는 게 의미가 없으므로 제외 — 그 외 일반 아이템(레시피 개발북 등)만 상점에 노출한다.
        var itemGroup = GameInstance.Table.GetTable<CTable.ItemRow>();
        if (itemGroup != null)
        {
            foreach (var row in itemGroup.All.Values)
            {
                if (row.ItemType == CTable.eItemType.Money)
                    continue;

                dataList.Add(new UIScrollIngredientShopData { Tid = row.Tid, Name = row.ItemName, Price = row.Price, OwnedCount = GameInstance.Model.Item.Get(row.Tid)?.Count.Value ?? 0, EntryType = eShopEntryType.Item });
            }
        }

        mScrollEx.SetData(dataList);
    }

    private static int GetIngredientCount(int tid) => GameInstance.Model.Ingredient.Get(tid)?.Count.Value ?? 0;

    private void ShowNotice(string message)
    {
        mTextNotice.SetTextEx(message);
        mNoticeDisposable.Disposable = Observable.Timer(TimeSpan.FromSeconds(NOTICE_SECONDS))
            .Subscribe(_ => mTextNotice.SetTextEx(string.Empty));
    }
}
