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

        string wood;
        private static int i;
        private static int rotationindex;
        private static int verticalindex;

        public static Dictionary<int, AssetLocation[]> VariantsDictionary = new Dictionary<int, AssetLocation[]>();

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            if (api.Side == EnumAppSide.Server)
            {
                if (VariantsDictionary.ContainsKey(Id)) return;
                if (FirstCodePart() == "logwall")
                {
                    wood = LastCodePart(3);
                    VariantsDictionary.Add(Id, GenLogwallVariants());
                }
                else if (FirstCodePart() == "rooframp" || FirstCodePart() == "roofstairs")
                {
                    wood = LastCodePart(2);
                    VariantsDictionary.Add(Id, GenRoofVariants());
                }
            }
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
            AssetLocation[] variants;
            AssetLocation nextAsset;
            Block nextBlock;
            VariantsDictionary.TryGetValue(Id, out variants);

            if (byPlayer.Entity.Controls.Sneak)
            {
                rotationindex = rotationindex < directions.Length - 1 ? rotationindex + 1 : 0;
                nextAsset = FirstCodePart() == "rooframp" || FirstCodePart() == "roofstairs" ? CodeWithPart(directions[rotationindex], 4) : CodeWithPart(directions[rotationindex], 5);

                nextBlock = api.World.BlockAccessor.GetBlock(nextAsset);
                if (nextBlock.Id == Id)
                {
                    rotationindex = rotationindex < directions.Length - 1 ? rotationindex + 1 : 0;
                    nextBlock = api.World.BlockAccessor.GetBlock(nextAsset);
                }
            }
            else if (byPlayer.Entity.Controls.Sprint && FirstCodePart() != "rooframp" && FirstCodePart() != "roofstairs")
            {
                verticalindex = verticalindex < wallverticals.Length - 1 ? rotationindex + 1 : 0;
                nextAsset = CodeWithPart(wallverticals[rotationindex], 4);

                nextBlock = api.World.BlockAccessor.GetBlock(nextAsset);
                if (nextBlock.Id == Id)
                {
                    rotationindex = rotationindex < wallverticals.Length - 1 ? rotationindex + 1 : 0;
                    nextBlock = api.World.BlockAccessor.GetBlock(nextAsset);
                }
            }
            else
            {
                i = i < variants.Length - 1 ? i + 1 : 0;
                nextBlock = world.BlockAccessor.GetBlock(variants[i]);
                if (nextBlock.Id == Id)
                {
                    i = i < variants.Length - 1 ? i + 1 : 0;
                    nextBlock = world.BlockAccessor.GetBlock(variants[i]);
                }
            }
            world.BlockAccessor.SetBlock(nextBlock.BlockId, pos);
        }

        public AssetLocation[] GenLogwallVariants()
        {
            List<AssetLocation> variantslist = new List<AssetLocation>();
            foreach (string type in walltypes)
            {
                variantslist.Add(new AssetLocation("neolithicmod:" + FirstCodePart() + "-" + type + "-" + wood + "-" + LastCodePart(2) + "-" + LastCodePart(1) + "-" + LastCodePart()));
            }
            return variantslist.ToArray();
        }

        public AssetLocation[] GenRoofVariants()
        {
            List<AssetLocation> variantslist = new List<AssetLocation>();
            foreach (string type in rooftypes)
            {
                variantslist.Add(new AssetLocation("neolithicmod:" + FirstCodePart() + "-" + type + "-" + wood + "-" + LastCodePart(1) + "-" + LastCodePart()));
            }
            return variantslist.ToArray();
        }
    }
}
