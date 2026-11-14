using System.Collections.Generic;
using System.Linq;

// 도감 레시피 목록(UIScrollCollectionData) 만들기. 로비 패널 도감 화면(UISideCollectionView)과 도감 상세 팝업(UIPopupCollection)이 같이 쓴다.
// 음료(Tid 순) → 디저트(Tid 순)로 도감 번호를 매긴다. 발견 여부는 RecipeBookModel.
public static class CollectionEntries
{
    public enum eFilter
    {
        All,
        Drink,
        Dessert,
    }

    public static readonly string[] FILTER_NAMES = { "전체", "음료", "디저트" };

    public static List<UIScrollCollectionData> Build()
    {
        var list = new List<UIScrollCollectionData>();

        var drinkTable = GameInstance.Table.GetTable<CTable.DrinkRow>();
        if (drinkTable != null)
        {
            foreach (var row in drinkTable.All.Values.OrderBy(r => r.Tid))
                list.Add(new UIScrollCollectionData { Tid = row.Tid, IsDrink = true });
        }

        var breadTable = GameInstance.Table.GetTable<CTable.BreadRow>();
        if (breadTable != null)
        {
            foreach (var row in breadTable.All.Values.OrderBy(r => r.Tid))
                list.Add(new UIScrollCollectionData { Tid = row.Tid, IsDrink = false });
        }

        for (int i = 0; i < list.Count; i++)
        {
            list[i].Number = i + 1;
            list[i].Discovered = GameInstance.Model.RecipeBook.IsDiscovered(list[i].Tid);
        }

        return list;
    }

    public static IEnumerable<UIScrollCollectionData> Filter(IEnumerable<UIScrollCollectionData> all, eFilter filter)
    {
        switch (filter)
        {
            case eFilter.Drink: return all.Where(e => e.IsDrink);
            case eFilter.Dessert: return all.Where(e => !e.IsDrink);
            default: return all;
        }
    }
}
