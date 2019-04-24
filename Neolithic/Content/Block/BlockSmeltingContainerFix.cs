using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace TheNeolithicMod
{
    class BlockSmeltingContainerFix : BlockSmeltingContainer
    {
        public override void DoSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot, ItemSlot outputSlot)
        {
            ItemStack[] stacks = GetIngredients(world, cookingSlotsProvider);

            AlloyRecipe alloy = GetMatchingAlloy(world, stacks);

            Block block = world.GetBlock(new AssetLocation(CodeWithoutParts(1) + "-smelted"));
            ItemStack outputStack = new ItemStack(block);

            if (alloy != null)
            {
                ItemStack smeltedStack = alloy.Output.ResolvedItemstack.Clone();
                int units = (int)(alloy.GetTotalOutputQuantity(stacks) * 100);

                ((BlockSmeltedContainerFix)block).SetContents(outputStack, smeltedStack, units);
                outputStack.Collectible.SetTemperature(world, outputStack, GetIngredientsTemperature(world, stacks));
                outputSlot.Itemstack = outputStack;
                inputSlot.Itemstack = null;

                for (int i = 0; i < cookingSlotsProvider.Slots.Length; i++)
                {
                    cookingSlotsProvider.Slots[i].Itemstack = null;
                }


                return;
            }


            MatchedSmeltableStack match = GetSingleSmeltableStack(stacks);

            if (match != null)
            {
                ((BlockSmeltedContainerFix)block).SetContents(outputStack, match.output, (int)(match.stackSize * 100));
                outputStack.Collectible.SetTemperature(world, outputStack, GetIngredientsTemperature(world, stacks));
                outputSlot.Itemstack = outputStack;
                inputSlot.Itemstack = null;

                for (int i = 0; i < cookingSlotsProvider.Slots.Length; i++)
                {
                    cookingSlotsProvider.Slots[i].Itemstack = null;
                }
            }

        }
    }

    class BlockSmeltedContainerFix : BlockSmeltedContainer
    {
        internal void SetContents(ItemStack stack, ItemStack output, int units)
        {
            stack.Attributes.SetItemstack("output", output);
            stack.Attributes.SetInt("units", units);
        }
    }
}
