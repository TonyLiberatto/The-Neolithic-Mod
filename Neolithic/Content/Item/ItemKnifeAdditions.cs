using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TheNeolithicMod
{
    class ItemKnifeAdditions : ItemKnife
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            BlockPos pos = blockSel.Position;
            Block block = pos.GetBlock(byEntity.World);
            if (block != null && byEntity.Controls.Sneak && (block.FirstCodePart().Contains("skinned") || block.FirstCodePart().Contains("dead")))
            {
                AssetLocation location = new AssetLocation("game:sounds/player/scrape");
                byEntity.World.PlaySoundAt(location, pos.X, pos.Y, pos.Z, byEntity as IPlayer);
                handling = EnumHandHandling.Handled;
                return;
            }
            else
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, ref handling);
            }
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            Block block = blockSel.Position.GetBlock(byEntity.World);
            if (block != null && byEntity.Controls.Sneak && (block.FirstCodePart().Contains("skinned") || block.FirstCodePart().Contains("dead")))
            {
                return HandAnimations.Skin(byEntity, secondsUsed);
            }
            else
            {
                return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);
            }
        }

        public bool preventmultiple = true;

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            Block block = blockSel.Position.GetBlock(byEntity.World);
            if (preventmultiple && byEntity.World.Side.IsServer() && block != null && (block.FirstCodePart().Contains("skinned") || block.FirstCodePart().Contains("dead")))
            {
                preventmultiple = false;
                byEntity.World.RegisterCallback(dt => preventmultiple = true, 5000);
                //byEntity.World.BlockAccessor.BreakBlock(blockSel.Position, byEntity as IPlayer);
                //this.OnBlockBreaking(byEntity as IPlayer, blockSel, slot, 0, secondsUsed, 4);
                block.OnBlockBroken(byEntity.World, blockSel.Position, byEntity as IPlayer);
            }
        }
    }
}
