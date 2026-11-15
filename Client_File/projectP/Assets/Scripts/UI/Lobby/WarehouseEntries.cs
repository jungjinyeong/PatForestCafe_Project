using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;

// 창고에 보관 중인 품목 목록. 창고 팝업(UIPopupWarehouse)과 로비 오른쪽 창고 패널(UISideWarehouseView)이 같이 쓴다.
// - 재료 = IngredientModel(음료 재료 + 빵 재료) 중 보유 수량이 있는 것.
// - 디저트 = 오븐에서 구워 창고에 보관 중인(진열 전) 빵 재고(DessertData.ProducedCount).
public static class WarehouseEntries
{
    public enum eFilter
    {
        All,
        Ingredients,
        Desserts,
    }

    public static List<UIWarehouseCellData> Get(eFilter filter)
    {
        var list = new List<UIWarehouseCellData>();

        if (filter != eFilter.Desserts)
        {
            list.AddRange(GameInstance.Model.Ingredient.GetAll()
                .Where(m => m.Count.Value > 0)
                .OrderBy(m => m.Tid)
                .Select(m => new UIWarehouseCellData { Tid = m.Tid, IsDessert = false, Name = m.Name, Count = m.Count.Value }));
        }

        if (filter != eFilter.Ingredients)
        {
            list.AddRange(GameInstance.Model.Dessert.GetAll()
                .Where(b => b.ProducedCount.Value > 0)
                .OrderBy(b => b.TId)
                .Select(b => new UIWarehouseCellData { Tid = b.TId, IsDessert = true, Name = b.MenuItemRow?.Name ?? b.TId.ToString(), Count = b.ProducedCount.Value }));
        }

        return list;
    }

    // 재료 수량이나 디저트 보관 수량이 바뀌면 발행한다(같은 프레임의 여러 변화는 한 번으로 묶는다).
    public static IObservable<Unit> ObserveChanges()
    {
        var changes = GameInstance.Model.Ingredient.GetAll().Select(m => m.Count.Skip(1).AsUnitObservable())
            .Concat(GameInstance.Model.Dessert.GetAll().Select(b => b.ProducedCount.Skip(1).AsUnitObservable()));

        return Observable.Merge(changes).ThrottleFrame(1);
    }
}
