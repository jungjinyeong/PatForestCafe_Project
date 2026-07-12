using System;
using System.Collections.Generic;
using System.Linq;

namespace CTable
{
    public class ItemMoneyTable : TableBaseGroup<ItemMoneyRow>
    {
        public override void Load(string[] lines)
        {
            for (int i = 3; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] values = line.Split(',');
                if (values.Length < 3) continue;

                var row = new ItemMoneyRow();
                row.Tid = int.TryParse(values[0].Trim(), out int _Tid) ? _Tid : 0;
                row.MoneyType = (eMoneyType)Enum.Parse(typeof(eMoneyType), values[2].Trim());

                AddRow(row.Tid, row);
            }
        }
    }
}