using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory;
using Vintagestory.Client;

[assembly: ModInfo("The Neolithic Mod",
    Description  = "This mod Requires New World Creation. Adds more Animals, Plants, blocks and tools",
    Website      = "https://github.com/TonyLiberatto/The-Neolithic-Mod",
    Authors      = new []{ "Tony Liberatto","Novocain","Balduranne","BunnyViking" },
    Contributors = new []{ "Tyron", "Milo", "Stroam", "Elwood", "copygirl", "MarcAFK", "Balduranne" })]

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
            api.RegisterBlockClass("BlockSmeltedContainer", typeof(NeoBlockSmeltedContainer));

            api.RegisterBlockClass("BlockCookingContainer", typeof(CookingContainerFix));
            api.RegisterBlockClass("BlockCookedContainer", typeof(CookedContainerFix));

            api.RegisterBlockClass("BlockCheeseCloth", typeof(BlockCheeseCloth));
            api.RegisterBlockClass("BlockNeolithicRoads", typeof(BlockNeolithicRoads));
            api.RegisterBlockClass("BlockLooseStones", typeof(BlockLooseStonesModified));
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
            api.RegisterBlockEntityClass("NeolithicRoads", typeof(BENeolithicRoads));
            api.RegisterBlockEntityClass("CookedContainerFix", typeof(CookedContainerFixBE));

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