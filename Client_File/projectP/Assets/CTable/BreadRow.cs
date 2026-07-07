using System;
using System.Collections.Generic;

namespace CTable
{
    [Serializable]
    public class BreadRow : TableBaseRow
    {
        public override int key => Tid;
        public int Tid;
        public string BreadName;
        public string Atlas;
        public string Icon;
        public long Price;
    }
}