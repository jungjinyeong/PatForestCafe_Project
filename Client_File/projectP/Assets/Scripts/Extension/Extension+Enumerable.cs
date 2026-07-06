using System;
using System.Collections.Generic;
using System.Linq;

namespace Extension
{
    public static class ExtensionEnumerable
    {
        public static void Each<T>(this IEnumerable<T> values, Action<T> itemAction)
        {
            if (null == values)
                return;

            if (values.Count() <= 0)
                return;
            
            foreach(var item in values)
            {
                itemAction?.Invoke(item);
            }
        }
    }
}