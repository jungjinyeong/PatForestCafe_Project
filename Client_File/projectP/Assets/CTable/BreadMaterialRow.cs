using System;
using System.Collections.Generic;

namespace CTable
{
    [Serializable]
    public class BreadMaterialRow : TableBaseRow
    {
        public override int key => Tid;
        public int Tid;
        public string Name;
        public long Price;
        public string Category;
        public int Grade;
        public string Tags;
        public string Desc;
    }
}