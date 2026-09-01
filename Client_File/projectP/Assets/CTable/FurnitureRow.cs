using System;
using System.Collections.Generic;

namespace CTable
{
    [Serializable]
    public class FurnitureRow : TableBaseRow
    {
        public override int key => Tid;
        public int Tid;
        public string Name;
        public string Atlas;
        public string Icon;
        public string PrefabPath;
        public long Price;
    }
}