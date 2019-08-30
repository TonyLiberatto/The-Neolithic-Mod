using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.NoObf;

namespace TheNeolithicMod
{
    class BlockDryingStation : Block
    {
        public DryingProp[] props;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            props = Attributes["dryingprops"].AsObject<DryingProp[]>();
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            base.OnBlockInteractStart(world, byPlayer, blockSel);
            (blockSel.BlockEntity(world) as BlockEntityDryingStation)?.OnInteract(world, byPlayer, blockSel);
            return true;
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            StringBuilder builder = new StringBuilder(base.GetPlacedBlockInfo(world, pos, forPlayer));
            BlockEntityDryingStation craftingStation = (pos.BlockEntity(world) as BlockEntityDryingStation);
            builder = craftingStation?.inventory?[0]?.Itemstack != null ? builder.AppendLine().AppendLine(craftingStation.inventory[0].StackSize + "x " + Lang.Get(craftingStation.inventory[0].Itemstack.Collectible.Code.ToString())) : builder;
            return builder.ToString();
        }
    }

    class BlockEntityDryingStation : BlockEntityContainer, IBlockShapeSupplier
    {
        public Block block { get => api.World.BlockAccessor.GetBlock(pos); }

        internal InventoryGeneric inventory;
        public override InventoryBase Inventory { get => inventory; }
        public override string InventoryClassName { get => "dryingstation"; }
        public DryingProp[] props;
        public MeshData mesh;

        public BlockEntityDryingStation()
        {
            inventory = new InventoryGeneric(1, null, null, (id, self) =>
            {
                return new ItemSlot(self);
            });
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            props = (pos.GetBlock(api) as BlockDryingStation)?.props;

            RegisterGameTickListener(dt =>
            {
            if (api.Side.IsClient())
            {
                    foreach (var val in props)
                    {
                        if (inventory[0].Itemstack?.Collectible?.Code?.ToString() == val.Input.Code.ToString())
                        {
                            ICoreClientAPI capi = api as ICoreClientAPI;

                            float? translateY = (((float?)inventory[0].Itemstack?.StackSize / inventory[0].Itemstack?.Collectible?.MaxStackSize) * 0.35f) + 0.01f;
                            float y = translateY ?? 0;

                            capi.Tesselator.TesselateBlock(block, out mesh);
                            MeshData fillPlane = QuadMeshUtil.GetCustomQuad(0, 0, 0, 0.9f, 0.9f, 255, 255, 255, 255);
                            fillPlane.Rotate(new Vec3f(0, 0, 0), GameMath.DEG2RAD * -90, 0, 0).Translate(0.05f, y, 0.95f);
                            TextureAtlasPosition texPos = new TextureAtlasPosition();

                            texPos = val.TextureSource.Type == EnumItemClass.Block ? capi.BlockTextureAtlas.GetPosition(val.TextureSource.Code.GetBlock(api), "up")
                            : capi.ItemTextureAtlas.GetPosition(val.TextureSource.Code.GetItem(api));
                            if (val.TextureSource.Code.GetBlock(api)?.ShapeHasWaterTint != null) fillPlane.AddTintIndex(2);
                            fillPlane.SetUv(texPos);
                            mesh.AddMeshData(fillPlane);
                            MarkDirty(true);
                            break;
                        }
                    }
                    
                }
            }, 30);
        }

        public bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            mesher.AddMeshData(mesh);
            return true;
        }

        public void OnInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer?.InventoryManager?.ActiveHotbarSlot;
            if (slot != null)
            {
                if (slot.Itemstack?.Block is BlockBucket)
                {
                    BlockBucket bucket = (slot.Itemstack.Block as BlockBucket);
                    if (byPlayer.Entity.Controls.Sneak)
                    {
                        foreach (var val in props)
                        {
                            if (val.Input.Code.ToString() == bucket.GetContent(world, slot.Itemstack)?.Collectible?.Code?.ToString())
                            {
                                DummySlot dummy = new DummySlot(bucket.TryTakeContent(world, slot.Itemstack, 1));
                                dummy.TryPutInto(world, inventory[0]);
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (bucket.TryPutContent(world, slot.Itemstack, inventory[0].Itemstack, 1) > 0)
                        {
                            inventory[0].TakeOut(1);
                        }
                    }
                }
                else
                {
                    foreach (var val in props)
                    {

                    }
                }
            }
        }

    }
}
