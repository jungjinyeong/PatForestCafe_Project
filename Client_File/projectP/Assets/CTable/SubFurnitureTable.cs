using System;
using System.Collections.Generic;
using System.Linq;

namespace CTable
{
    public class SubFurnitureTable : TableBaseGroup<SubFurnitureRow>
    {
        public override void Load(string[] lines)
        {
            for (int i = 3; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] values = line.Split(',');
                if (values.Length < 9) continue;

                var row = new SubFurnitureRow();
                row.Tid = int.TryParse(values[0].Trim(), out int _Tid) ? _Tid : 0;
                row.Name = values[1].Trim();
                row.Atlas = values[2].Trim();
                row.Icon = values[3].Trim();
                row.PrefabPath = values[4].Trim();
                row.Price = long.TryParse(values[5].Trim(), out long _Price) ? _Price : 0;
                row.Width = int.TryParse(values[6].Trim(), out int _Width) ? _Width : 0;
                row.Height = int.TryParse(values[7].Trim(), out int _Height) ? _Height : 0;
                row.AllowedGroupId = int.TryParse(values[8].Trim(), out int _AllowedGroupId) ? _AllowedGroupId : 0;

                AddRow(row.Tid, row);
            }
        }
    }
}