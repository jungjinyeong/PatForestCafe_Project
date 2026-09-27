using System;
using System.Collections.Generic;

namespace CTable
{
    [Serializable]
    public class GameTimeRow : TableBaseRow
    {
        public override int key => Tid;
        public int Tid;
        public float DayDurationSeconds;
        public int OpenHour;
        public int CloseHour;
        public float ClosingTimeoutSeconds;
        public int SeasonDays;
    }
}