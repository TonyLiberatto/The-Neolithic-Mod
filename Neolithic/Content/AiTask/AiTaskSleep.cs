using System;
using System.Collections.Generic;
using TheNeolithicMod.Utility;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TheNeolithicMod
{
    public class AiTaskSleep : AiTaskBase
    {
        public AiTaskSleep(EntityAgent entity) : base(entity)
        {
            List<BlockPos> poslist = AreaMethods.CardinalOffsetList();
            offsets = poslist.ToArray();
        }

        public bool isNocturnal = true;
        BlockPos[] offsets;

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
            if (isNocturnal && entity.World.Calendar.DayLightStrength > 0.50f || !isNocturnal && entity.World.Calendar.DayLightStrength < 0.50f) return true;
            return false;
        }

        bool preventDuplicateAction = true;
        public override void StartExecute()
        {
            if (preventDuplicateAction == true)
            {
                preventDuplicateAction = false;
                BlockPos pos = entity.LocalPos.AsBlockPos;
                BlockPos dPos = new BlockPos(pos.X, pos.Y - 1, pos.Z);
                Block block = pos.GetBlock(entity.World);
                Block dBlock = dPos.GetBlock(entity.World);

                if (block.CollisionBoxes == null && dBlock.CollisionBoxes != null)
                {
                    entity.TeleportToDouble(pos.X + 0.5, pos.Y, pos.Z + 0.5);
                }
                else
                {
                    for (int i = 0; i < offsets.Length; i++)
                    {
                        pos = new BlockPos(pos.X + offsets[i].X, pos.Y + offsets[i].Y, pos.Z + offsets[i].Z);
                        dPos = new BlockPos(pos.X + offsets[i].X, pos.Y + offsets[i].Y - 1, pos.Z + offsets[i].Z);
                        block = pos.GetBlock(entity.World);
                        dBlock = dPos.GetBlock(entity.World);

                        if (block.CollisionBoxes == null && dBlock.CollisionBoxes != null)
                        {
                            entity.TeleportToDouble(pos.X + 0.5, pos.Y, pos.Z + 0.5);
                            break;
                        }
                    }
                }

                entity.World.RegisterCallback(dt => preventDuplicateAction = true, 10000);
            }

            base.StartExecute();
        }

        public override bool ContinueExecute(float dt)
        {
            if (isNocturnal && entity.World.Calendar.DayLightStrength > 0.50f || !isNocturnal && entity.World.Calendar.DayLightStrength < 0.50f) return true;
            return false;
        }

    }
}
