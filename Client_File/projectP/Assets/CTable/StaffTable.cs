using System;
using System.Collections.Generic;
using System.Linq;

namespace CTable
{
    public class StaffTable : TableBaseGroup<StaffRow>
    {
        public override void Load(string[] lines)
        {
            for (int i = 3; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] values = line.Split(',');
                if (values.Length < 3) continue;

                var row = new StaffRow();
                row.Tid = int.TryParse(values[0].Trim(), out int _Tid) ? _Tid : 0;
                row.Name = values[1].Trim();
                row.WorkSpeed = float.TryParse(values[2].Trim(), out float _WorkSpeed) ? _WorkSpeed : 0f;

                AddRow(row.Tid, row);
            }
        }
    }
}