using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace TheNeolithicMod
{
    class BlockNeolithicRoads : Block
    {
        public readonly string[] types = new string[] 
        {
            "bricks", "circle", "cobble", "fish", "squares", "tightbricks", "tightsquares"
        };

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot.Itemstack != null)
            {
                if (IsSettingHammer(slot))
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
                if (IsSettingHammer(slot))
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
                if (IsSettingHammer(slot))
                {
                    if (world.Side.IsServer())
                    {
                        uint index = (world.BlockAccessor.GetBlockEntity(blockSel.Position) as BENeolithicRoads).index;
                        Block nextBlock;

                        if (byPlayer.Entity.Controls.Sneak) nextBlock = new AssetLocation("neolithicmod:" + CodeWithoutParts(1) + "-" + types.Prev(ref index)).GetBlock();
                        else nextBlock = new AssetLocation("neolithicmod:" + CodeWithoutParts(1) + "-" + types.Next(ref index)).GetBlock();

                        if (nextBlock == null) return;
                        (world.BlockAccessor.GetBlockEntity(blockSel.Position) as BENeolithicRoads).index = index;

                        world.PlaySoundAtWithDelay(nextBlock.Sounds.Place, blockSel.Position, 100);
                        world.PlaySoundAtWithDelay(new AssetLocation("sounds/effect/anvilhit"), blockSel.Position, 150);
                        world.BlockAccessor.SetBlock(nextBlock.BlockId, blockSel.Position);
                    }
                    return;
                }
            }
        }

        public bool IsSettingHammer(ItemSlot slot) => slot.Itemstack.Collectible.FirstCodePart() == "settinghammer";

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
        {
            if (world.BlockAccessor.GetBlock(pos).Class != this.Class) base.OnBlockRemoved(world, pos);
        }
    }

    class BENeolithicRoads : BlockEntity
    {
        public uint index = 0;
    }
}
