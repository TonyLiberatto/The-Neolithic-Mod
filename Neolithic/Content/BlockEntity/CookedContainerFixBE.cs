using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.GameContent;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;

namespace TheNeolithicMod
{
    class CookedContainerFixBE : BlockEntityCookedContainer
    {
        public CookedContainerFix ownBlock;
        private MeshData currentMesh;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            ownBlock = api.World.BlockAccessor.GetBlock(pos) as CookedContainerFix;
        }

        public new void ServePlayer(IPlayer player)
        {
            ItemStack itemStack = new ItemStack(new AssetLocation(("bowl-meal-colors-" + player.InventoryManager.ActiveHotbarSlot.Itemstack.Collectible.Variant["color"]).ToString()).GetBlock(), 1);
            (itemStack.Collectible as BlockMeal).SetContents(RecipeCode, itemStack, GetContentStacks(true));
            if (player.InventoryManager.ActiveHotbarSlot.StackSize == 1)
            {
                player.InventoryManager.ActiveHotbarSlot.Itemstack = itemStack;
            }
            else
            {
                player.InventoryManager.ActiveHotbarSlot.TakeOut(1);

                if (!player.InventoryManager.TryGiveItemstack(itemStack, true))
                {
                    api.World.SpawnItemEntity(itemStack, pos.ToVec3d().Add(0.5, 0.5, 0.5), null);
                }
                player.InventoryManager.ActiveHotbarSlot.MarkDirty();
            }
            --QuantityServings;
            if (QuantityServings <= 0)
            {
                api.World.BlockAccessor.SetBlock(api.World.GetBlock(ownBlock.CodeWithPath(ownBlock.CodeWithoutParts(1) + "-burned")).BlockId, pos);
            }
            else
            {
                if (api.Side == EnumAppSide.Client) currentMesh = GenMesh();
                MarkDirty(true);
            }
        }

        public new MeshData GenMesh()
        {
            if (ownBlock == null) return null;
            ItemStack[] contentStacks = GetContentStacks(true);
            if (contentStacks == null || contentStacks.Length == 0) return null;

            return (api as ICoreClientAPI).ModLoader.GetModSystem<MealMeshCache>().CreateMealMesh(ownBlock.Shape, FromRecipe, contentStacks, new Vec3f(0.0f, 5f / 32f, 0.0f));
        }
    }
}
