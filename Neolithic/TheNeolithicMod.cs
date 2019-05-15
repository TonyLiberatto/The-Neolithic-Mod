using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory;
using Vintagestory.Client;

namespace TheNeolithicMod
{
    public class Neolithic : ModSystem
    {
        ICoreClientAPI capi;

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            api.Event.BlockTexturesLoaded += ReloadTextures;
        }

        public void ReloadTextures()
        {
            if (capi.Settings.Int["maxTextureAtlasSize"] < 4096)
            {
                capi.Settings.Int["maxTextureAtlasSize"] = 4096;
            }
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterBlockBehaviorClass("BlockCreateBehavior", typeof(BlockCreateBehavior));
            api.RegisterBlockBehaviorClass("BlockSwapBehavior", typeof(NewBlockSwapBehavior));
            api.RegisterBlockBehaviorClass("LampConnectorBehavior", typeof(LampConnectorBehavior));
            api.RegisterBlockBehaviorClass("LampPostBehavior", typeof(LampPostBehavior));
            api.RegisterBlockBehaviorClass("RotateNinety", typeof(RotateNinety));
            api.RegisterBlockBehaviorClass("ChimneyBehavior", typeof(ChimneyBehavior));
            //api.RegisterBlockClass("BlockPlaceOnDrop", typeof(BlockPlaceOnDropNew));
            api.RegisterBlockClass("BlockGiantReeds", typeof(BlockGiantReeds));
            api.RegisterBlockClass("BlockMortarAndPestle", typeof(BlockMortarAndPestle));
            //api.RegisterBlockClass("BlockBowl", typeof(BlockBowlNew));
            api.RegisterBlockClass("BlockLogWall", typeof(BlockLogWall));
            api.RegisterBlockClass("BlockCheeseCloth", typeof(BlockCheeseCloth));
            api.RegisterBlockClass("BlockCookedContainer", typeof(CookedContainerFix));
            api.RegisterBlockClass("BlockCookingContainer", typeof(CookingContainerFix));
            api.RegisterBlockClass("BlockNeolithicRoads", typeof(BlockNeolithicRoads));
            api.RegisterBlockClass("BlockLooseStones", typeof(BlockLooseStonesModified));
            api.RegisterBlockClass("BlockSmeltingContainer", typeof(BlockSmeltingContainerFix));
            api.RegisterBlockClass("BlockSmeltedContainer", typeof(BlockSmeltedContainerFix));
			api.RegisterBlockClass("BlockFirepit", typeof(BlockFirePitOverride));
			api.RegisterBlockClass("FixedStairs", typeof(FixedStairs));

			api.RegisterItemClass("ItemSickle", typeof(ItemSickle));
            api.RegisterItemClass("ItemGiantReedsRoot", typeof(ItemGiantReedsRoot));
            api.RegisterItemClass("ItemAdze", typeof(ItemAdze));
            api.RegisterItemClass("ItemChisel", typeof(ItemChiselFix));
            api.RegisterItemClass("ItemSwapBlocks", typeof(ItemSwapBlocks));
            api.RegisterItemClass("ItemSlaughteringAxe", typeof(ItemSlaughteringAxe));
            //api.RegisterItemClass("ItemKnife", typeof(ItemKnifeAdditions));

            api.RegisterBlockEntityClass("GenericPOI", typeof(POIEntity));
            api.RegisterBlockEntityClass("NeolithicTransient", typeof(NeolithicTransient));
            api.RegisterBlockEntityClass("BEScary", typeof(BEScary));
            api.RegisterBlockEntityClass("FixedBESapling", typeof(FixedBESapling));
            api.RegisterBlockEntityClass("BEMortarAndPestle", typeof(BEMortarAndPestle));
            api.RegisterBlockEntityClass("BlockEntityChimney", typeof(BlockEntityChimney));
            api.RegisterBlockEntityClass("BucketB", typeof(BEBucketOverride));
            api.RegisterBlockEntityClass("CookedContainerFix", typeof(CookedContainerFixBE));
            api.RegisterBlockEntityClass("NeolithicRoads", typeof(BENeolithicRoads));

			AiTaskRegistry.Register("fleepoi", typeof(AiTaskFleePOI));
            AiTaskRegistry.Register("sleep", typeof(AiTaskSleep));
            AiTaskRegistry.Register("neolithicseekfoodandeat", typeof(FixedAiTaskSeekFoodAndEat));

            api.RegisterEntityBehaviorClass("milkable", typeof(BehaviorMilkable));
            api.RegisterEntityBehaviorClass("slaughterable", typeof(BehaviorSlaughterable));
            api.RegisterEntityBehaviorClass("featherpluck", typeof(BehaviorFeatherPluck));
            api.RegisterEntityBehaviorClass("damagenotify", typeof(BehaviorNotifyHerdOfDamage));
        }
    }
}