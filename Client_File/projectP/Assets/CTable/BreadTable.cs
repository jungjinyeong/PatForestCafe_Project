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
                if (values.Length < 7) continue;

                var row = new BreadRow();
                row.Tid = int.TryParse(values[0].Trim(), out int _Tid) ? _Tid : 0;
                row.BreadMaterial1 = int.TryParse(values[2].Trim(), out int _BreadMaterial1) ? _BreadMaterial1 : 0;
                row.BreadMaterial2 = int.TryParse(values[3].Trim(), out int _BreadMaterial2) ? _BreadMaterial2 : 0;
                row.BreadMaterial3 = int.TryParse(values[4].Trim(), out int _BreadMaterial3) ? _BreadMaterial3 : 0;
                row.BreadMaterial4 = int.TryParse(values[5].Trim(), out int _BreadMaterial4) ? _BreadMaterial4 : 0;
                row.BreadMaterial5 = int.TryParse(values[6].Trim(), out int _BreadMaterial5) ? _BreadMaterial5 : 0;

                AddRow(row.Tid, row);
            }
        }
    }
}