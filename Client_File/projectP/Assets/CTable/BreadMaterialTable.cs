using System;
using System.Collections.Generic;
using System.Linq;

namespace CTable
{
    public class BreadMaterialTable : TableBaseGroup<BreadMaterialRow>
    {
        public override void Load(string[] lines)
        {
            for (int i = 3; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] values = line.Split(',');
                if (values.Length < 7) continue;

                var row = new BreadMaterialRow();
                row.Tid = int.TryParse(values[0].Trim(), out int _Tid) ? _Tid : 0;
                row.Name = values[1].Trim();
                row.Price = long.TryParse(values[2].Trim(), out long _Price) ? _Price : 0;
                row.Category = values[3].Trim();
                row.Grade = int.TryParse(values[4].Trim(), out int _Grade) ? _Grade : 0;
                row.Tags = values[5].Trim();
                row.Desc = values[6].Trim();

                AddRow(row.Tid, row);
            }
        }
    }
}