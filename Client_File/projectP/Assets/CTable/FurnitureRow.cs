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
        public string VisualPath;
        public long Price;
        public int Width;
        public int Height;
        public int GroupId;
        public int FixedType;
        public int LayoutOrder;
        public int FurnitureType;
        public int Category;
    }
}