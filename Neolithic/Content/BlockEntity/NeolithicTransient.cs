using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TheNeolithicMod
{
    public class NeolithicContentConfig : ContentConfig
    {
    }

    public class NeolithicTransient : BlockEntityTransient, IAnimalFoodSource
    {
        public Vec3d Position => pos.ToVec3d().Add(0.5, 0.5, 0.5);
        public string Type => "food";
        NeolithicContentConfig[] nltConfig;
        BlockFacing facing;
        Block ownBlock;
        public string contentCode = "";
        double transitionAtTotalDays = -1;
        public static SimpleParticleProperties Flies;
        public bool flies = true;

        static NeolithicTransient()
        {

            Flies = new SimpleParticleProperties(
                1, 1,
                ColorUtil.ToRgba(100, 0, 0, 0),
                new Vec3d(), new Vec3d(),
                new Vec3f(-1f, -1f, -1f),
                new Vec3f(1f, 1f, 1f),
                1.0f,
                0.01f,
                0.25f, 0.30f,
                EnumParticleModel.Cube
            );
            Flies.glowLevel = 1;
            Flies.addPos.Set(1 / 16f, 0, 1 / 16f);
            Flies.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.05f);
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            RegisterGameTickListener(OnTick, 50);
            ownBlock = api.World.BlockAccessor.GetBlock(pos) as Block;
            facing = BlockFacing.FromCode(ownBlock.LastCodePart());
            if (nltConfig == null)
            {
                nltConfig = ownBlock.Attributes["contentConfig"].AsObject<NeolithicContentConfig[]>();
            }

            if (transitionAtTotalDays <= 0)
            {
                float hours = ownBlock.Attributes["inGameHours"].AsFloat(24);
                transitionAtTotalDays = api.World.Calendar.TotalDays + hours / 24;
            }

            if (api.Side == EnumAppSide.Server)
            {
                //api.World.Logger.Notification("AddedPOI: " + this);
                api.ModLoader.GetModSystem<POIRegistry>().AddPOI(this);
                RegisterGameTickListener(CheckTransition, 2000);
            }

        }

        private void OnTick(float dt)
        {
            //Block block = api.World.BlockAccessor.GetBlock(pos);
            bool flies = ownBlock.Attributes["flies"].AsBool(true);
            if (api.Side == EnumAppSide.Client && flies && api.World.Calendar.DayLightStrength > 0.5 )
            {
                double modx = api.World.Rand.NextDouble();
                double modz = api.World.Rand.NextDouble();
                double mody = api.World.Rand.NextDouble();

                int modc = Convert.ToInt32((modx + mody + modz / 3) * 25);

                Flies.minPos.Set(pos.X + modx, pos.Y + mody, pos.Z + modz);
                Flies.color = ColorUtil.ToRgba(100, 0, modc, modc);
                Flies.glowLevel = Convert.ToByte(api.World.Calendar.DayLightStrength * 50);

                api.World.SpawnParticles(Flies);
            }
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();

            if (api.Side == EnumAppSide.Server)
            {
                api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
            }
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();

            if (api.Side == EnumAppSide.Server)
            {
                api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
            }
        }

        public bool IsSuitableFor(Entity entity)
        {
            NeolithicContentConfig config = nltConfig.FirstOrDefault();
            if (config == null)
            {
                return false;
            }

            for (int i = 0; i < config.Foodfor.Length; i++)
            {
                if (RegistryObject.WildCardMatch(config.Foodfor[i], entity.Code)) return true;
            }
            return false;
        }

        public float ConsumeOnePortion()
        {
            Block block = api.World.BlockAccessor.GetBlock(pos);
            Block tblock;

            if (block.Attributes == null) return 1f;

            string fromCode = block.Attributes["convertFrom"].AsString();
            string toCode = block.Attributes["eatenTo"].AsString();
            if (fromCode == null || toCode == null) return 1f;

            if (fromCode.IndexOf(":") == -1) fromCode = block.Code.Domain + ":" + fromCode;
            if (toCode.IndexOf(":") == -1) toCode = block.Code.Domain + ":" + toCode;


            if (fromCode == null || !toCode.Contains("*"))
            {
                tblock = api.World.GetBlock(new AssetLocation(toCode));
                if (tblock == null) return 1f;

                api.World.BlockAccessor.SetBlock(tblock.BlockId, pos);
                return 1f;
            }

            AssetLocation blockCode = block.WildCardPop(new AssetLocation(fromCode), new AssetLocation(toCode));

            tblock = api.World.GetBlock(blockCode);
            if (tblock == null) return 1f;

            api.World.BlockAccessor.SetBlock(tblock.BlockId, pos);
            MarkDirty(true);
            return 1f;
        }
    }
}
