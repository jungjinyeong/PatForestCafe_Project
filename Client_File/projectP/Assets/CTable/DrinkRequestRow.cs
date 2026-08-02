using System;
using System.Collections.Generic;

namespace CTable
{
    [Serializable]
    public class DrinkRequestRow : TableBaseRow
    {
        public override int key => Tid;
        public int Tid;
        public int DrinkTid;
        public int Weight;
    }
}