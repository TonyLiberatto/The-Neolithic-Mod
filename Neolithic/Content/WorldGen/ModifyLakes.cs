using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;
using Vintagestory.ServerMods.NoObf;

namespace TheNeolithicMod
{
    public class ModifyLakes : ModSystem
    {
        ICoreServerAPI api;
        public override double ExecuteOrder() => 0;
        public override bool ShouldLoad(EnumAppSide forSide) => forSide.IsServer();
        long[] ids = new long[2];

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.api = api;
            
            api.Event.ServerRunPhase(EnumServerRunPhase.LoadGamePre, ChangeLakeWater);
        }

        public void ChangeLakeWater()
        {
            NeolithicGlobalConfig config = api.Assets.Get("worldgen/global.json").ToObject<NeolithicGlobalConfig>();
            config.SetApi(api);
            ids[0] = api.Event.RegisterGameTickListener(dt => 
            {
                if (api.ModLoader.GetModSystem<GenLakes>().GlobalConfig != null)
                {
                    api.ModLoader.GetModSystem<GenLakes>().GlobalConfig.waterBlockCode = config.lakeWaterBlockCode;
                    api.ModLoader.GetModSystem<GenLakes>().GlobalConfig.waterBlockId = config.lakeWaterBlockId;
                    api.Event.UnregisterGameTickListener(ids[0]);
                }
            }, 30);
            ids[1] = api.Event.RegisterGameTickListener(dt =>
            {
                if (api.ModLoader.GetModSystem<GenRivulets>().GlobalConfig != null)
                {
                    api.ModLoader.GetModSystem<GenRivulets>().GlobalConfig.waterBlockCode = config.lakeWaterBlockCode;
                    api.ModLoader.GetModSystem<GenRivulets>().GlobalConfig.waterBlockId = config.lakeWaterBlockId;
                    api.Event.UnregisterGameTickListener(ids[1]);
                }
            }, 30);
        }
    }

    public class BlockSeaweedOverride : BlockSeaweed
    {
        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, LCGRandom worldGenRand)
        {
            bool gen = false;
            BlockPos pos1 = pos.DownCopy(1);
            if (blockAccessor.GetBlock(pos1).LiquidCode == "seawater")
            {
                blockAccessor.GetBlock(pos1).LiquidCode = "water";
                gen = base.TryPlaceBlockForWorldGen(blockAccessor, pos, onBlockFace, worldGenRand);
                blockAccessor.GetBlock(pos1).LiquidCode = "seawater";
            }
            return gen;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class NeolithicGlobalConfig
    {
        [JsonProperty]
        public AssetLocation waterBlockCode;

        [JsonProperty]
        public AssetLocation lakeWaterBlockCode;

        [JsonProperty]
        public AssetLocation lakeIceBlockCode;

        [JsonProperty]
        public AssetLocation glacierIceBlockCode;

        [JsonProperty]
        public AssetLocation lavaBlockCode;

        [JsonProperty]
        public AssetLocation basaltBlockCode;

        [JsonProperty]
        public AssetLocation mantleBlockCode;

        [JsonProperty]
        public AssetLocation defaultRockCode;

        private ICoreAPI api;

        public int waterBlockId { get => waterBlockCode.GetID(api); }
        public int lakeWaterBlockId { get => lakeWaterBlockCode.GetID(api); }
        public int lakeIceBlockId { get => lakeIceBlockCode.GetID(api); }
        public int glacierIceBlockId { get => glacierIceBlockCode.GetID(api); }
        public int lavaBlockId { get => lavaBlockCode.GetID(api); }
        public int basaltBlockId { get => basaltBlockCode.GetID(api); }
        public int mantleBlockId { get => mantleBlockCode.GetID(api); }
        public int defaultRockId { get => defaultRockCode.GetID(api); }

        public void SetApi(ICoreAPI api)
        {
            this.api = api;
        }
    }
}
