using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace BehaviorsLib
{
    public class BehaviorsLib : ModSystem
	{
        public override void Start(ICoreAPI api)
		{

            api.RegisterEntityBehaviorClass("AiTaskSleep", typeof(AiTaskSleep));
            //AiTaskManager.RegisterTaskType("seekastar", typeof(AiTaskBaseAStar));
            api.RegisterEntityBehaviorClass("fixedseekfoodandeat", typeof(FixedAiTaskSeekFoodAndEat));
            //AiTaskManager.RegisterTaskType("fleepoi", typeof(AiTaskFleePOI));
          //  api.RegisterEntityBehaviorClass("injured", typeof(AiTaskInjured));
           // api.RegisterEntityBehaviorClass("fixedmultiply", typeof(FixedEntityBehaviorMultiply));
        }
    }
}