using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace TheNeolithicMod
{
    public static class ArrayStuff
    {
        static readonly string[] woodtypes = new string[] {
            "birch",
            "oak",
            "maple",
            "pine",
            "acacia",
            "kapok",
        };

        public static T Next<T>(this T[] array, ref uint index)
        {
            index = (uint)(++index % array.Length);
            return array[index];
        }

        public static bool MatchingWood(this AssetLocation asset, ref string woodtype)
        {
            foreach (string a in woodtypes)
            {
                if (asset.ToString().Contains(a))
                {
                    woodtype = a;
                    return true;
                }
            }
            woodtype = null;
            return false;
        }
    }
}
