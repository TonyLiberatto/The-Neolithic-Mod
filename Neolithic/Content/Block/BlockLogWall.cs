using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TheNeolithicMod
{
    class BlockLogWall : Block
    {
        public readonly string[] walltypes =
        {
            "wall",
            "corner",
            "jut",
        };

        public readonly string[] wallverticals =
        {
            "up",
            "right",
            "down",
            "left",
        };

        public readonly string[] rooftypes =
        {
            "corner",
            "cornerin",
            "cornertop",
            "slope",
            "slopewall",
            "top",
            "topwall",
            "topend",
        };

        public readonly string[] directions =
        {
            "north",
            "east",
            "south",
            "west",
        };

        private static uint dIndex;
        private static uint tIndex;
        private static uint vIndex;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            IItemSlot activeslot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (activeslot.Itemstack == null || activeslot.Itemstack.Item == null) return false;
            if (activeslot.Itemstack.Item.Tool == EnumTool.Hammer)
            {
                return true;
            }
            return false;
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            return HandAnimations.Hit(world, byPlayer, secondsUsed);
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            IItemSlot activeslot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (activeslot.Itemstack.Item.Tool == EnumTool.Hammer && world.Side == EnumAppSide.Server)
            {
                world.PlaySoundAt(Sounds.Place, byPlayer);
                Swap(world, byPlayer, blockSel);
                world.SpawnCubeParticles(blockSel.Position, blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5), 2, 32);
            }
        }

        public void Swap(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockPos pos = blockSel.Position;
            AssetLocation nextAsset;
            string[] types = FirstCodePart() == "rooframp" || FirstCodePart() == "roofstairs" ? rooftypes : walltypes;

            if (byPlayer.Entity.Controls.Sneak) 
            {
                nextAsset = new AssetLocation("neolithicmod:" + CodeWithoutParts(1) + "-" + directions.Next(ref dIndex));
            }
            else if (byPlayer.Entity.Controls.Sprint && FirstCodePart() == "logwall") 
            {
                nextAsset = CodeWithPart(wallverticals.Next(ref vIndex), 4);
            }
            else 
            {
                nextAsset = CodeWithPart(types.Next(ref tIndex), 1);
            }
            world.BlockAccessor.SetBlock(world.BlockAccessor.GetBlock(nextAsset).BlockId, pos);
        }
    }
}
