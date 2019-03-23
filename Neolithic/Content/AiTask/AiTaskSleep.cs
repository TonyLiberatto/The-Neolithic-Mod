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

        public override void StartExecute()
        {
            base.StartExecute();
        }

        public override bool ContinueExecute(float dt)
        {
            if (isNocturnal && entity.World.Calendar.DayLightStrength > 0.50f + WakeRNG || !isNocturnal && entity.World.Calendar.DayLightStrength < 0.50f + WakeRNG) return true;
            return false;
        }

    }
}
