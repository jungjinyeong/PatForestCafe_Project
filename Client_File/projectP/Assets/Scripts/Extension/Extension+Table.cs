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

        public static BreadRow GetBreadRow(this MenuItemRow menuItemRow)
        {
            return GameInstance.Table.Get<BreadRow>(menuItemRow.Tid);
        }

        // DrinkMaterial.csv의 Tags 컬럼은 CSV 구분자(,)와 겹치지 않도록 '|'로 구분해 저장한다.
        public static IEnumerable<string> GetTags(this DrinkMaterialRow materialRow)
        {
            if (materialRow == null || string.IsNullOrEmpty(materialRow.Tags))
                return Enumerable.Empty<string>();

            return materialRow.Tags.Split('|').Select(t => t.Trim()).Where(t => t.Length > 0);
        }

    }
}