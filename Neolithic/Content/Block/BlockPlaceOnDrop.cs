using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheNeolithicMod.Utility;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace TheNeolithicMod
{
    class BlockPlaceOnDropNew : Block
    {
        BlockPos[] around;
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            around = AreaMethods.AreaAroundOffsetList().ToArray();
        }

        public override void OnGroundIdle(EntityItem entityItem)
        {
            if (entityItem.World.Side == EnumAppSide.Client || !entityItem.CollidedVertically) return;
            IBlockAccessor bA = entityItem.World.BlockAccessor;
            BlockPos pos = entityItem.LocalPos.AsBlockPos;
            if (TryPlace(bA, pos, entityItem)) return;

            around.Shuffle(entityItem.World.Rand);

            foreach (BlockPos ipos in around)
            {
                BlockPos tpos = pos.Add(ipos);
                if (TryPlace(bA, tpos, entityItem)) return;
            }
        }

        public bool TryPlace(IBlockAccessor bA, BlockPos pos, EntityItem item)
        {
            Block rBlock = bA.GetBlock(pos);
            Block dBlock = bA.GetBlock(pos.DownCopy());

            if (rBlock.IsReplacableBy(this) && !dBlock.IsReplacableBy(this))
            {
                bA.SetBlock(BlockId, pos);
                item.Die(EnumDespawnReason.Removed, null);
                return true;
            }
            return false;
        }
    }
}
