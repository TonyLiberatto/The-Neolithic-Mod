using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace TheNeolithicMod
{
    class BlockNeolithicRoads : Block
    {
        private static uint index = 0;

        public string[] types = new string[] 
        {
            "bricks", "circle", "cobble", "fish", "squares", "tightbricks", "tightsquares"
        };

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot.Itemstack != null)
            {
                if (slot.Itemstack.Collectible.FirstCodePart() == "settinghammer")
                {
                    return true;
                }
            }
            return false;
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot.Itemstack != null)
            {
                if (slot.Itemstack.Collectible.FirstCodePart() == "settinghammer")
                {
                    return HandAnimations.Hit(world, byPlayer.Entity, secondsUsed);
                }
            }
            return false;
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot.Itemstack != null)
            {
                if (slot.Itemstack.Collectible.FirstCodePart() == "settinghammer")
                {
                    if (world.Side.IsServer())
                    {
                        Block nextBlock = new AssetLocation("neolithicmod:" + CodeWithoutParts(1) + "-" + types.Next(ref index)).GetBlock();
                        if (nextBlock == null) return;

                        world.PlaySoundAtWithDelay(nextBlock.Sounds.Place, blockSel.Position, 100);
                        world.PlaySoundAtWithDelay(new AssetLocation("sounds/effect/anvilhit"), blockSel.Position, 150);
                        world.BlockAccessor.SetBlock(nextBlock.BlockId, blockSel.Position);
                    }
                    return;
                }
            }
        }
    }
}
