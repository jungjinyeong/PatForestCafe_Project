using System;
using System.Collections.Generic;

namespace CTable
{
    [Serializable]
    public class BreadRow : TableBaseRow
    {
        public override int key => Tid;
        public int Tid;
        public int BreadIngredient1;
        public int BreadIngredient2;
        public int BreadIngredient3;
        public int BreadIngredient4;
        public int BreadIngredient5;
        public long RecipePrice;
        public string Tags;
        public string Desc;
    }
}