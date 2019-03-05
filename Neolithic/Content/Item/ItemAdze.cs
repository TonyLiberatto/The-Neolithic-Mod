using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace TheNeolithicMod
{
    public class ItemAdze : ItemChisel
    {
        public override void OnHeldInteractStart(IItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
            if (block.BlockMaterial != EnumBlockMaterial.Wood) return;
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, ref handling);
        }
    }
}