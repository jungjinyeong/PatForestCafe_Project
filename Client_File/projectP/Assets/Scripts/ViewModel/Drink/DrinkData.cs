using CTable;
using Extension;

public class DrinkData
{
    public int TId { get; private set; }

    public DrinkRow Row { get; private set; }
    public MenuItemRow MenuItemRow { get; private set; }

    public static DrinkData Create(MenuItemRow menuItemRow)
    {
        if (null == menuItemRow)
        {
            return null;
        }

        return new DrinkData
        {
            Row = menuItemRow.GetDrinkrow(),
            MenuItemRow = menuItemRow,
            TId = menuItemRow.Tid
        };
    }

    public static DrinkData Create(DrinkRow drinkRow)
    {
        if (null == drinkRow)
        {
            return null;
        }
        return new DrinkData
        {
            Row = drinkRow,
            MenuItemRow = GameInstance.Table.Get<MenuItemRow>(drinkRow.Tid),
            TId = drinkRow.Tid
        };
    }
}
