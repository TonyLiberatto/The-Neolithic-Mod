using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace TheNeolithicMod
{
    class CookingContainerFix : BlockCookingContainer
    {
        public override void DoSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, IItemSlot inputSlot, IItemSlot outputSlot)
        {
            ItemStack[] stacks = GetCookingStacks(cookingSlotsProvider);

            CookingRecipe recipe = GetMatchingCookingRecipe(world, stacks);

            Block block = world.GetBlock(CodeWithPath(CodeWithoutParts(1) + "-cooked"));
            ItemStack outputStack = new ItemStack(block);

            if (recipe != null)
            {
                int quantityServings = recipe.GetQuantityServings(stacks);
                for (int i = 0; i < stacks.Length; i++)
                {
                    stacks[i].StackSize /= quantityServings;
                }

                ((BlockCookedContainer)block).SetContents(recipe.Code, quantityServings, outputStack, stacks);

                outputStack.Collectible.SetTemperature(world, outputStack, GetIngredientsTemperature(world, stacks));
                outputSlot.Itemstack = outputStack;
                inputSlot.Itemstack = null;

                for (int i = 0; i < cookingSlotsProvider.Slots.Length; i++)
                {
                    cookingSlotsProvider.Slots[i].Itemstack = null;
                }
                return;
            }
        }
    }
}
