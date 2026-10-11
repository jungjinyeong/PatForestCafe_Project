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
        public int DrinkIngredient1;
        public int DrinkIngredient2;
        public int DrinkIngredient3;
        public int DrinkIngredient4;
        public int DrinkIngredient5;
        public int Weight;
        public string Desc;
    }
}