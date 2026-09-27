using System;
using System.Collections.Generic;

namespace CTable
{
    [Serializable]
    public class SeasonRow : TableBaseRow
    {
        public override int key => Tid;
        public int Tid;
        public string Name;
        public float SaleRate;
        public float IceDrinkRate;
        public float HotDrinkRate;
    }
}