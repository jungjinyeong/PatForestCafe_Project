using System;
using System.Collections.Generic;
using System.Linq;

namespace CTable
{
    public class DrinkMaterialGroup : TableBaseGroup<DrinkMaterialRow>
    {
        public override void Load(string[] lines)
        {
            for (int i = 3; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] values = line.Split(',');
                if (values.Length < 2) continue;

                var row = new DrinkMaterialRow();
                row.Tid = int.TryParse(values[0].Trim(), out int _Tid) ? _Tid : 0;
                row.Name = values[1].Trim();

                AddRow(row.Tid, row);
            }
        }
    }
}