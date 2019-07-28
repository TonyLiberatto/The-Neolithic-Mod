using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TheNeolithicMod
{
    class BlockMetalWedge : Block
    {
        ItemStack[] finishDrops;
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            EnumTool? tool = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Collectible.Tool;
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityMetalWedge && tool == EnumTool.Hammer)
            {
                BlockEntityMetalWedge wedge = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMetalWedge;
                if (!wedge.interacting)
                {
                    wedge.interacting = true;
                    if (wedge.animState < 1.0)
                    {
                        wedge.animState += 0.25f;
                        world.SpawnCubeParticles(blockSel.Position, blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5), 2, 16);
                        ((byPlayer.Entity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                    }
                    else if (world.Side.IsServer())
                    {
                        Block dBlock = api.World.BlockAccessor.GetBlock(blockSel.Position.DownCopy());
                        if (dBlock.GetBehavior<BlockBehaviorWedgable>() != null)
                        {
                            finishDrops = dBlock.GetBehavior<BlockBehaviorWedgable>().drops;
                        }

                        world.BlockAccessor.BreakBlock(blockSel.Position, null);
                        if (finishDrops != null)
                        {
                            world.BlockAccessor.SetBlock(0, blockSel.Position.DownCopy());
                            foreach (var val in finishDrops)
                            {
                                world.SpawnItemEntity(val, blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5));
                            }
                        }
                        else
                        {
                            world.BlockAccessor.BreakBlock(blockSel.Position.DownCopy(), null);
                        }
                    }
                    world.RegisterCallback(dt => wedge.interacting = false, 1000);
                }
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }

    class BlockBehaviorWedgable : BlockBehavior
    {
        public BlockBehaviorWedgable(Block block) : base(block)
        {
        }
        public ItemStack[] drops;

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            drops = properties["drops"].AsArray<ItemStack>();
        }
    }

    class BlockEntityMetalWedge : BlockEntity
    {
        public float animState { get; set; } = 0.0f;
        public bool interacting { get; set; } = false;
        public string prev { get; set; } = null;
        string[] pause = new string[] { "0", "1", "2", "3", "4" };
        string[] transition = new string[] { "0-1", "1-2", "2-3", "3-4" };

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            BlockEntityAnimationUtil util = new BlockEntityAnimationUtil(api, this);
            Block block = api.World.BlockAccessor.GetBlock(pos);
            if (api.World.Side.IsClient())
            {
                for (int i = 0; i < pause.Length; i++)
                {
                    
                    util.InitializeAnimator(pause[i], new Vec3f(0, block.Shape.rotateY, 0));
                }

                for (int i = 0; i < transition.Length; i++)
                {
                    util.InitializeAnimator(transition[i], new Vec3f(0, block.Shape.rotateY, 0));
                }

                if (prev != null)
                {
                    util.StartAnimation(new AnimationMetaData() { Code = prev });
                }
            }
            float lastState = animState;
            api.World.RegisterGameTickListener(dt => {
                if (animState != lastState)
                {
                    lastState = animState;
                    if (api.World.Side.IsClient())
                    {
                        if (prev != null)
                        {
                            util.StopAnimation(prev);
                        }
                        util.StartAnimation(new AnimationMetaData() { Code = transition[(int)Math.Round(animState * (transition.Length - 1))] });
                        api.World.PlaySoundAt(block.Sounds.Place, pos.X, pos.Y, pos.Z);
                        api.World.RegisterCallback(dtb => 
                        {
                            util.StopAnimation(transition[(int)Math.Round(animState * (transition.Length - 1))]);
                            util.StartAnimation(new AnimationMetaData() { Code = pause[(int)Math.Round(animState * (pause.Length - 1))] });
                        }, 100);
                    }
                    prev = pause[(int)Math.Round(animState * (pause.Length - 1))];
                }
            }, 30);
        }

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAtributes(tree, worldAccessForResolve);
            animState = tree.GetFloat("animState");
            prev = tree.GetString("prevAnim");
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("animState", animState);
            tree.SetString("prevAnim", pause[(int)Math.Round(animState * (pause.Length - 1))]);
        }
    }
}
