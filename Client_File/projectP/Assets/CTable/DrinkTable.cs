using System;
using System.Collections.Generic;
using System.Linq;

namespace CTable
{
    public class DrinkTable : TableBaseGroup<DrinkRow>
    {
        public override void Load(string[] lines)
        {
            for (int i = 3; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] values = line.Split(',');
                if (values.Length < 11) continue;

                var row = new DrinkRow();
                row.Tid = int.TryParse(values[0].Trim(), out int _Tid) ? _Tid : 0;
                row.DrinkType = (eDrinkType)Enum.Parse(typeof(eDrinkType), values[2].Trim());
                row.DrinkTempType = (eDrinkTempType)Enum.Parse(typeof(eDrinkTempType), values[3].Trim());
                row.DrinkIngredient1 = int.TryParse(values[4].Trim(), out int _DrinkIngredient1) ? _DrinkIngredient1 : 0;
                row.DrinkIngredient2 = int.TryParse(values[5].Trim(), out int _DrinkIngredient2) ? _DrinkIngredient2 : 0;
                row.DrinkIngredient3 = int.TryParse(values[6].Trim(), out int _DrinkIngredient3) ? _DrinkIngredient3 : 0;
                row.DrinkIngredient4 = int.TryParse(values[7].Trim(), out int _DrinkIngredient4) ? _DrinkIngredient4 : 0;
                row.DrinkIngredient5 = int.TryParse(values[8].Trim(), out int _DrinkIngredient5) ? _DrinkIngredient5 : 0;
                row.Weight = int.TryParse(values[9].Trim(), out int _Weight) ? _Weight : 0;
                row.Desc = values[10].Trim();

                AddRow(row.Tid, row);
            }
        }
    }
}