using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TheNeolithicMod
{
    class NewBlockSwapBehavior : BlockBehavior
    {
        const int floc = 2;
        Dictionary<string, object[]> swapPairs = new Dictionary<string, object[]>();

        public NewBlockSwapBehavior(Block block) : base(block){}

        public override void Initialize(JsonObject properties)
        {
            object[][] objects = properties["swapBlocks"].AsObject<object[][]>();
            if (objects == null) return;
            foreach (var array in objects)
            {
                List<object> list = new List<object>();
                List<object> keylist = new List<object>();
                for (int i = 0; i < array.Length; i++)
                {
                    keylist.Add(array[i]);
                    if (i != floc) list.Add(array[i]);
                }
                swapPairs.Add(GetKey(keylist), list.ToArray());
            }
            
        }
        public string GetKey(List<object> objects)
        {
            string combined = "";
            combined += (string)objects[0];
            combined += (string)objects[2];
            return GameMath.Md5Hash(combined);
        }
        public string GetKey(List<string> objects)
        {
            string combined = "";
            combined += objects[0];
            combined += objects[1];
            return GameMath.Md5Hash(combined);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventDefault;
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            BlockPos pos = blockSel.Position;
            List<string> things = new List<string>() { slot.Itemstack.Collectible.Code.ToString(), blockSel.Position.GetBlock(world).Code.ToString()};
            string key = GetKey(things);

            if (slot.Itemstack != null)
            {
                if (swapPairs.TryGetValue(key, out object[] values))
                {
                    AssetLocation asset = slot.Itemstack.Collectible.Code;
                    if (asset.ToString() == values[0].ToString())
                    {
                        string toCode = (string)values[1];
                        if (toCode.IndexOf(":") == -1) toCode = block.Code.Domain + ":" + toCode;
                        AssetLocation toAsset = new AssetLocation(toCode);
                        Block toBlock = toAsset.GetBlock(world.Api);

                        int count = Convert.ToInt32(values.Last());

                        if (count > 0 && slot.Itemstack.StackSize >= count)
                        {
                            if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Survival) slot.TakeOut(count); slot.MarkDirty();
                            world.BlockAccessor.SetBlock(toBlock.BlockId, pos);
                        }
                        else if (count == 0)
                        {
                            world.BlockAccessor.SetBlock(toBlock.BlockId, pos);
                        }
                        
                        if (world.Side.IsServer())
                        {
                            world.SpawnCubeParticles(pos, pos.ToVec3d().Add(0.5, 0.5, 0.5), 2, 16);
                            world.SpawnCubeParticles(pos.ToVec3d().Add(0.5, 0.5, 0.5), slot.Itemstack, 2, 16);
                        }
                        else
                        {
                            world.PlaySoundAt(block.Sounds.Place, pos.X, pos.Y, pos.Z);
                        }
                    }
                }
            }
            return true;
        }
    }
}
