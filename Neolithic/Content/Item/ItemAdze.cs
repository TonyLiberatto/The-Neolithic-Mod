using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace TheNeolithicMod
{
    public class ItemAdze : ItemChisel
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            canMicroChisel = true;
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
            if (block.BlockMaterial != EnumBlockMaterial.Wood) return;
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        }

    }
    public class ItemChiselFix : ItemChisel
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            canMicroChisel = true;
        }
    }
}