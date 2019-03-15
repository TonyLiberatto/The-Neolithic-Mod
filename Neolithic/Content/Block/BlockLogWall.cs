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
        private static int typeindex;
        private static int rotationindex;
        private static int verticalindex;

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
            if (world.Side == EnumAppSide.Client)
            {
                ModelTransform tf = new ModelTransform();

                tf.EnsureDefaultValues();

                tf.Origin.Set(0f, 0f, 0f);

                tf.Rotation.X -= (float)Math.Sin(secondsUsed * 6) * 90;

                byPlayer.Entity.Controls.UsingHeldItemTransformAfter = tf;
                return tf.Rotation.X > -80;
            }
            return true;
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
            Block nextBlock;
            string[] types = FirstCodePart() == "rooframp" || FirstCodePart() == "roofstairs" ? rooftypes : walltypes;

            if (byPlayer.Entity.Controls.Sneak) 
            {
                rotationindex = rotationindex < directions.Length - 1 ? rotationindex + 1 : 0;
                nextAsset = new AssetLocation("neolithicmod:" + CodeWithoutParts(1) + "-" + directions[rotationindex]);
                nextBlock = world.BlockAccessor.GetBlock(nextAsset);
            }
            else if (byPlayer.Entity.Controls.Sprint && FirstCodePart() != "rooframp" && FirstCodePart() != "roofstairs") 
            {
                verticalindex = verticalindex < wallverticals.Length - 1 ? verticalindex + 1 : 0;
                nextAsset = CodeWithPart(wallverticals[verticalindex], 4);
                nextBlock = world.BlockAccessor.GetBlock(nextAsset);
            }
            else 
            {
                typeindex = typeindex < types.Length - 1 ? typeindex + 1 : 0;
                nextAsset = CodeWithPart(types[typeindex], 1);
                nextBlock = world.BlockAccessor.GetBlock(nextAsset);
            }
            world.BlockAccessor.SetBlock(nextBlock.BlockId, pos);
        }
    }
}
