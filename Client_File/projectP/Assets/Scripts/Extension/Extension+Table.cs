using CTable;
using System.Collections.Generic;
using System.Linq;

namespace Extension
{
    public static class ExtensionTable
    {
        public static DrinkRow GetDrinkrow(this MenuItemRow menuItemRow)
        {
            return GameInstance.Table.Get<DrinkRow>(menuItemRow.Tid);
        }

        public static DessertRow GetDessertRow(this MenuItemRow menuItemRow)
        {
            return GameInstance.Table.Get<DessertRow>(menuItemRow.Tid);
        }

        // 테이블의 Tags 컬럼은 CSV 구분자(,)와 겹치지 않도록 '|'로 구분해 저장한다.
        public static IEnumerable<string> GetTags(this DrinkIngredientRow ingredientRow) => ParseTags(ingredientRow?.Tags);
        public static IEnumerable<string> GetTags(this DessertIngredientRow ingredientRow) => ParseTags(ingredientRow?.Tags);
        public static IEnumerable<string> GetTags(this DessertRow dessertRow) => ParseTags(dessertRow?.Tags);

        // DessertIngredient1~5 중 0이 아닌 재료 Tid(중복 포함, 순서 유지).
        public static List<int> GetIngredientTids(this DessertRow dessertRow)
        {
            var list = new List<int>();
            if (dessertRow == null) return list;

            foreach (int tid in new[] { dessertRow.DessertIngredient1, dessertRow.DessertIngredient2, dessertRow.DessertIngredient3, dessertRow.DessertIngredient4, dessertRow.DessertIngredient5 })
            {
                if (tid != 0) list.Add(tid);
            }
            return list;
        }

        private static IEnumerable<string> ParseTags(string tags)
        {
            if (string.IsNullOrEmpty(tags))
                return Enumerable.Empty<string>();

            return tags.Split('|').Select(t => t.Trim()).Where(t => t.Length > 0);
        }

    }
}