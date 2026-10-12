using System;
using System.Collections.Generic;

namespace CTable
{
    [Serializable]
    public class FloorRow : TableBaseRow
    {
        public override int key => Tid;
        public int Tid;
        public string Name;
        public int FloorType;
        public int UnlockOrder;
        public long UnlockCost;
    }
}