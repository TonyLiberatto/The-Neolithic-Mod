using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheNeolithicMod;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace CookingFix
{
    class CookingFix : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass("BlockSmeltedContainer", typeof(BlockSmeltedContainer));
            api.RegisterBlockClass("BlockSmeltingContainer", typeof(NeoBlockSmeltingContainer));
            api.RegisterBlockClass("BlockCookingContainer", typeof(CookingContainerFix));
            api.RegisterBlockClass("BlockCookedContainer", typeof(CookedContainerFix));
            api.RegisterBlockClass("BlockMeal", typeof(BlockMealFix));
            api.RegisterBlockEntityClass("CookedContainerFix", typeof(CookedContainerFixBE));
            api.RegisterBlockEntityClass("MealFix", typeof(BlockEntityMealFix));
        }
    }

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
    public class NeoBlockSmeltingContainer : BlockSmeltingContainer
    {
        public override void DoSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot, ItemSlot outputSlot)
        {
            ItemStack[] stacks = GetIngredients(world, cookingSlotsProvider);

            AlloyRecipe alloy = GetMatchingAlloy(world, stacks);

            Block block = world.GetBlock(CodeWithPath(FirstCodePart() + "-smelted"));
            ItemStack outputStack = new ItemStack(block);

            if (alloy != null)
            {
                ItemStack smeltedStack = alloy.Output.ResolvedItemstack.Clone();
                int units = (int)Math.Round(alloy.GetTotalOutputQuantity(stacks) * 100, 4);

                ((BlockSmeltedContainer)block).SetContents(outputStack, smeltedStack, units);
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
                ((BlockSmeltedContainer)block).SetContents(outputStack, match.output, (int)(match.stackSize * 100));
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

    public class BlockSmeltedContainer : Block
    {
        public static SimpleParticleProperties smokeHeld;
        public static SimpleParticleProperties smokePouring;
        public static SimpleParticleProperties bigMetalSparks;

        static BlockSmeltedContainer()
        {
            smokeHeld = new SimpleParticleProperties(
                1, 1,
                ColorUtil.ToRgba(50, 220, 220, 220),
                new Vec3d(),
                new Vec3d(),
                new Vec3f(-0.25f, 0.1f, -0.25f),
                new Vec3f(0.25f, 0.1f, 0.25f),
                1.5f,
                -0.075f,
                0.25f,
                0.25f,
                EnumParticleModel.Quad
            );
            smokeHeld.addPos.Set(0.1, 0.1, 0.1);

            smokePouring = new SimpleParticleProperties(
                1, 2,
                ColorUtil.ToRgba(50, 220, 220, 220),
                new Vec3d(),
                new Vec3d(),
                new Vec3f(-0.5f, 0f, -0.5f),
                new Vec3f(0.5f, 0f, 0.5f),
                1.5f,
                -0.1f,
                0.75f,
                0.75f,
                EnumParticleModel.Quad
            );
            smokePouring.addPos.Set(0.3, 0.3, 0.3);

            bigMetalSparks = new SimpleParticleProperties(
                1, 1,
                ColorUtil.ToRgba(255, 255, 233, 83),
                new Vec3d(), new Vec3d(),
                new Vec3f(-3f, 1f, -3f),
                new Vec3f(3f, 8f, 3f),
                0.5f,
                1f,
                0.25f, 0.25f
            );
            bigMetalSparks.glowLevel = 128;
        }


        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
                failureCode = "claimed";
                return false;
            }

            if (!byPlayer.Entity.Controls.Sneak || world.BlockAccessor.GetBlockEntity(blockSel.Position.DownCopy()) is ILiquidMetalSink)
            {
                failureCode = "__ignore__";
                return false;
            }

            if (!IsSuitablePosition(world, blockSel.Position, ref failureCode)) return false;

            if (world.BlockAccessor.GetBlock(blockSel.Position.DownCopy()).SideSolid[BlockFacing.UP.Index])
            {
                DoPlaceBlock(world, blockSel.Position, blockSel.Face, itemstack);

                BlockEntity be = world.BlockAccessor.GetBlockEntity(blockSel.Position);
                if (be is BlockEntitySmeltedContainer)
                {
                    BlockEntitySmeltedContainer belmc = (BlockEntitySmeltedContainer)be;
                    KeyValuePair<ItemStack, int> contents = GetContents(world, itemstack);
                    contents.Key.Collectible.SetTemperature(world, contents.Key, GetTemperature(world, itemstack));
                    belmc.contents = contents.Key.Clone();
                    belmc.units = contents.Value;
                }
                return true;
            }

            failureCode = "requiresolidground";

            return false;
        }


        public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
        {
            if (byEntity.World is IClientWorldAccessor && byEntity.World.Rand.NextDouble() < 0.02)
            {
                KeyValuePair<ItemStack, int> contents = GetContents(byEntity.World, slot.Itemstack);

                if (contents.Key != null && !HasSolidifed(slot.Itemstack, contents.Key, byEntity.World))
                {
                    Vec3d pos =
                        byEntity.Pos.XYZ.Add(0, byEntity.EyeHeight - 0.5f, 0)
                        .Ahead(0.3f, byEntity.Pos.Pitch, byEntity.Pos.Yaw)
                        .Ahead(0.47f, 0, byEntity.Pos.Yaw + GameMath.PIHALF)
                    ;

                    smokeHeld.minPos = pos.AddCopy(-0.05, -0.05, -0.05);
                    byEntity.World.SpawnParticles(smokeHeld);
                }
            }
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (blockSel == null) return;

            ILiquidMetalSink be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as ILiquidMetalSink;

            if (be != null)
            {
                handHandling = EnumHandHandling.PreventDefault;
            }

            if (be != null && be.CanReceiveAny)
            {
                KeyValuePair<ItemStack, int> contents = GetContents(byEntity.World, slot.Itemstack);

                if (contents.Key == null)
                {
                    string emptiedCode = Attributes["emptiedBlockCode"].AsString();

                    slot.Itemstack = new ItemStack(byEntity.World.GetBlock(NAssetLocation.Create(emptiedCode, Code.Domain)));
                    slot.MarkDirty();
                    handHandling = EnumHandHandling.PreventDefault;
                    return;
                }


                if (HasSolidifed(slot.Itemstack, contents.Key, byEntity.World))
                {
                    handHandling = EnumHandHandling.NotHandled;
                    return;
                }

                if (contents.Value <= 0) return;
                if (!be.CanReceive(contents.Key)) return;
                be.BeginFill(blockSel.HitPosition);

                byEntity.World.RegisterCallback((world, pos, dt) =>
                {
                    if (byEntity.Controls.HandUse == EnumHandInteract.HeldItemInteract)
                    {
                        IPlayer byPlayer = null;
                        if (byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

                        world.PlaySoundAt(new AssetLocation("sounds/hotmetal"), byEntity, byPlayer);
                    }
                }, blockSel.Position, 666);

                handHandling = EnumHandHandling.PreventDefault;
            }
        }


        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel == null) return false;

            ILiquidMetalSink be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as ILiquidMetalSink;
            if (be == null) return false;

            if (!be.CanReceiveAny) return false;
            KeyValuePair<ItemStack, int> contents = GetContents(byEntity.World, slot.Itemstack);
            if (!be.CanReceive(contents.Key)) return false;

            float speed = 1.5f;
            float temp = GetTemperature(byEntity.World, slot.Itemstack);

            if (byEntity.World is IClientWorldAccessor)
            {
                ModelTransform tf = new ModelTransform();
                tf.EnsureDefaultValues();

                tf.Origin.Set(0.5f, 0.2f, 0.5f);
                tf.Translation.Set(0, 0, -Math.Min(0.25f, speed * secondsUsed / 4));
                tf.Scale = 1f + Math.Min(0.25f, speed * secondsUsed / 4);
                tf.Rotation.X = Math.Max(-110, -secondsUsed * 90 * speed);
                byEntity.Controls.UsingHeldItemTransformBefore = tf;
            }

            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);


            if (secondsUsed > 1 / speed)
            {
                if ((int)(30 * secondsUsed) % 3 == 1)
                {
                    Vec3d pos =
                        byEntity.Pos.XYZ
                        .Ahead(0.1f, byEntity.Pos.Pitch, byEntity.Pos.Yaw)
                        .Ahead(1.0f, byEntity.Pos.Pitch, byEntity.Pos.Yaw - GameMath.PIHALF)
                    ;
                    pos.Y += byEntity.EyeHeight - 0.4f;

                    smokePouring.minPos = pos.AddCopy(-0.15, -0.15, -0.15);

                    Vec3d blockpos = blockSel.Position.ToVec3d().Add(0.5, 0.2, 0.5);

                    bigMetalSparks.minQuantity = Math.Max(0.2f, 1 - (secondsUsed - 1) / 4);

                    if ((int)(30 * secondsUsed) % 7 == 1)
                    {
                        bigMetalSparks.minPos = pos;
                        bigMetalSparks.minVelocity.Set(-2, -1, -2);
                        bigMetalSparks.addVelocity.Set(4, 1, 4);
                        byEntity.World.SpawnParticles(bigMetalSparks, byPlayer);

                        byEntity.World.SpawnParticles(smokePouring, byPlayer);
                    }

                    float y2 = 0;
                    Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
                    Cuboidf[] collboxs = block.GetCollisionBoxes(byEntity.World.BlockAccessor, blockSel.Position);
                    for (int i = 0; collboxs != null && i < collboxs.Length; i++)
                    {
                        y2 = Math.Max(y2, collboxs[i].Y2);
                    }

                    // Metal Spark on the mold
                    bigMetalSparks.minVelocity.Set(-2, 1, -2);
                    bigMetalSparks.addVelocity.Set(4, 5, 4);
                    bigMetalSparks.minPos = blockpos.AddCopy(-0.25, y2 - 2 / 16f, -0.25);
                    bigMetalSparks.addPos.Set(0.5, 0, 0.5);
                    bigMetalSparks.glowLevel = (byte)GameMath.Clamp((int)temp - 770, 48, 128);
                    byEntity.World.SpawnParticles(bigMetalSparks, byPlayer);

                    // Smoke on the mold
                    byEntity.World.SpawnParticles(
                        Math.Max(1, 12 - (secondsUsed - 1) * 6),
                        ColorUtil.ToRgba(50, 220, 220, 220),
                        blockpos.AddCopy(-0.5, y2 - 2 / 16f, -0.5),
                        blockpos.Add(0.5, y2 - 2 / 16f + 0.15, 0.5),
                        new Vec3f(-0.5f, 0f, -0.5f),
                        new Vec3f(0.5f, 0f, 0.5f),
                        1.5f,
                        -0.05f,
                        0.75f,
                        EnumParticleModel.Quad,
                        byPlayer
                    );

                }

                int transferedAmount = Math.Min(2, contents.Value);


                be.ReceiveLiquidMetal(contents.Key, ref transferedAmount, temp);

                int newAmount = Math.Max(0, contents.Value - (2 - transferedAmount));
                slot.Itemstack.Attributes.SetInt("units", newAmount);


                if (newAmount <= 0 && byEntity.World is IServerWorldAccessor)
                {
                    string emptiedCode = Attributes["emptiedBlockCode"].AsString();
                    slot.Itemstack = new ItemStack(byEntity.World.GetBlock(NAssetLocation.Create(emptiedCode, Code.Domain)));
                    slot.MarkDirty();
                    // Since we change the item stack we have to call this ourselves
                    OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
                    return false;
                }

                return true;
            }

            return true;
        }


        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            slot.MarkDirty();

            if (blockSel == null) return;

            ILiquidMetalSink be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as ILiquidMetalSink;
            be?.OnPourOver();
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            return GetDrops(world, pos, null)[0];
        }

        public override BlockDropItemStack[] GetDropsForHandbook(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
        {
            return GetHandbookDropsFromBreakDrops(world, pos, byPlayer);
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            ItemStack[] stacks = base.GetDrops(world, pos, byPlayer);

            BlockEntity be = world.BlockAccessor.GetBlockEntity(pos);
            if (be is BlockEntitySmeltedContainer)
            {
                BlockEntitySmeltedContainer belmc = (BlockEntitySmeltedContainer)be;
                ItemStack contents = belmc.contents.Clone();
                SetContents(stacks[0], contents, belmc.units);
                belmc.contents?.ResolveBlockOrItem(world);
                stacks[0].Collectible.SetTemperature(world, stacks[0], belmc.contents.Collectible.GetTemperature(world, contents));
            }

            return stacks;
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            BlockEntity be = world.BlockAccessor.GetBlockEntity(pos);
            if (be is BlockEntitySmeltedContainer)
            {
                BlockEntitySmeltedContainer belmc = (BlockEntitySmeltedContainer)be;
                belmc.contents.ResolveBlockOrItem(world);

                /*return
                    "Units: " + (int)(belmc.units) + "\n" +
                    "Metal: " + belmc.contents.GetName() + "\n" +
                    "Temperature: " + (int)belmc.Temperature + " °C\n"
                ;*/
                return Lang.Get("blocksmeltedcontainer-contents", (int)(belmc.units), belmc.contents.GetName(), (int)belmc.Temperature);
            }

            return base.GetPlacedBlockInfo(world, pos, forPlayer);
        }


        public override void GetHeldItemInfo(ItemStack stack, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            KeyValuePair<ItemStack, int> contents = GetContents(world, stack);

            if (contents.Key != null)
            {
                // cheap hax to get metal name
                string name = contents.Key.GetName();
                string metal = name.Substring(name.IndexOf("(") + 1, name.Length - 1 - name.IndexOf("("));

                dsc.Append(Lang.Get("item-unitdrop", (int)(contents.Value), metal));

                if (HasSolidifed(stack, contents.Key, world))
                {
                    dsc.Append(Lang.Get("metalwork-toocold"));
                }
            }



            base.GetHeldItemInfo(stack, dsc, world, withDebugInfo);
        }


        public bool HasSolidifed(ItemStack ownStack, ItemStack contentstack, IWorldAccessor world)
        {
            if (ownStack?.Collectible == null) return false;

            return ownStack.Collectible.GetTemperature(world, ownStack) < 0.9 * contentstack.Collectible.GetMeltingPoint(world, null, null);
        }

        internal void SetContents(ItemStack stack, ItemStack output, int units)
        {
            stack.Attributes.SetItemstack("output", output);
            stack.Attributes.SetInt("units", units);
        }

        KeyValuePair<ItemStack, int> GetContents(IWorldAccessor world, ItemStack stack)
        {
            ItemStack outstack = stack.Attributes.GetItemstack("output");
            if (outstack != null)
            {
                outstack.ResolveBlockOrItem(world);
            }
            return new KeyValuePair<ItemStack, int>(
                outstack,
                stack.Attributes.GetInt("units")
            );
        }
    }
}
