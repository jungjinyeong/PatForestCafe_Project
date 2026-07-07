using System;
using System.Collections.Generic;
using System.Linq;

namespace CTable
{
    public class ItemTable : TableBaseGroup<ItemRow>
    {
        public override void Load(string[] lines)
        {
            for (int i = 3; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] values = line.Split(',');
                if (values.Length < 5) continue;

                var row = new ItemRow();
                row.Tid = int.TryParse(values[0].Trim(), out int _Tid) ? _Tid : 0;
                row.ItemType = (eItemType)Enum.Parse(typeof(eItemType), values[1].Trim());
                row.ItemName = values[2].Trim();
                row.Atlas = values[3].Trim();
                row.Icon = values[4].Trim();

                AddRow(row.Tid, row);
            }
        }
    }
}