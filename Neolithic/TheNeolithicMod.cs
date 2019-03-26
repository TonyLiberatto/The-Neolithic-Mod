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
            if (ClientSettings._lhbuqUjwz7r0BVEAqsVqK9fC7ho.Int["maxTextureAtlasSize"] < 4096)
            {
                ClientSettings._lhbuqUjwz7r0BVEAqsVqK9fC7ho.Int["maxTextureAtlasSize"] = 4096;
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

            api.RegisterBlockClass("BlockPlaceOnDrop", typeof(BlockPlaceOnDropNew));
            api.RegisterBlockClass("BlockGiantReeds", typeof(BlockGiantReeds));
            api.RegisterBlockClass("BlockMortarAndPestle", typeof(BlockMortarAndPestle));
            api.RegisterBlockClass("BlockBowl", typeof(BlockBowlNew));
            api.RegisterBlockClass("BlockLogWall", typeof(BlockLogWall));
            api.RegisterBlockClass("BlockCheeseCloth", typeof(BlockCheeseCloth));
            api.RegisterBlockClass("BlockCookedContainerFix", typeof(CookedContainerFix));

            api.RegisterItemClass("ItemSickle", typeof(ItemSickle));
            api.RegisterItemClass("ItemGiantReedsRoot", typeof(ItemGiantReedsRoot));
            api.RegisterItemClass("ItemAdze", typeof(ItemAdze));
            api.RegisterItemClass("ItemSwapBlocks", typeof(ItemSwapBlocks));
            api.RegisterItemClass("ItemSlaughteringAxe", typeof(ItemSlaughteringAxe));

            api.RegisterBlockEntityClass("GenericPOI", typeof(POIEntity));
            api.RegisterBlockEntityClass("NeolithicTransient", typeof(NeolithicTransient));
            api.RegisterBlockEntityClass("BEScary", typeof(BEScary));
            api.RegisterBlockEntityClass("FixedBESapling", typeof(FixedBESapling));
            api.RegisterBlockEntityClass("BEMortarAndPestle", typeof(BEMortarAndPestle));
            api.RegisterBlockEntityClass("BlockEntityChimney", typeof(BlockEntityChimney));
            api.RegisterBlockEntityClass("BucketB", typeof(BEBucketOverride));
            api.RegisterBlockEntityClass("CookedContainerFix", typeof(CookedContainerFixBE));

            AiTaskRegistry.Register("fleepoi", typeof(AiTaskFleePOI));
            AiTaskRegistry.Register("sleep", typeof(AiTaskSleep));
            AiTaskRegistry.Register("neolithicseekfoodandeat", typeof(FixedAiTaskSeekFoodAndEat));

            api.RegisterEntityBehaviorClass("milkable", typeof(BehaviorMilkable));
            api.RegisterEntityBehaviorClass("slaughterable", typeof(BehaviorSlaughterable));
            api.RegisterEntityBehaviorClass("featherpluck", typeof(BehaviorFeatherPluck));
        }
    }
}