using System.Collections.Generic;
using UnityEngine;
using Extension;

public class UIPopupRecipeBook : UIWndBase, IUIParam<UIPopupRecipeBook.Param>
{
    public struct Param
    {
    }

    [Header("Scroll")]
    [SerializeField] private UIScrollEx mScrollEx;
    [SerializeField] private GameObject mRecipeRowPrefab;

    public override eUIType GetUIType() => eUIType.UIPopupRecipeBook;

    public override void Init()
    {
        base.Init();

        mScrollEx.Init(mRecipeRowPrefab);
    }

    public override void Open()
    {
        base.Open();

        RefreshRecipeList();
    }

    public void Set(Param param)
    {
    }

    private void RefreshRecipeList()
    {
        var group = GameInstance.Table.GetTable<CTable.DrinkRow>();
        if (group == null)
        {
            Logger.Warning("[UIPopupRecipeBook] DrinkGroup을 찾을 수 없습니다.");
            return;
        }

        var dataList = new List<UIScrollRecipeBookData>();
        foreach (var row in group.All.Values)
        {
            string name = GameInstance.Model.Drink.Get(row.Tid)?.MenuItemRow?.Name ?? row.Tid.ToString();

            dataList.Add(new UIScrollRecipeBookData
            {
                Tid = row.Tid,
                Name = name,
                Discovered = GameInstance.Model.RecipeBook.IsDiscovered(row.Tid),
            });
        }

        mScrollEx.SetData(dataList);
    }
}
