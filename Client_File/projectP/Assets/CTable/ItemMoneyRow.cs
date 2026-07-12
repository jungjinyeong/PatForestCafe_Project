using System;
using System.Collections.Generic;

namespace CTable
{
    [Serializable]
    public class ItemMoneyRow : TableBaseRow
    {
        public override int key => Tid;
        public int Tid;
        public eMoneyType MoneyType;
    }
}