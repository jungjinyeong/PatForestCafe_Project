using System;
using System.Collections.Generic;
using System.Linq;

namespace CTable
{
    public class DrinkMaterialTable : TableBaseGroup<DrinkMaterialRow>
    {
        public override void Load(string[] lines)
        {
            for (int i = 3; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] values = line.Split(',');
                if (values.Length < 8) continue;

                var row = new DrinkMaterialRow();
                row.Tid = int.TryParse(values[0].Trim(), out int _Tid) ? _Tid : 0;
                row.Name = values[1].Trim();
                row.Price = long.TryParse(values[2].Trim(), out long _Price) ? _Price : 0;
                row.Tags = values[3].Trim();
                row.Desc = values[4].Trim();
                row.Atlas = values[5].Trim();
                row.Icon = values[6].Trim();
                row.IsTopping = bool.TryParse(values[7].Trim(), out bool _IsTopping) && _IsTopping;

                AddRow(row.Tid, row);
            }
        }
    }
}