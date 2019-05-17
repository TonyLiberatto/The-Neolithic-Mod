using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace TheNeolithicMod
{
	class BlockFirePitOverride : BlockFirepit
	{
		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			int stage = Stage;
			ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;

			if (stage == 5)
			{
				BlockEntityFirepit bef = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityFirepit;

				if (bef != null && stack != null && byPlayer.Entity.Controls.Sneak)
				{
					if (stack.Collectible.CombustibleProps != null && stack.Collectible.CombustibleProps.MeltingPoint > 0)
					{
						ItemStackMoveOperation op = new ItemStackMoveOperation(world, EnumMouseButton.Button1, 0, EnumMergePriority.DirectMerge, 1);
						byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(bef.inputSlot, ref op);
						if (op.MovedQuantity > 0) return true;
					}

					if (stack.Collectible.CombustibleProps != null && stack.Collectible.CombustibleProps.BurnTemperature > 0)
					{
						ItemStackMoveOperation op = new ItemStackMoveOperation(world, EnumMouseButton.Button1, 0, EnumMergePriority.DirectMerge, 1);
						byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(bef.fuelSlot, ref op);
						if (op.MovedQuantity > 0) return true;
					}
				}

				if (stack?.Collectible is BlockBowl && (stack.Collectible as BlockBowl)?.BowlContentItemCode() == null && stack.Collectible.Attributes?["mealContainer"].AsBool() == true)
				{
					ItemSlot potSlot = null;
					if (bef?.inputStack?.Collectible is CookedContainerFix)
					{
						potSlot = bef.inputSlot;
					}
					if (bef?.outputStack?.Collectible is CookedContainerFix)
					{
						potSlot = bef.outputSlot;
					}

					if (potSlot != null)
					{
						CookedContainerFix blockPot = potSlot.Itemstack.Collectible as CookedContainerFix;
						ItemSlot targetSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
						if (byPlayer.InventoryManager.ActiveHotbarSlot.StackSize > 1)
						{
							targetSlot = new DummySlot(targetSlot.TakeOut(1));
							byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
							blockPot.ServeIntoBowlStack(targetSlot, potSlot, world);
							if (!byPlayer.InventoryManager.TryGiveItemstack(targetSlot.Itemstack, true))
							{
								world.SpawnItemEntity(targetSlot.Itemstack, byPlayer.Entity.ServerPos.XYZ);
							}
						}
						else
						{
							blockPot.ServeIntoBowlStack(targetSlot, potSlot, world);
						}

					}

					return true;
				}
				return base.OnBlockInteractStart(world, byPlayer, blockSel);
			}


			if (stack != null && TryConstruct(world, blockSel.Position, stack.Collectible, byPlayer))
			{
				if (byPlayer != null && byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
				{
					byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
				}
				return true;
			}


			return false;
		}
	}
}
