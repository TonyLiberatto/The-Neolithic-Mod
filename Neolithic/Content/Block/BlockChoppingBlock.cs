using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.ServerMods.NoObf;

namespace TheNeolithicMod
{
    class BlockChoppingBlock : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            base.OnBlockInteractStart(world, byPlayer, blockSel);
            (world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityChoppingBlock)?.OnInteract(world, byPlayer, blockSel);
            return true;
        }
    }

    class BlockEntityChoppingBlock : BlockEntity
    {
        private bool action = true;

        public void OnInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot?.Itemstack?.Item?.Tool == EnumTool.Axe && action)
            {
                action = false;
                api.World.RegisterCallback(dt => action = true, 500);

                slot.Itemstack.Collectible.DamageItem(api.World, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot, 1);

                (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                (world as IServerWorldAccessor)?.PlaySoundAt(new AssetLocation("sounds/block/wood-tool"), blockSel.Position);
                (world as IServerWorldAccessor)?.SpawnCubeParticles(pos, pos.MidPoint(), 1, 32, 0.5f);
                MarkDirty();
            }
        }
    }
}
