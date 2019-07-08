using System;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TheNeolithicMod
{
    class BlockCreateBehavior : BlockBehavior
    {
        private object[][] createBlocks;
        private AssetLocation makes;
        private bool t;
        private int count;
        ICoreAPI api;

        public BlockCreateBehavior(Block block) : base(block) { }

        public void PostOLInit(JsonObject properties)
        {
            try
            {
                createBlocks = properties["createBlocks"].AsObject<object[][]>();
            }
            catch (Exception)
            {
                createBlocks = null;
                api.World.Logger.Notification("CreateBlocks error in " + block.Code.ToString());
            }
            
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            this.api = api;
            PostOLInit(block.GetBehavior<BlockCreateBehavior>().properties);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            if (createBlocks == null)
            {
                world.Logger.Notification("CreateBlocks error in " + block.Code.ToString());
                return true;
            }
            handling = EnumHandling.PreventDefault;
            var active = byPlayer.InventoryManager.ActiveHotbarSlot;
            BlockPos pos = blockSel.Position;
            t = false;
            if (active.Itemstack != null)
            {
                foreach (var val in createBlocks)
                {
                    if (active.Itemstack.Collectible.WildCardMatch(new AssetLocation(val[0].ToString())))
                    {
                        makes = new AssetLocation(val[1].ToString());
                        count = Convert.ToInt32(val[2]);
                        t = true;
                        break;
                    }
                }
                if (t && active.Itemstack.StackSize >= count)
                {
                    
                    if (world.Side == EnumAppSide.Client) world.PlaySoundAt(block.Sounds.Place, pos.X, pos.Y, pos.Z);
                    if (count < 0 && active.Itemstack.StackSize >= 64 )
                    {
                        world.SpawnItemEntity(new ItemStack(active.Itemstack.Collectible, -count), pos.ToVec3d().Add(0.5, 0.5, 0.5), new Vec3d(0.0, 0.1, 0.0));
                    }
                    else
                    {
                        active.Itemstack.StackSize -= count;
                    }
                    if (active.Itemstack.StackSize <= 0) active.Itemstack = null;
                    world.SpawnItemEntity(new ItemStack(world.GetBlock(makes)), pos.ToVec3d().Add(0.5, 0.5, 0.5), new Vec3d(0.0, 0.1, 0.0));
                    try
                    {
                        if (world.Side.IsClient()) world.SpawnCubeParticles(pos.ToVec3d().Add(0.5, 0.5, 0.5), active.Itemstack, 2, 16);
                    }
                    catch (Exception)
                    {
                        world.Logger.Error("Could not create particles, missing itemstack?");
                    }
                    
                    active.MarkDirty();
                    return true;
                }
            }
            return true;
        }
    }
}
