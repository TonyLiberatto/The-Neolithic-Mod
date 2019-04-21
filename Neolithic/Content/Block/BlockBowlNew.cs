using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace TheNeolithicMod
{
    class BlockBowlNew : Block
    {
        /*
        public string Contents()
        {
            string content = LastCodePart();
            content = content == "raw" || content == "burned" ? null : content;
            return content;
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handHandling)
        {
            if (blockSel == null)
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, ref handHandling);
                return;
            }
            IBlockAccessor bA = byEntity.World.BlockAccessor;
            BlockBucket bucket = bA.GetBlock(blockSel.Position) as BlockBucket;
            string contents = Contents();

            if (bucket != null)
            {
                ItemStack stack = bucket.GetContent(byEntity.World, blockSel.Position);

                if (contents == null && stack != null)
                {
                    string bucketcontentpath = stack?.Collectible.Code.Path;
                    string bucketcontents = bucketcontentpath.Contains("portion") ? bucketcontentpath.Replace("portion", "") : bucketcontentpath;

                    ReplaceContents(slot, byEntity, bucketcontents);
                    bucket.TryTakeContent(byEntity.World, blockSel.Position, 1);
                }
                else
                {
                    Item contentItem = byEntity.World.GetItem(new AssetLocation(contents + "portion"));
                    if (byEntity.World.GetItem(new AssetLocation(contents + "portion")) != null)
                    {
                        if (bucket.TryAddContent(byEntity.World, blockSel.Position, new ItemStack(byEntity.World.GetItem(new AssetLocation(contents + "portion"))), 1) > 0)
                        {
                            ReplaceContents(slot, byEntity, "burned");
                        }
                    }
                }

                handHandling = EnumHandHandling.PreventDefaultAction;
                return;
            }

            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, ref handHandling);
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel != null && (byEntity.World.BlockAccessor.GetBlock(blockSel.Position) as BlockBucket) != null)
            {
                return false;
            }

            return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);
        }

        public void ReplaceContents(ItemSlot slot, EntityAgent byEntity, string content)
        {
            Block bowl = byEntity.World.GetBlock(new AssetLocation(CodeWithoutParts(1) + "-" + content));
            ItemStack stack = new ItemStack(bowl);

            if (slot.Itemstack.StackSize <= 1)
            {
                slot.Itemstack = stack;
            }
            else
            {
                IPlayer player = (byEntity as EntityPlayer)?.Player;
                slot.TakeOut(1);
                if (!player.InventoryManager.TryGiveItemstack(stack, true))
                {
                    byEntity.World.SpawnItemEntity(stack, byEntity.LocalPos.XYZ);
                }
            }
            slot.MarkDirty();
        }
        */
    }
}
