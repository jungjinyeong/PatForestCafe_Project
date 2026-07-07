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
                if (values.Length < 9) continue;

                var row = new DrinkRow();
                row.Tid = int.TryParse(values[0].Trim(), out int _Tid) ? _Tid : 0;
                row.Name = values[1].Trim();
                row.DrinkType = (eDrinkType)Enum.Parse(typeof(eDrinkType), values[2].Trim());
                row.DrinkTempType = (eDrinkTempType)Enum.Parse(typeof(eDrinkTempType), values[3].Trim());
                row.DrinkMaterial1 = int.TryParse(values[4].Trim(), out int _DrinkMaterial1) ? _DrinkMaterial1 : 0;
                row.DrinkMaterial2 = int.TryParse(values[5].Trim(), out int _DrinkMaterial2) ? _DrinkMaterial2 : 0;
                row.DrinkMaterial3 = int.TryParse(values[6].Trim(), out int _DrinkMaterial3) ? _DrinkMaterial3 : 0;
                row.DrinkMaterial4 = int.TryParse(values[7].Trim(), out int _DrinkMaterial4) ? _DrinkMaterial4 : 0;
                row.DrinkMaterial5 = int.TryParse(values[8].Trim(), out int _DrinkMaterial5) ? _DrinkMaterial5 : 0;

                AddRow(row.Tid, row);
            }
        }
    }
}