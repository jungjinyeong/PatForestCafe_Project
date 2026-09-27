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

        // 테이블의 Tags 컬럼은 CSV 구분자(,)와 겹치지 않도록 '|'로 구분해 저장한다.
        public static IEnumerable<string> GetTags(this DrinkMaterialRow materialRow) => ParseTags(materialRow?.Tags);
        public static IEnumerable<string> GetTags(this BreadMaterialRow materialRow) => ParseTags(materialRow?.Tags);
        public static IEnumerable<string> GetTags(this BreadRow breadRow) => ParseTags(breadRow?.Tags);

        // BreadMaterial1~5 중 0이 아닌 재료 Tid(중복 포함, 순서 유지).
        public static List<int> GetMaterialTids(this BreadRow breadRow)
        {
            var list = new List<int>();
            if (breadRow == null) return list;

            foreach (int tid in new[] { breadRow.BreadMaterial1, breadRow.BreadMaterial2, breadRow.BreadMaterial3, breadRow.BreadMaterial4, breadRow.BreadMaterial5 })
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