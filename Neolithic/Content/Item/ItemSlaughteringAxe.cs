using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace TheNeolithicMod
{
    class ItemSlaughteringAxe : Item
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }

        public override void OnHeldAttackStart(IItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            handling = EnumHandHandling.PreventDefault;
        }

        public override bool OnHeldAttackStep(float secondsPassed, IItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
        {
            if (entitySel != null)
            {
                Entity entity = entitySel.Entity;
                if (entity.HasBehavior("slaughterable"))
                {
                    return HandAnimations.Slaughter(byEntity.World, byEntity, secondsPassed);
                }
            }
            return false;
        }

        public override void OnHeldAttackStop(float secondsPassed, IItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
        {
            if (entitySel != null)
            {
                Entity entity = entitySel.Entity;
                if (entity.HasBehavior("slaughterable"))
                {
                    if (byEntity.World.Side.IsServer()) { entitySel.Entity.Die(); }
                }
            }
        }
    }
}
