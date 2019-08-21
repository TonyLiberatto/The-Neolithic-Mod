using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.NoObf;

namespace TheNeolithicMod
{
    class BlockChoppingBlock : Block
    {
        public ChoppingProp[] props;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            props = Attributes["choppingprops"].AsObject<ChoppingProp[]>();
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            base.OnBlockInteractStart(world, byPlayer, blockSel);
            (blockSel.BlockEntity(world) as BlockEntityChoppingBlock)?.OnInteract(world, byPlayer, blockSel);
            return true;
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            StringBuilder builder = new StringBuilder(base.GetPlacedBlockInfo(world, pos, forPlayer));
            BlockEntityChoppingBlock choppingBlock = (pos.BlockEntity(world) as BlockEntityChoppingBlock);
            builder = choppingBlock?.inventory?[0]?.Itemstack != null ? builder.AppendLine().AppendLine(choppingBlock.inventory[0].StackSize + "x " + Lang.Get(choppingBlock.inventory[0].Itemstack.Collectible.Code.ToString())) : builder;
            return builder.ToString();
        }
    }

    class BlockEntityChoppingBlock : BlockEntityContainer
    {
        private bool action = true;
        internal InventoryGeneric inventory;
        private ChoppingProp[] props;
        BlockEntityAnimationUtil util;

        public override InventoryBase Inventory { get => inventory; }
        public override string InventoryClassName { get => "choppingblock"; }
        string[] anims = new string[] { "idle", "chop", "chopidle" };

        public BlockEntityChoppingBlock()
        {
            inventory = new InventoryGeneric(1, null, null, (id, self) =>
            {
                return new ItemSlot(self);
            });
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            props = (pos.GetBlock(api) as BlockChoppingBlock).props;
            util = new BlockEntityAnimationUtil(api, this);
            if (api.Side.IsClient())
            {
                util.InitializeAnimators(new Vec3f(pos.GetBlock(api).Shape.rotateX, pos.GetBlock(api).Shape.rotateY, pos.GetBlock(api).Shape.rotateZ), anims);
                pos.GetBlock(api).DrawType = EnumDrawType.Empty;

                RegisterGameTickListener(dt =>
                {
                    StopAllAnims();
                    if (!action)
                    {
                        
                        util.StartAnimation(new AnimationMetaData() { Code = "chop" });
                    }
                    else
                    {
                        if (inventory[0].Itemstack?.StackSize > 0)
                        {
                            util.StartAnimation(new AnimationMetaData() { Code = "chopidle" });
                        }
                        else
                        {
                            util.StartAnimation(new AnimationMetaData() { Code = "idle" });
                        }
                    }
                }, 30);
            }
        }

        public void OnInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer?.InventoryManager.ActiveHotbarSlot;
            if (slot?.Itemstack?.Item?.Tool == EnumTool.Axe && action)
            {
                foreach (var val in props)
                {
                    if (inventory?[0]?.Itemstack?.Collectible?.WildCardMatch(val.input.Code) != null && inventory?[0]?.StackSize >= val.input.StackSize)
                    {
                        action = false;
                        inventory[0].TakeOut(val.input.StackSize);
                        api.World.RegisterCallback(dt => action = true, 500);

                        world.SpawnItemEntity(val.output, pos.MidPoint());

                        slot.Itemstack.Collectible.DamageItem(api.World, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot, 1);

                        (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                        (world as IServerWorldAccessor)?.PlaySoundAt(new AssetLocation("sounds/block/wood-tool"), blockSel.Position);
                        (world as IServerWorldAccessor)?.SpawnCubeParticles(pos, pos.MidPoint(), 1, 32, 0.5f);
                        MarkDirty();
                        break;
                    }
                }
            }
            else if (!byPlayer.Entity.Controls.Sneak)
            {
                foreach (var val in props)
                {
                    if (slot?.Itemstack?.Collectible?.WildCardMatch(val.input.Code) != null)
                    {
                        if (inventory?[0] != null)
                        {
                            slot.TryPutInto(world, inventory[0]);
                        }
                    }
                }
            }
        }

        public void StopAllAnims()
        {
            foreach (var val in anims)
            {
                util.StopAnimation(val);
            }
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            StopAllAnims();
        }
    }

    class ChoppingProp
    {
        public JsonItemStack input { get; set; }
        public JsonItemStack[] output { get; set; }
    }
}