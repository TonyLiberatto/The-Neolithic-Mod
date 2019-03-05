using System;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TheNeolithicMod
{
    class BlockSwapBehavior : BlockBehavior
    {
        private object[][] swapBlocks;
        private AssetLocation makes;
        private AssetLocation takes;
        private bool t;
        private int count;

        public BlockSwapBehavior(Block block) : base(block) { }

        public override void Initialize(JsonObject properties)
        {
            swapBlocks = properties["swapBlocks"].AsObject<object[][]>();
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventDefault;
            var active = byPlayer.InventoryManager.ActiveHotbarSlot;
            BlockPos pos = blockSel.Position;
            t = false;
            if (active.Itemstack != null)
            {
                foreach (var val in swapBlocks)
                {
                    if (active.Itemstack.Collectible.WildCardMatch(new AssetLocation(val[0].ToString())))
                    {
                        makes = new AssetLocation(val[1].ToString());
                        takes = new AssetLocation(val[2].ToString());
                        count = Convert.ToInt32(val[3]);
                        t = true;
                        break;
                    }
                }
                if (t && active.Itemstack.StackSize >= count && world.BlockAccessor.GetBlock(pos).WildCardMatch(takes))
                {
                    if (world.Side == EnumAppSide.Client) world.PlaySoundAt(block.Sounds.Place, pos.X, pos.Y, pos.Z);
                    if (count < 0 && active.Itemstack.StackSize >= 64)
                    {
                        world.SpawnItemEntity(new ItemStack(active.Itemstack.Collectible, -count), pos.ToVec3d().Add(0.5, 0.5, 0.5), new Vec3d(0.0, 0.05, 0.0));
                    }
                    else
                    {
                        active.Itemstack.StackSize -= count;
                    }
                    if (active.Itemstack.StackSize <= 0) active.Itemstack = null;
                    world.BlockAccessor.SetBlock(world.GetBlock(makes).BlockId, pos);

                    active.MarkDirty();
                    return true;
                }
            }
            return true;
        }
    }
}
