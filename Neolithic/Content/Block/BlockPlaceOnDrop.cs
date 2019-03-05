using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TheNeolithicMod
{
    class BlockPlaceOnDropNew : Block
    {
        public override void OnGroundIdle(EntityItem entityItem)
        {
            IBlockAccessor bA = entityItem.World.BulkBlockAccessor;
            BlockPos pos = entityItem.Pos.AsBlockPos;

            if (entityItem.World.Side == EnumAppSide.Client || !bA.GetBlock(pos).IsReplacableBy(this)) return;

            bA.SetBlock(BlockId, pos);
            entityItem.Die(EnumDespawnReason.Removed, null);
        }

        public override void OnHeldInteractStart(IItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handHandling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, ref handHandling);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
}
