using System;
using System.Collections.Generic;

namespace CTable
{
    [Serializable]
    public class BreadRow : TableBaseRow
    {
        public override int key => Tid;
        public int Tid;
        public int BreadMaterial1;
        public int BreadMaterial2;
        public int BreadMaterial3;
        public int BreadMaterial4;
        public int BreadMaterial5;
    }
}