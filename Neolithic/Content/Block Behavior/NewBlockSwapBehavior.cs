using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TheNeolithicMod
{
    class SwapSystem : ModSystem
    {
        public Dictionary<string, object[]> SwapPairs { get; set; } = new Dictionary<string, object[]>();
    }

    class NewBlockSwapBehavior : BlockBehavior
    {
        ICoreAPI api;
        bool requireSneak = false;

        public NewBlockSwapBehavior(Block block) : base(block) { }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            this.api = api;
            PostOLInit(block.GetBehavior<NewBlockSwapBehavior>().properties);
        }

        public void PostOLInit(JsonObject properties)
        {
            SwapSystem swapSystem = api.ModLoader.GetModSystem<SwapSystem>();
            requireSneak = properties["requireSneak"].AsBool(requireSneak);

            object[][] objects = properties["swapBlocks"].AsObject<object[][]>();
            if (objects == null) return;
            foreach (var array in objects)
            {
                List<object> list = new List<object>();
                for (int i = 0; i < array.Length; i++)
                {
                    list.Add(array[i]);
                }
                if (!swapSystem.SwapPairs.ContainsKey(GetKey((string)array[0])) && !list.Any((a) => a.ToString().Contains("{")))
                {
                    swapSystem.SwapPairs.Add(GetKey((string)array[0]), list.ToArray());
                }
            }

        }

        public string GetKey(string holdingstack)
        {
            string combined = "";
            combined += holdingstack;
            combined += block.Code.ToString();
            return GameMath.Md5Hash(combined);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            SwapSystem swapSystem = api.ModLoader.GetModSystem<SwapSystem>();
            handling = EnumHandling.PreventDefault;
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            BlockPos pos = blockSel.Position;

            if (requireSneak && !byPlayer.Entity.Controls.Sneak) return true;

            if (slot.Itemstack != null)
            {
                string key = GetKey(slot.Itemstack.Collectible.Code.ToString());

                if (swapSystem.SwapPairs.TryGetValue(key, out object[] values))
                {
                    if (values.Length > 3 && values[2].ToString() != block.Code.ToString())
                    {
                        return true;
                    }
                    AssetLocation asset = slot.Itemstack.Collectible.Code;
                    if (asset.ToString() == values[0].ToString())
                    {
                        ((byPlayer.Entity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

                        string toCode = (string)values[1];
                        if (toCode.IndexOf(":") == -1) toCode = block.Code.Domain + ":" + toCode;
                        AssetLocation toAsset = new AssetLocation(toCode);
                        Block toBlock = toAsset.GetBlock(world.Api);

                        int count = 0;
                        try { count = Convert.ToInt32(values.Last()); } catch (Exception) { }

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
                            try
                            {
                                world.SpawnCubeParticles(pos, pos.ToVec3d().Add(0.5, 0.5, 0.5), 2, 16);
                                world.SpawnCubeParticles(pos.ToVec3d().Add(0.5, 0.5, 0.5), slot.Itemstack, 2, 16);
                            }
                            catch (Exception){}
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
