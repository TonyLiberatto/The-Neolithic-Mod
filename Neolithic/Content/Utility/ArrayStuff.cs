using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

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

        public static T Prev<T>(this T[] array, ref uint index)
        {
            index = index > 0 ? index - 1 : (uint)(array.Length-1);
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

        public static Block GetBlock(this BlockPos pos, IWorldAccessor world) { return world.BlockAccessor.GetBlock(pos); }
        public static Block GetBlock(this BlockPos pos, ICoreAPI api) { return pos.GetBlock(api.World); }

        public static Block GetBlock(this AssetLocation asset, ICoreAPI api)
        {
            if (api.World.BlockAccessor.GetBlock(asset) != null)
            {
                return api.World.BlockAccessor.GetBlock(asset);
            }
            return null;
        }

        public static Item GetItem(this AssetLocation asset, ICoreAPI api)
        {
            if (api.World.GetItem(asset) != null)
            {
                return api.World.GetItem(asset);
            }
            return null;
        }

        public static AssetLocation ToAsset(this string asset) { return new AssetLocation(asset); }

        public static void PlaySoundAtWithDelay(this IWorldAccessor world, AssetLocation location, BlockPos pos, int delay)
        {
            world.RegisterCallback(dt => world.PlaySoundAt(location, pos.X, pos.Y, pos.Z), delay);
        }

        public static T[] Stretch<T>(this T[] array, int amount)
        {
            if (amount < 1) return array;
            T[] parray = array;

            Array.Resize(ref array, array.Length + amount);
            for (int i = 0; i < array.Length; i++)
            {
                double scalar = (double)i / array.Length;
                array[i] = parray[(int)(scalar * parray.Length)];
            }
            return array;
        }

        public static int IndexOfMin(this IList<int> self)
        {
            if (self == null)
            {
                throw new ArgumentNullException("self");
            }

            if (self.Count == 0)
            {
                throw new ArgumentException("List is empty.", "self");
            }

            int min = self[0];
            int minIndex = 0;

            for (int i = 1; i < self.Count; ++i)
            {
                if (self[i] < min)
                {
                    min = self[i];
                    minIndex = i;
                }
            }
            return minIndex;
        }

        public static int IndexOfMin(this int[] self) => IndexOfMin(self.ToList());

        public static bool IsSurvival(this EnumGameMode gamemode) => gamemode == EnumGameMode.Survival;
        public static bool IsCreative(this EnumGameMode gamemode) => gamemode == EnumGameMode.Creative;
        public static bool IsSpectator(this EnumGameMode gamemode) => gamemode == EnumGameMode.Spectator;
        public static bool IsGuest(this EnumGameMode gamemode) => gamemode == EnumGameMode.Guest;

    }

    public class NAssetLocation : AssetLocation
    {
        public NAssetLocation(string domainAndPath) : base(domainAndPath)
        {
        }

        public NAssetLocation(string domain, string path) : base(domain, path)
        {
        }

        public static AssetLocation Create(string code, string domain)
        {
            return new AssetLocation(domain + ":" + code);
        }
    }
}
