using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TheNeolithicMod
{
    public class FixedStairs : BlockStairs
    {
        Block block;

        public override void OnNeighourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            block = world.BlockAccessor.GetBlock(pos);
            Block nBlock = world.BlockAccessor.GetBlock(neibpos);
            if (!nBlock.WildCardMatch(new AssetLocation("stair*")) && !nBlock.WildCardMatch(new AssetLocation("air*"))) return;

            AssetLocation[] cardinal = GetCardinal(world, pos);

            if (block.WildCardMatch(new AssetLocation("*stair-side*")))
            {
                StairsCheck(world, pos, cardinal);
            }
            else if (block.WildCardMatch(new AssetLocation("*stair-corner*")))
            {
                CornersCheck(world, pos, cardinal);
            }
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);
            block = world.BlockAccessor.GetBlock(blockPos);
            AssetLocation[] cardinal = GetCardinal(world, blockPos);

            if (block.WildCardMatch(new AssetLocation("*stair-side*")))
            {
                StairsCheck(world, blockPos, cardinal);
            }
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
			return new ItemStack(world.BlockAccessor.GetBlock(new AssetLocation("stair-side-" + FirstCodePart(2) + "-" + FirstCodePart(3) + "-up-north")));
		}

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            return new ItemStack[] { new ItemStack(world.BlockAccessor.GetBlock(new AssetLocation("stair-side-" + FirstCodePart(2) + "-" + FirstCodePart(3) + "-up-north"))) };
        }

        public void StairsCheck(IWorldAccessor world, BlockPos pos, AssetLocation[] cardinal)
        {

            var bA = world.BlockAccessor;
            string type = FirstCodePart(2);
            string material = FirstCodePart(3);

            if (!cardinal.Any(val => bA.GetBlock(val).WildCardMatch(new AssetLocation("*stair-side*")))) return;
            string cardinalN = bA.GetBlock(cardinal[0]).LastCodePart();
            string cardinalS = bA.GetBlock(cardinal[1]).LastCodePart();
            string cardinalE = bA.GetBlock(cardinal[2]).LastCodePart();
            string cardinalW = bA.GetBlock(cardinal[3]).LastCodePart();

            string updown = world.BlockAccessor.GetBlock(pos).LastCodePart(1);
            string baseStringO = "stair-corner-" + type + "-" + material + "-" + "outside" + "-" + updown;
            string baseStringI = "stair-corner-" + type + "-" + material + "-" + "inside" + "-" + updown;

            if (cardinalW == "east")
            {
                if (cardinalN == "north")
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(baseStringO + "-northeast")).BlockId, pos);
                }
                else if (cardinalS == "south")
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(baseStringI + "-southeast")).BlockId, pos);
                }
            }
            else if (cardinalW == "west")
            {
                if (cardinalN == "south")
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(baseStringI + "-southwest")).BlockId, pos);
                }
                else if (cardinalS == "north")
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(baseStringO + "-northwest")).BlockId, pos);
                }
            }
            else if (cardinalE == "east")
            {
                if (cardinalN == "south")
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(baseStringO + "-southeast")).BlockId, pos);
                }
                else if (cardinalS == "north")
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(baseStringI + "-northeast")).BlockId, pos);
                }
            }
            else if (cardinalE == "west")
            {
                if (cardinalN == "north")
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(baseStringI + "-northwest")).BlockId, pos);
                }
                else if (cardinalS == "south")
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(baseStringO + "-southwest")).BlockId, pos);
                }
            }
        }

        public void CornersCheck(IWorldAccessor world, BlockPos pos, AssetLocation[] cardinal)
        {
            var bA = world.BlockAccessor;
            string type = FirstCodePart(2);
            string material = FirstCodePart(3);
            string updown = LastCodePart(1);
            Block north = bA.GetBlock(new AssetLocation("stair-side-" + type + "-" + material + "-" + updown + "-north"));
            Block south = bA.GetBlock(new AssetLocation("stair-side-" + type + "-" + material + "-" + updown + "-south"));
            Block east = bA.GetBlock(new AssetLocation("stair-side-" + type + "-" + material + "-" + updown + "-east"));
            Block west = bA.GetBlock(new AssetLocation("stair-side-" + type + "-" + material + "-" + updown + "-west"));
            string cardinalN = bA.GetBlock(cardinal[0]).LastCodePart();
            string cardinalS = bA.GetBlock(cardinal[1]).LastCodePart();
            string cardinalE = bA.GetBlock(cardinal[2]).LastCodePart();
            string cardinalW = bA.GetBlock(cardinal[3]).LastCodePart();


            if (LastCodePart() == "northeast")
            {
                if (cardinalN == "north")
                {
                    bA.SetBlock(north.BlockId, pos);
                }
                else if (cardinalE == "east")
                {
                    bA.SetBlock(east.BlockId, pos);
                }
                else if (cardinalW == "east")
                {
                    bA.SetBlock(east.BlockId, pos);
                }
                else if (cardinalS == "north")
                {
                    bA.SetBlock(north.BlockId, pos);
                }

            }
            else if (LastCodePart() == "southwest")
            {
                if (cardinalS == "south")
                {
                    bA.SetBlock(south.BlockId, pos);
                }
                else if (cardinalE == "west")
                {
                    bA.SetBlock(west.BlockId, pos);
                }
                else if (cardinalN == "south")
                {
                    bA.SetBlock(south.BlockId, pos);
                }
                else if (cardinalW == "west")
                {
                    bA.SetBlock(west.BlockId, pos);
                }
            }
            else if (LastCodePart() == "southeast")
            {
                if (cardinalN == "south")
                {
                    bA.SetBlock(south.BlockId, pos);
                }
                else if (cardinalE == "east")
                {
                    bA.SetBlock(east.BlockId, pos);
                }
                else if (cardinalS == "south")
                {
                    bA.SetBlock(south.BlockId, pos);
                }
                else if (cardinalW == "east")
                {
                    bA.SetBlock(east.BlockId, pos);
                }
            }
            else if (LastCodePart() == "northwest")
            {
                if (cardinalW == "west")
                {
                    bA.SetBlock(west.BlockId, pos);
                }
                else if (cardinalS == "north")
                {
                    bA.SetBlock(north.BlockId, pos);
                }
                else if (cardinalE == "west")
                {
                    bA.SetBlock(west.BlockId, pos);
                }
                else if (cardinalN == "north")
                {
                    bA.SetBlock(north.BlockId, pos);
                }
            }
        }

        public static AssetLocation[] GetCardinal(IWorldAccessor world, BlockPos pos)
        {
            var bA = world.BlockAccessor;
            AssetLocation[] cardinal = {
                bA.GetBlock(pos.NorthCopy()).Code,
                bA.GetBlock(pos.SouthCopy()).Code,
                bA.GetBlock(pos.EastCopy()).Code,
                bA.GetBlock(pos.WestCopy()).Code,
            };
            return cardinal;
        }
    }
}
