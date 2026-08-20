using System;
using System.Collections.Generic;

namespace CTable
{
    [Serializable]
    public class DrinkRow : TableBaseRow
    {
        public override int key => Tid;
        public int Tid;
        public eDrinkType DrinkType;
        public eDrinkTempType DrinkTempType;
        public int DrinkMaterial1;
        public int DrinkMaterial2;
        public int DrinkMaterial3;
        public int DrinkMaterial4;
        public int DrinkMaterial5;
        public int Weight;
        public string Atlas;
        public string Icon;
    }
}