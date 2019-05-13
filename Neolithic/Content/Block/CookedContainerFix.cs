using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TheNeolithicMod
{
    class CookedContainerFix : BlockCookedContainer
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
			if (activeHotbarSlot.Empty) return true;

            CollectibleObject collectible = activeHotbarSlot.Itemstack.Collectible;
            if (!activeHotbarSlot.Empty && collectible.FirstCodePart() == "bowl" && collectible.Variant["type"] == "burned")
            {
                CookedContainerFixBE container = world.BlockAccessor.GetBlockEntity(blockSel.Position) as CookedContainerFixBE;
                if (container == null) return false;

                container.ServePlayer(byPlayer);
                return true;
            }
            ItemStack itemstack = OnPickBlock(world, blockSel.Position);
            if (!byPlayer.InventoryManager.TryGiveItemstack(itemstack, true)) return base.OnBlockInteractStart(world, byPlayer, blockSel);

            world.BlockAccessor.SetBlock(0, blockSel.Position);
            world.PlaySoundAt(Sounds.Place, byPlayer, byPlayer, true, 32f, 1f);
            return true;
        }
    }
}
