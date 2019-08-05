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
            BlockEntityToolMold be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityToolMold;
            if (be != null)
            {
                be.OnPlayerInteract(byPlayer, blockSel.Face, blockSel.HitPosition);

                if (be.IsFull && be.IsHardened && world.Side.IsServer())
                {
                    if (Attributes["returnBlock"].Exists)
                    {
                        world.BlockAccessor.SetBlock(Attributes["returnBlock"].AsString().ToBlock(api).BlockId, blockSel.Position);
                    }
                }
                return true;
            } 
            return false;
        }
    }
}
