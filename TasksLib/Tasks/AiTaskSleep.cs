using System;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BehaviorsLib
{
    public class AiTaskSleep : AiTaskBase
    {
        public AiTaskSleep(EntityAgent entity) : base(entity)
        {
        }

        public bool isNocturnal = true;
        public double WakeRNG = 0.0d;
        public bool doOnce = true;

        public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
        {
            if (taskConfig["isnocturnal"] != null)
            {
                isNocturnal = taskConfig["isnocturnal"].AsBool(true);
            }

            base.LoadConfig(taskConfig, aiConfig);
        }

        public override bool ShouldExecute()
        {
            if (doOnce)
            {
                WakeRNG = entity.World.Rand.NextDouble() / 5.00d;
                //entity.World.Logger.Notification("Entity at position " + entity.ServerPos + " is set to a WakeRNG of " + WakeRNG);
                doOnce = false;
            }
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
