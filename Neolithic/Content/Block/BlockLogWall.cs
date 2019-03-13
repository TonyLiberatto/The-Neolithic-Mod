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
        public string[] types = new string[]
        {
            "wall",
            "corner",
            "jut",
        };

        public string[] verticals = new string[]
        {
            "up",
            "left",
            "down",
            "right",
        };

        public string[] directions = new string[]
        {
            "north",
            "south",
            "east",
            "west",
        };

        string wood;
        private static int i;
        private static int rotationindex;
        
        public static Dictionary<int, AssetLocation[]> VariantsDictionary = new Dictionary<int, AssetLocation[]>();

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            wood = LastCodePart(3);
            if (api.Side == EnumAppSide.Server)
            {
                VariantsDictionary.Add(Id, GenVariants());
            }
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            IItemSlot activeslot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (activeslot.Itemstack.Item.Tool is EnumTool.Hammer && world.Side is EnumAppSide.Server)
            {
                world.RegisterCallback((dt) =>
                {
                    world.PlaySoundAt(Sounds.Place, byPlayer);
                    Swap(world, byPlayer, blockSel);
                    world.SpawnCubeParticles(blockSel.Position, blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5), 2, 32);
                }, 50);
            }
            /*
            if (world.Side is EnumAppSide.Client)
            {
                ModelTransform tf = new ModelTransform();

                tf.EnsureDefaultValues();

                tf.Origin.Set(0f, 0f, 0f);

                tf.Rotation.Y += (float)Math.Sin(world.Rand.Next())*360;

                byPlayer.Entity.Controls.UsingHeldItemTransformAfter = tf;
            }
            */
            return true;
        }

        public void Swap(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockPos pos = blockSel.Position;
            AssetLocation[] variants;
            Block nextBlock;
            VariantsDictionary.TryGetValue(Id, out variants);

            if (byPlayer.Entity.Controls.Sneak)
            {
                rotationindex = rotationindex < directions.Length ? rotationindex + 1 : 0;
                AssetLocation nextAsset = CodeWithPart(directions[rotationindex], 5);
                nextBlock = api.World.BlockAccessor.GetBlock(nextAsset);
                if (nextBlock.Id == Id)
                {
                    rotationindex = rotationindex < directions.Length ? rotationindex + 1 : 0;
                }
            }
            else
            {
                i = i < variants.Length ? i + 1 : 0;
                nextBlock = world.BlockAccessor.GetBlock(variants[i]);
                if (nextBlock.Id == Id)
                {
                    i = i < variants.Length ? i + 1 : 0;
                }
            }
            world.BlockAccessor.SetBlock(nextBlock.BlockId, pos);
        }

        public AssetLocation[] GenVariants()
        {
            List<AssetLocation> variantslist = new List<AssetLocation>();
            foreach (string type in types)
            {
                foreach (string vertical in verticals)
                {
                    variantslist.Add(new AssetLocation("neolithicmod:logwall-" + type + "-" + wood + "-" + LastCodePart(2) + "-" + vertical + "-" + LastCodePart()));
                }
            }
            return variantslist.ToArray();
        }
    }
}
