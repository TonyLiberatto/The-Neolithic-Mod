using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace TheNeolithicMod
{
    class BlockToolMoldReturnBlock : BlockToolMold
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel == null) return false;
            bool ok = (world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityToolMold)?.OnPlayerInteract(byPlayer, blockSel.Face, blockSel.HitPosition) != null ? true : false;
            if (ok)
            {
                if (Attributes["returnBlock"].Exists)
                {
                    world.BlockAccessor.SetBlock(Attributes["returnBlock"].AsString().WithDomain().ToBlock(api).BlockId, blockSel.Position);
                }
            }
            return ok;
        }
    }
}
