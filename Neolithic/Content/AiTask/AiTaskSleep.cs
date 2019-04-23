using System;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace TheNeolithicMod
{
    public class AiTaskSleep : AiTaskBase
    {
        public AiTaskSleep(EntityAgent entity) : base(entity)
        {
        }

        public bool isNocturnal = true;
        public double WakeRNG = 0.0d;

        public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
        {
            if (taskConfig["isnocturnal"] != null)
            {
                isNocturnal = taskConfig["isnocturnal"].AsBool(true);
            }

            WakeRNG = entity.World.Rand.NextDouble() / 5.00d;
            base.LoadConfig(taskConfig, aiConfig);
        }

        public override bool ShouldExecute()
        {
            if (isNocturnal && entity.World.Calendar.DayLightStrength > 0.50f + WakeRNG || !isNocturnal && entity.World.Calendar.DayLightStrength < 0.50f + WakeRNG) return true;
            return false;
        }

        bool preventDuplicateAction = true;
        public override void StartExecute()
        {
            if (preventDuplicateAction == true)
            {
                preventDuplicateAction = false;
                entity.TeleportToDouble(entity.LocalPos.AsBlockPos.X + 0.5, entity.LocalPos.AsBlockPos.Y, entity.LocalPos.AsBlockPos.Z + 0.5);
                entity.World.RegisterCallback(dt => preventDuplicateAction = true, 1000);
            }

            
            base.StartExecute();
        }

        public override bool ContinueExecute(float dt)
        {
            if (isNocturnal && entity.World.Calendar.DayLightStrength > 0.50f + WakeRNG || !isNocturnal && entity.World.Calendar.DayLightStrength < 0.50f + WakeRNG) return true;
            return false;
        }

    }
}
