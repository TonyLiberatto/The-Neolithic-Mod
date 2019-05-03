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

        public static Block GetBlock(this BlockPos pos, IWorldAccessor world)
        {
            return world.BlockAccessor.GetBlock(pos);
        }

        public static Block GetBlock(this AssetLocation asset)
        {
            return GetAPI.coreapi.World.BlockAccessor.GetBlock(asset);
        }

        public static Item GetItem(this AssetLocation asset)
        {
            return GetAPI.coreapi.World.GetItem(asset);
        }

        public static AssetLocation ToAsset(this string asset)
        {
            return new AssetLocation(asset);
        }

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
    }

    public class GetAPI : ModSystem
    {
        public static ICoreServerAPI sapi;
        public static ICoreClientAPI capi;
        public static ICoreAPI coreapi;

        public override double ExecuteOrder() => 0;

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
        }

        public override void Start(ICoreAPI api)
        {
            coreapi = api;
        }
    }
}
