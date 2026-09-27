using System;
using System.Collections.Generic;
using System.Linq;

namespace CTable
{
    public class GameTimeTable : TableBaseGroup<GameTimeRow>
    {
        public override void Load(string[] lines)
        {
            for (int i = 3; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] values = line.Split(',');
                if (values.Length < 6) continue;

                var row = new GameTimeRow();
                row.Tid = int.TryParse(values[0].Trim(), out int _Tid) ? _Tid : 0;
                row.DayDurationSeconds = float.TryParse(values[1].Trim(), out float _DayDurationSeconds) ? _DayDurationSeconds : 0f;
                row.OpenHour = int.TryParse(values[2].Trim(), out int _OpenHour) ? _OpenHour : 0;
                row.CloseHour = int.TryParse(values[3].Trim(), out int _CloseHour) ? _CloseHour : 0;
                row.ClosingTimeoutSeconds = float.TryParse(values[4].Trim(), out float _ClosingTimeoutSeconds) ? _ClosingTimeoutSeconds : 0f;
                row.SeasonDays = int.TryParse(values[5].Trim(), out int _SeasonDays) ? _SeasonDays : 0;

                AddRow(row.Tid, row);
            }
        }
    }
}