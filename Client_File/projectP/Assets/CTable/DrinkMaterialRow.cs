using System;
using System.Collections.Generic;

namespace CTable
{
    [Serializable]
    public class DrinkMaterialRow : TableBaseRow
    {
        public override int key => Tid;
        public int Tid;
        public string Name;
        public long Price;
        public string Tags;
        public string Desc;
        public string Atlas;
        public string Icon;
        public bool IsTopping;
    }
}