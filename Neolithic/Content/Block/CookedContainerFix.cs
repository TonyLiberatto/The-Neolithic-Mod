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

		public new void ServeIntoBowlStack(ItemSlot bowlSlot, ItemSlot potslot, IWorldAccessor world)
		{
			if (world.Side == EnumAppSide.Client) return;

			string code = bowlSlot.Itemstack.Block.Attributes["mealBlockCode"].AsString();
			BlockMeal mealblock = api.World.GetBlock(new AssetLocation(code)) as BlockMeal;

			ItemStack[] stacks = GetContents(api.World, potslot.Itemstack);

			int quantityServings = GetServings(world, potslot.Itemstack);
			int servingsToTransfer = Math.Min(quantityServings, bowlSlot.Itemstack.Block.Attributes["servingCapacity"].AsInt(1));

			ItemStack stack = new ItemStack(mealblock);
			mealblock.SetContents(GetRecipeCode(world, potslot.Itemstack), stack, stacks, servingsToTransfer);

			SetServings(world, potslot.Itemstack, quantityServings - servingsToTransfer);

			if (quantityServings - servingsToTransfer <= 0)
			{
				potslot.Itemstack = new ItemStack(api.World.GetBlock(new AssetLocation(CodeWithoutParts(1) + "-burned")));
			}

			potslot.MarkDirty();

			bowlSlot.Itemstack = stack;
			bowlSlot.MarkDirty();
		}

		internal int GetServings(IWorldAccessor world, ItemStack byItemStack)
		{
			return byItemStack.Attributes.GetInt("servings");
		}

		internal void SetServings(IWorldAccessor world, ItemStack byItemStack, int value)
		{
			byItemStack.Attributes.SetInt("servings", value);
		}
	}
}
