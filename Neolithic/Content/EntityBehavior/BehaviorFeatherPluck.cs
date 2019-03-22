using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace TheNeolithicMod
{
    class BehaviorFeatherPluck : EntityBehavior
    {
        private bool notplucking = true;
        public BehaviorFeatherPluck(Entity entity) : base(entity)
        {
        }

        public override string PropertyName()
        {
            return "featherpluck";
        }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
        }

        public override void OnInteract(EntityAgent byEntity, IItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            if (entity.World.Side.IsServer() && notplucking)
            {
                notplucking = false;
                ItemStack feather = new ItemStack(entity.World.GetItem(new AssetLocation("game:feather")), 1);
                feather.StackSize = 1;

                entity.ReceiveDamage(new DamageSource(), (float)(byEntity.World.Rand.NextDouble() * 0.5));
                if (!byEntity.TryGiveItemStack(feather))
                {
                    entity.World.SpawnItemEntity(feather, entity.Pos.XYZ);
                }

                entity.World.RegisterCallback(dt =>
                {
                    notplucking = true;
                }, 2000);
            }
        }
    }
}
