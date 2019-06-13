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
    class CookingContainerFix : BlockCookingContainer, IInFirepitRendererSupplier
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

    class CookedContainerFix : BlockCookedContainer, IInFirepitRendererSupplier
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

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (!hotbarSlot.Empty && hotbarSlot.Itemstack.Collectible.Attributes?["mealContainer"].AsBool() == true)
            {
                CookedContainerFixBE bec = world.BlockAccessor.GetBlockEntity(blockSel.Position) as CookedContainerFixBE;
                if (bec == null) return false;

                bec.ServePlayer(byPlayer);
                return true;
            }


            ItemStack stack = OnPickBlock(world, blockSel.Position);

            if (byPlayer.InventoryManager.TryGiveItemstack(stack, true))
            {
                world.BlockAccessor.SetBlock(0, blockSel.Position);
                world.PlaySoundAt(this.Sounds.Place, byPlayer, byPlayer);
                return true;
            }


            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        MealMeshCacheFix meshCache;
        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            if (meshCache == null) meshCache = capi.ModLoader.GetModSystem<MealMeshCacheFix>();

            CookingRecipe recipe = GetCookingRecipe(capi.World, itemstack);
            ItemStack[] contents = GetContents(capi.World, itemstack);

            float yoff = 2.5f; // itemstack.Attributes.GetInt("servings");

            MeshRef meshref = meshCache.GetOrCreateMealMeshRef(this.Shape, recipe, contents, this, new Vec3f(0, yoff / 16f, 0));
            if (meshref != null) renderinfo.ModelRef = meshref;
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

        public bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            mesher.AddMeshData(currentMesh);
            return true;
        }

        public new void ServePlayer(IPlayer player)
        {
            Block mealBlock = player.InventoryManager.ActiveHotbarSlot.Itemstack.Block;
            ItemStack mealstack = new ItemStack(api.World.GetBlock(new AssetLocation("bowl-meal-" + mealBlock.Variant["color"])));
            (mealstack.Collectible as BlockMeal).SetContents(RecipeCode, mealstack, GetContentStacks());

            if (player.InventoryManager.ActiveHotbarSlot.StackSize == 1)
            {
                player.InventoryManager.ActiveHotbarSlot.Itemstack = mealstack;
            }
            else
            {
                player.InventoryManager.ActiveHotbarSlot.TakeOut(1);
                if (!player.InventoryManager.TryGiveItemstack(mealstack, true))
                {
                    api.World.SpawnItemEntity(mealstack, pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }
                player.InventoryManager.ActiveHotbarSlot.MarkDirty();
            }

            QuantityServings--;

            if (QuantityServings <= 0)
            {
                Block block = api.World.GetBlock(ownBlock.CodeWithPath(ownBlock.CodeWithoutParts(1) + "-burned"));
                api.World.BlockAccessor.SetBlock(block.BlockId, pos);
                return;
            }

            if (api.Side == EnumAppSide.Client) currentMesh = GenMesh();

            MarkDirty(true);
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

    class BlockMealFix : BlockMeal
    {
        MealMeshCacheFix meshCache;
        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            if (meshCache == null) meshCache = capi.ModLoader.GetModSystem<MealMeshCacheFix>();

            MeshRef meshref = meshCache.GetOrCreateMealMeshRef(this.Shape, GetCookingRecipe(capi.World, itemstack), GetContents(capi.World, itemstack), this);
            if (meshref != null) renderinfo.ModelRef = meshref;
        }
    }

    class BlockEntityMealFix : BlockEntityMeal, IBlockShapeSupplier
    {
        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName => "meal";


        internal InventoryGeneric inventory;
        internal BlockMeal ownBlock;
        MeshData currentMesh;

        public BlockEntityMealFix()
        {
            inventory = new InventoryGeneric(4, null, null);
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            ownBlock = api.World.BlockAccessor.GetBlock(pos) as BlockMeal;

            if (api.Side == EnumAppSide.Client && currentMesh == null)
            {
                currentMesh = GenMesh();
                MarkDirty(true);
            }
        }

        internal MeshData GenMesh()
        {
            if (ownBlock == null) return null;
            ItemStack[] stacks = GetContentStacks();
            if (stacks == null || stacks.Length == 0) return null;

            ICoreClientAPI capi = api as ICoreClientAPI;
            return capi.ModLoader.GetModSystem<MealMeshCacheFix>().CreateMealMesh(ownBlock.Shape, FromRecipe, stacks, ownBlock);
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
            if (api.Side == EnumAppSide.Client)
            {
                currentMesh = GenMesh();
                MarkDirty(true);
            }
        }

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAtributes(tree, worldForResolving);
            if (api?.Side == EnumAppSide.Client && currentMesh == null)
            {
                currentMesh = GenMesh();
                MarkDirty(true);
            }
        }

        public bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            mesher.AddMeshData(currentMesh);
            return true;
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

        public MeshRef GetOrCreateMealMeshRef(CompositeShape containerShape, CookingRecipe forRecipe, ItemStack[] contentStacks, Block block, Vec3f foodTranslate = null)
        {
            Dictionary<int, MeshRef> meshrefs = null;

            object obj;
            if (capi.ObjectCache.TryGetValue("cookedMeshRefs", out obj))
            {
                meshrefs = obj as Dictionary<int, MeshRef>;
            }
            else
            {
                capi.ObjectCache["cookedMeshRefs"] = meshrefs = new Dictionary<int, MeshRef>();
            }

            if (contentStacks == null) return null;

            int mealhashcode = GetMealHashCode(containerShape, capi.World, contentStacks);

            MeshRef mealMeshRef = null;

            if (!meshrefs.TryGetValue(mealhashcode, out mealMeshRef))
            {
                meshrefs[mealhashcode] = mealMeshRef = capi.Render.UploadMesh(CreateMealMesh(containerShape, forRecipe, contentStacks, block, foodTranslate));
            }

            return mealMeshRef;
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

        private int GetMealHashCode(CompositeShape containerShape, IClientWorldAccessor world, ItemStack[] contentStacks)
        {
            string s = containerShape.Base.ToShortString();
            for (int i = 0; i < contentStacks.Length; i++)
            {
                s += contentStacks[i].Collectible.Code.ToShortString();
            }

            return s.GetHashCode();
        }

    }

    public class NeoPotInFirepitRenderer : IInFirepitRenderer
    {
        public double RenderOrder => 0.5;
        public int RenderRange => 20;

        ICoreClientAPI capi;
        ItemStack stack;
        MeshRef potRef;
        MeshRef lidRef;
        BlockPos pos;
        float temp;

        ILoadedSound cookingSound;

        bool isInOutputSlot;
        Matrixf ModelMat = new Matrixf();

        public NeoPotInFirepitRenderer(ICoreClientAPI capi, ItemStack stack, BlockPos pos, bool isInOutputSlot)
        {
            this.capi = capi;
            this.stack = stack;
            this.pos = pos;
            this.isInOutputSlot = isInOutputSlot;

            BlockCookedContainer potBlock = capi.World.GetBlock(stack.Collectible.CodeWithVariant("type", "cooked")) as BlockCookedContainer;

            if (isInOutputSlot)
            {
                MealMeshCacheFix meshcache = capi.ModLoader.GetModSystem<MealMeshCacheFix>();

                MeshData potMesh = meshcache.CreateMealMesh(potBlock.Shape, potBlock.GetCookingRecipe(capi.World, stack), potBlock.GetContents(capi.World, stack), potBlock, new Vec3f(0, 2.5f / 16f, 0));
                potRef = capi.Render.UploadMesh(potMesh);
            }
            else
            {
                MeshData potMesh;
                capi.Tesselator.TesselateShape(potBlock, capi.Assets.TryGet("shapes/block/clay/pot-opened-empty.json").ToObject<Shape>(), out potMesh);
                potRef = capi.Render.UploadMesh(potMesh);

                MeshData lidMesh;
                capi.Tesselator.TesselateShape(potBlock, capi.Assets.TryGet("shapes/block/clay/pot-part-lid.json").ToObject<Shape>(), out lidMesh);
                lidRef = capi.Render.UploadMesh(lidMesh);
            }
        }

        public void Dispose()
        {
            potRef?.Dispose();
            lidRef?.Dispose();

            cookingSound?.Stop();
            cookingSound?.Dispose();
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            IRenderAPI rpi = capi.Render;
            Vec3d camPos = capi.World.Player.Entity.CameraPos;

            rpi.GlDisableCullFace();
            rpi.GlToggleBlend(true);

            IStandardShaderProgram prog = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);
            capi.Render.BindTexture2d(capi.BlockTextureAtlas.AtlasTextureIds[0]);

            prog.DontWarpVertices = 0;
            prog.AddRenderFlags = 0;
            prog.ModelMatrix = ModelMat
                .Identity()
                .Translate(pos.X - camPos.X + 0.001f, pos.Y - camPos.Y, pos.Z - camPos.Z - 0.001f)
                .Translate(0f, 1 / 16f, 0f)
                .Values
            ;
            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;

            rpi.RenderMesh(potRef);

            if (!isInOutputSlot)
            {
                float origx = GameMath.Sin(capi.World.ElapsedMilliseconds / 300f) * 5 / 16f;
                float origz = GameMath.Cos(capi.World.ElapsedMilliseconds / 300f) * 5 / 16f;

                float cookIntensity = GameMath.Clamp((temp - 50) / 50, 0, 1);

                prog.ModelMatrix = ModelMat
                    .Identity()
                    .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
                    .Translate(0, 6.5f / 16f, 0)
                    .Translate(-origx, 0, -origz)
                    .RotateX(cookIntensity * GameMath.Sin(capi.World.ElapsedMilliseconds / 50f) / 60)
                    .RotateZ(cookIntensity * GameMath.Sin(capi.World.ElapsedMilliseconds / 50f) / 60)
                    .Translate(origx, 0, origz)
                    .Values
                ;
                prog.ViewMatrix = rpi.CameraMatrixOriginf;
                prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;


                rpi.RenderMesh(lidRef);
            }

            prog.Stop();
        }

        public void OnUpdate(float temperature)
        {
            temp = temperature;

            float soundIntensity = GameMath.Clamp((temp - 50) / 50, 0, 1);
            SetCookingSoundVolume(isInOutputSlot ? 0 : soundIntensity);
        }

        public void OnCookingComplete()
        {
            isInOutputSlot = true;
        }


        public void SetCookingSoundVolume(float volume)
        {
            if (volume > 0)
            {

                if (cookingSound == null)
                {
                    cookingSound = capi.World.LoadSound(new SoundParams()
                    {
                        Location = new AssetLocation("sounds/effect/cooking.ogg"),
                        ShouldLoop = true,
                        Position = pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                        DisposeOnFinish = false,
                        Volume = volume
                    });
                    cookingSound.Start();
                }
                else
                {
                    cookingSound.SetVolume(volume);
                }

            }
            else
            {
                if (cookingSound != null)
                {
                    cookingSound.Stop();
                    cookingSound.Dispose();
                    cookingSound = null;
                }

            }

        }

    }
}
