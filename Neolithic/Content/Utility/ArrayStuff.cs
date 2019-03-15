using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheNeolithicMod
{
    public static class ArrayStuff
    {
        public static uint index;

        public static T Next<T>(this T[] array)
        {
            return array[++index % array.Length];
        }
    }
}
