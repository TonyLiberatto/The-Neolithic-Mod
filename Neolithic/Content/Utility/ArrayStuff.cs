using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheNeolithicMod
{
    public static class ArrayStuff
    {
        public static T Next<T>(this T[] array, ref uint index)
        {
            return array[++index % array.Length];
        }
    }
}
