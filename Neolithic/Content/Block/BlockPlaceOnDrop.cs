using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            around = AreaAroundOffsetList().ToArray();
        }

        public override void OnGroundIdle(EntityItem entityItem)
        {
            if (entityItem.World.Side == EnumAppSide.Client || !entityItem.CollidedVertically) return;
            IBlockAccessor bA = entityItem.World.BlockAccessor;
            around.Shuffle(entityItem.World.Rand);

            foreach (BlockPos ipos in around)
            {
                BlockPos pos = entityItem.LocalPos.AsBlockPos.Add(ipos);
                if (bA.GetBlock(pos).IsReplacableBy(this) && !bA.GetBlock(pos.DownCopy()).IsReplacableBy(this))
                {
                    bA.SetBlock(BlockId, pos);
                    entityItem.Die(EnumDespawnReason.Removed, null);
                    return;
                }
            }
        }

        public List<BlockPos> AreaAroundOffsetList()
        {
            List<BlockPos> positions = new List<BlockPos>();
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        positions.Add(new BlockPos(x, y, z));
                    }
                }
            }
            return positions;
        }
    }
}
