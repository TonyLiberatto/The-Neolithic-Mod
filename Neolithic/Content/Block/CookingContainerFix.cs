using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TheNeolithicMod
{
    class CookingContainerFix : BlockCookingContainer
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }

        public new IInFirepitRenderer GetRendererWhenInFirepit(ItemStack stack, BlockEntityFirepit firepit, bool forOutputSlot)
        {
            return new NeoPotInFirepitRenderer(api as ICoreClientAPI, stack, firepit.pos, forOutputSlot);
        }
    }

    class CookedContainerFix : BlockCookedContainer
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }

        internal int GetServings(IWorldAccessor world, ItemStack byItemStack)
        {
            return byItemStack.Attributes.GetInt("servings");
        }

        internal void SetServings(IWorldAccessor world, ItemStack byItemStack, int value)
        {
            byItemStack.Attributes.SetInt("servings", value);
        }

        public new IInFirepitRenderer GetRendererWhenInFirepit(ItemStack stack, BlockEntityFirepit firepit, bool forOutputSlot)
        {
            return new NeoPotInFirepitRenderer(api as ICoreClientAPI, stack, firepit.pos, forOutputSlot);
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            ItemStack stack = base.OnPickBlock(world, pos);

            CookedContainerFixBE bec = world.BlockAccessor.GetBlockEntity(pos) as CookedContainerFixBE;

            if (bec != null)
            {
                ItemStack[] contentStacks = bec.GetContentStacks();
                SetContents(bec.RecipeCode, bec.QuantityServings, stack, contentStacks);
                float temp = contentStacks.Length > 0 ? contentStacks[0].Collectible.GetTemperature(world, contentStacks[0]) : 0;
                SetTemperature(world, stack, temp, false);
            }

            return stack;
        }
    }

    class CookedContainerFixBE : BlockEntityCookedContainer, IBlockShapeSupplier
    {
        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName => "cookedcontainer";
        internal InventoryGeneric inventory;

        CookedContainerFix ownBlock;
        MeshData currentMesh;

        public CookedContainerFixBE()
        {
            inventory = new InventoryGeneric(4, null, null);
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            ownBlock = api.World.BlockAccessor.GetBlock(pos) as CookedContainerFix;

            if (api.Side.IsClient() && currentMesh == null)
            {
                currentMesh = GenMesh();
                MarkDirty(true);
            }
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            CookedContainerFix blockpot = byItemStack?.Block as CookedContainerFix;
            if (blockpot != null)
            {
                TreeAttribute tempTree = byItemStack.Attributes?["temperature"] as TreeAttribute;

                ItemStack[] stacks = blockpot.GetContents(api.World, byItemStack);
                for (int i = 0; i < stacks.Length; i++)
                {
                    ItemStack stack = stacks[i].Clone();
                    Inventory[i].Itemstack = stack;

                    // Clone temp attribute    
                    if (tempTree != null) stack.Attributes["temperature"] = tempTree.Clone();
                }

                RecipeCode = blockpot.GetRecipeCode(api.World, byItemStack);
                QuantityServings = blockpot.GetServings(api.World, byItemStack);
            }

            if (api.Side == EnumAppSide.Client)
            {
                currentMesh = GenMesh();
                MarkDirty(true);
            }
        }

        public new MeshData GenMesh()
        {
            if (ownBlock == null) return null;
            ItemStack[] stacks = GetContentStacks();
            if (stacks == null || stacks.Length == 0) return null;

            ICoreClientAPI capi = api as ICoreClientAPI;
            return capi.ModLoader.GetModSystem<MealMeshCacheFix>().CreateMealMesh(ownBlock.Shape, FromRecipe, stacks, ownBlock, new Vec3f(0, 2.5f / 16f, 0));
        }
    }

    class MealMeshCacheFix : MealMeshCache
    {
        ICoreClientAPI capi;

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            capi = api;
        }

        public MeshData CreateMealMesh(CompositeShape cShape, CookingRecipe forRecipe, ItemStack[] contentStacks, Block inBlock, Vec3f foodTranslate = null)
        {
            MealTextureSource source = new MealTextureSource(capi, inBlock);
            Shape shape = capi.Assets.TryGet("shapes/" + cShape.Base.Path + ".json").ToObject<Shape>();

            MeshData containerMesh;
            capi.Tesselator.TesselateShape("meal", shape, out containerMesh, source, new Vec3f(cShape.rotateX, cShape.rotateY, cShape.rotateZ));

            if (forRecipe != null)
            {
                MeshData foodMesh = GenFoodMixMesh(contentStacks, forRecipe, foodTranslate);
                containerMesh.AddMeshData(foodMesh);
            }

            return containerMesh;
        }

        public MeshData GenFoodMixMesh(ItemStack[] contentStacks, CookingRecipe recipe, Vec3f foodTranslate, Block inBlock)
        {
            MeshData mergedmesh = null;
            MealTextureSource texSource = new MealTextureSource(capi, inBlock);

            Shape shape = capi.Assets.TryGet("shapes/" + recipe.Shape.Base.Path + ".json").ToObject<Shape>();
            Dictionary<CookingRecipeIngredient, int> usedIngredQuantities = new Dictionary<CookingRecipeIngredient, int>();

            for (int i = 0; i < contentStacks.Length; i++)
            {
                texSource.ForStack = contentStacks[i];
                CookingRecipeIngredient ingred = recipe.GetIngrendientFor(
                    contentStacks[i],
                    usedIngredQuantities.Where(val => val.Key.MaxQuantity <= val.Value).Select(val => val.Key).ToArray()
                );

                if (ingred == null)
                {
                    ingred = recipe.GetIngrendientFor(contentStacks[i]);
                }
                else
                {
                    int cnt = 0;
                    usedIngredQuantities.TryGetValue(ingred, out cnt);
                    cnt++;
                    usedIngredQuantities[ingred] = cnt;
                }

                if (ingred == null) continue;


                MeshData meshpart;
                string[] selectiveElements = null;

                CookingRecipeStack recipestack = ingred.GetMatchingStack(contentStacks[i]);

                if (recipestack.ShapeElement != null) selectiveElements = new string[] { recipestack.ShapeElement };
                texSource.customTextureMapping = recipestack.TextureMapping;

                capi.Tesselator.TesselateShape(
                    "mealpart", shape, out meshpart, texSource,
                    new Vec3f(recipe.Shape.rotateX, recipe.Shape.rotateY, recipe.Shape.rotateZ), 0, 0, null, selectiveElements
                );

                if (mergedmesh == null) mergedmesh = meshpart;
                else mergedmesh.AddMeshData(meshpart);
            }

            if (foodTranslate != null) mergedmesh.Translate(foodTranslate);

            return mergedmesh;
        }

    }
}
