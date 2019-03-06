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
            IBlockAccessor bA = entityItem.World.BulkBlockAccessor;
            BlockPos pos = entityItem.Pos.AsBlockPos;
            if (entityItem.World.Side == EnumAppSide.Client) return;
            around.Shuffle(entityItem.World.Rand);

            foreach (BlockPos ipos in around)
            {
                if (bA.GetBlock(pos.X + ipos.X, pos.Y + ipos.Y, pos.Z + ipos.Z).IsReplacableBy(this) && bA.GetBlock(pos.X + ipos.X, pos.Y + ipos.Y - 1, pos.Z + ipos.Z).CollisionBoxes != null)
                {
                    bA.SetBlock(BlockId, pos);
                    entityItem.Die(EnumDespawnReason.Removed, null);
                    break;
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
