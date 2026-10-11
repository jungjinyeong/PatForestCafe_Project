using System;
using System.Collections.Generic;
using System.Linq;

namespace CTable
{
    public class BreadTable : TableBaseGroup<BreadRow>
    {
        public override void Load(string[] lines)
        {
            for (int i = 3; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] values = line.Split(',');
                if (values.Length < 10) continue;

                var row = new BreadRow();
                row.Tid = int.TryParse(values[0].Trim(), out int _Tid) ? _Tid : 0;
                row.BreadIngredient1 = int.TryParse(values[2].Trim(), out int _BreadIngredient1) ? _BreadIngredient1 : 0;
                row.BreadIngredient2 = int.TryParse(values[3].Trim(), out int _BreadIngredient2) ? _BreadIngredient2 : 0;
                row.BreadIngredient3 = int.TryParse(values[4].Trim(), out int _BreadIngredient3) ? _BreadIngredient3 : 0;
                row.BreadIngredient4 = int.TryParse(values[5].Trim(), out int _BreadIngredient4) ? _BreadIngredient4 : 0;
                row.BreadIngredient5 = int.TryParse(values[6].Trim(), out int _BreadIngredient5) ? _BreadIngredient5 : 0;
                row.RecipePrice = long.TryParse(values[7].Trim(), out long _RecipePrice) ? _RecipePrice : 0;
                row.Tags = values[8].Trim();
                row.Desc = values[9].Trim();

                AddRow(row.Tid, row);
            }
        }
    }
}