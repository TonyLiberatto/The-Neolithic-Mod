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
    class BlockCheeseCloth : Block
    {
        public override void OnHeldInteractStart(IItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            if (slot.Itemstack.Collectible.LastCodePart() != "none") return;

            Block selBlock = api.World.BlockAccessor.GetBlock(blockSel.Position);
            if (selBlock is BlockBucket)
            {
                BlockBucket bucket = selBlock as BlockBucket;
                if (bucket.GetContent(byEntity.World, blockSel.Position) != null)
                {
                    if (bucket.GetContent(byEntity.World, blockSel.Position).Item.Code.Path == "curdsportion")
                    {
                        handling = EnumHandHandling.PreventDefault;
                    }
                }
            }
        }

        public override bool OnHeldInteractStep(float secondsPassed, IItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
        {
            if (slot.Itemstack.Collectible.LastCodePart() != "none") return false;
            Block selBlock = api.World.BlockAccessor.GetBlock(blockSelection.Position);
            if (selBlock is BlockBucket)
            {
                BlockBucket bucket = selBlock as BlockBucket;
                return HandAnimations.Collect(api.World, byEntity, secondsPassed);
            }
            return false;
        }

        public override bool OnHeldInteractCancel(float secondsUsed, IItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            return false;
        }

        public override void OnHeldInteractStop(float secondsUsed, IItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            BlockPos pos = blockSel.Position;
            Block selBlock = api.World.BlockAccessor.GetBlock(pos);
            if (slot.Itemstack.Collectible.LastCodePart() != "none") return;

            if (selBlock is BlockBucket)
            {
                BlockBucket bucket = selBlock as BlockBucket;
                ItemStack contents = bucket.GetContent(byEntity.World, pos);
                WaterTightContainableProps contentProps = bucket.GetContentProps(byEntity.World, pos);
                if (contents != null)
                {
                    if (contents.Item.Code.Path == "curdsportion")
                    {
                        if (api.World.Side.IsServer())
                        {
                            ItemStack curdsandwhey = new ItemStack(CodeWithPart("curdsandwhey", 2).GetBlock(), 1);

                            bucket.TryTakeContent(api.World, pos, 1);

                            slot.TakeOut(1);
                            if (!byEntity.TryGiveItemStack(curdsandwhey))
                            {
                                api.World.SpawnItemEntity(curdsandwhey, pos.ToVec3d());
                            }

                            api.World.PlaySoundAt(contentProps.FillSpillSound, pos.X, pos.Y, pos.Z);
                        }
                    }
                }
            }
        }
    }
}
