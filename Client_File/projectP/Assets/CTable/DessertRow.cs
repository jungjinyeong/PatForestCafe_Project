using System;
using System.Collections.Generic;

namespace CTable
{
    [Serializable]
    public class DessertRow : TableBaseRow
    {
        public override int key => Tid;
        public int Tid;
        public int DessertIngredient1;
        public int DessertIngredient2;
        public int DessertIngredient3;
        public int DessertIngredient4;
        public int DessertIngredient5;
        public long RecipePrice;
        public string Tags;
        public string Desc;
    }
}