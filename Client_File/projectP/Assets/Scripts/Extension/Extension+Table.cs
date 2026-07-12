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

    }
}