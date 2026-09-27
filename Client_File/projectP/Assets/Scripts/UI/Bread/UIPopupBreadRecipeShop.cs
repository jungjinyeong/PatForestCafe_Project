using System;
using System.Linq;
using UnityEngine;
using TMPro;
using UniRx;
using Extension;

// 빵 레시피 상점(프리팹 UI_Popup_BreadRecipeShop, 상점가 찍찍이 [빵 레시피]).
// 골드(BreadRow.RecipePrice)로 빵 레시피를 사면 레시피 도감에 등록되고 오븐 반죽 설정에서 고를 수 있다.
public class UIPopupBreadRecipeShop : UIWndBase, IUIParam<UIPopupBreadRecipeShop.Param>
{
    public struct Param
    {
    }

    private const float NOTICE_SECONDS = 2f;

    [SerializeField] private UIScrollEx mScrollEx;
    [SerializeField] private GameObject mRowPrefab;
    [SerializeField] private TextMeshProUGUI mTextNotice;

    private readonly SerialDisposable mNoticeDisposable = new SerialDisposable();

    public override eUIType GetUIType() => eUIType.PopupBreadRecipeShop;

    public override void Init()
    {
        base.Init();

        mScrollEx.Init(mRowPrefab);
        mScrollEx.SetOnSelect(OnSelectRecipe);
        mNoticeDisposable.AddTo(this);
    }

    public override void Open()
    {
        base.Open();

        mTextNotice.SetTextEx(string.Empty);
        RefreshList();
    }

    public void Set(Param param)
    {
    }

    private void RefreshList()
    {
        var table = GameInstance.Table.GetTable<CTable.BreadRow>();
        if (table == null) return;

        // 아직 없는 레시피를 먼저, 그다음 보유 중인 레시피.
        var list = table.All.Values
            .Select(row => new UIScrollBreadRecipeShopData
            {
                BreadTid = row.Tid,
                Price = row.RecipePrice,
                Owned = GameInstance.Model.RecipeBook.IsDiscovered(row.Tid),
            })
            .OrderBy(d => d.Owned)
            .ThenBy(d => d.BreadTid)
            .ToList();

        mScrollEx.SetData(list);
    }

    private void OnSelectRecipe(UIScrollRow row)
    {
        if (row is not UIScrollBreadRecipeShop shopRow || shopRow.CurrentData == null || shopRow.CurrentData.Owned)
            return;

        var data = shopRow.CurrentData;
        var gold = GameInstance.Model.Item.GetWealth(CTable.eMoneyType.Gold);
        if (gold == null || gold.Count.Value < data.Price)
        {
            ShowNotice("골드가 부족해요.");
            return;
        }

        gold.Consume((int)data.Price);
        GameInstance.Model.RecipeBook.Discover(data.BreadTid);

        string name = GameInstance.Table.Get<CTable.MenuItemRow>(data.BreadTid)?.Name ?? data.BreadTid.ToString();
        ShowNotice($"{name} 레시피를 배웠어요! 오븐에서 구울 수 있어요.");
        RefreshList();
    }

    private void ShowNotice(string message)
    {
        mTextNotice.SetTextEx(message);
        mNoticeDisposable.Disposable = Observable.Timer(TimeSpan.FromSeconds(NOTICE_SECONDS))
            .Subscribe(_ => mTextNotice.SetTextEx(string.Empty));
    }
}
