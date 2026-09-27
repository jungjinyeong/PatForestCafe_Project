using System;
using System.Collections.Generic;
using System.Linq;

namespace CTable
{
    public class SeasonTable : TableBaseGroup<SeasonRow>
    {
        public override void Load(string[] lines)
        {
            for (int i = 3; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] values = line.Split(',');
                if (values.Length < 5) continue;

                var row = new SeasonRow();
                row.Tid = int.TryParse(values[0].Trim(), out int _Tid) ? _Tid : 0;
                row.Name = values[1].Trim();
                row.SaleRate = float.TryParse(values[2].Trim(), out float _SaleRate) ? _SaleRate : 0f;
                row.IceDrinkRate = float.TryParse(values[3].Trim(), out float _IceDrinkRate) ? _IceDrinkRate : 0f;
                row.HotDrinkRate = float.TryParse(values[4].Trim(), out float _HotDrinkRate) ? _HotDrinkRate : 0f;

                AddRow(row.Tid, row);
            }
        }
    }
}