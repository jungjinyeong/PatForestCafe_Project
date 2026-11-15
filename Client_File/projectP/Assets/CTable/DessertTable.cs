using System;
using System.Collections.Generic;
using System.Linq;

namespace CTable
{
    public class DessertTable : TableBaseGroup<DessertRow>
    {
        public override void Load(string[] lines)
        {
            for (int i = 3; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] values = line.Split(',');
                if (values.Length < 10) continue;

                var row = new DessertRow();
                row.Tid = int.TryParse(values[0].Trim(), out int _Tid) ? _Tid : 0;
                row.DessertIngredient1 = int.TryParse(values[2].Trim(), out int _DessertIngredient1) ? _DessertIngredient1 : 0;
                row.DessertIngredient2 = int.TryParse(values[3].Trim(), out int _DessertIngredient2) ? _DessertIngredient2 : 0;
                row.DessertIngredient3 = int.TryParse(values[4].Trim(), out int _DessertIngredient3) ? _DessertIngredient3 : 0;
                row.DessertIngredient4 = int.TryParse(values[5].Trim(), out int _DessertIngredient4) ? _DessertIngredient4 : 0;
                row.DessertIngredient5 = int.TryParse(values[6].Trim(), out int _DessertIngredient5) ? _DessertIngredient5 : 0;
                row.RecipePrice = long.TryParse(values[7].Trim(), out long _RecipePrice) ? _RecipePrice : 0;
                row.Tags = values[8].Trim();
                row.Desc = values[9].Trim();

                AddRow(row.Tid, row);
            }
        }
    }
}