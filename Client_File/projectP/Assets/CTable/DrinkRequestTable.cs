using System;
using System.Collections.Generic;
using System.Linq;

namespace CTable
{
    public class DrinkRequestTable : TableBaseGroup<DrinkRequestRow>
    {
        public override void Load(string[] lines)
        {
            for (int i = 3; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] values = line.Split(',');
                if (values.Length < 3) continue;

                var row = new DrinkRequestRow();
                row.Tid = int.TryParse(values[0].Trim(), out int _Tid) ? _Tid : 0;
                row.DrinkTid = int.TryParse(values[1].Trim(), out int _DrinkTid) ? _DrinkTid : 0;
                row.Weight = int.TryParse(values[2].Trim(), out int _Weight) ? _Weight : 0;

                AddRow(row.Tid, row);
            }
        }
    }
}