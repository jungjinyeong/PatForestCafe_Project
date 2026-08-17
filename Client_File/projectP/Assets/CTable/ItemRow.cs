using System;
using System.Collections.Generic;

namespace CTable
{
    [Serializable]
    public class ItemRow : TableBaseRow
    {
        public override int key => Tid;
        public int Tid;
        public eItemType ItemType;
        public string ItemName;
        public string Atlas;
        public string Icon;
        public long Price;
    }
}