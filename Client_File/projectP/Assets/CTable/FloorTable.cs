using System;
using System.Collections.Generic;
using System.Linq;

namespace CTable
{
    public class FloorTable : TableBaseGroup<FloorRow>
    {
        public override void Load(string[] lines)
        {
            for (int i = 3; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] values = line.Split(',');
                if (values.Length < 5) continue;

                var row = new FloorRow();
                row.Tid = int.TryParse(values[0].Trim(), out int _Tid) ? _Tid : 0;
                row.Name = values[1].Trim();
                row.FloorType = int.TryParse(values[2].Trim(), out int _FloorType) ? _FloorType : 0;
                row.UnlockOrder = int.TryParse(values[3].Trim(), out int _UnlockOrder) ? _UnlockOrder : 0;
                row.UnlockCost = long.TryParse(values[4].Trim(), out long _UnlockCost) ? _UnlockCost : 0;

                AddRow(row.Tid, row);
            }
        }
    }
}