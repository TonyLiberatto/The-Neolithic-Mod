using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TheNeolithicMod
{
    class UnderSeaWaterEffects : ModSystem
    {
        List<WaterProps> AllowedLiquids = new List<WaterProps>();
        public Vec4f activeColor { get; set; } = new Vec4f();

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            AllowedLiquids = new List<WaterProps>()
            {
                //new WaterProps(new Vec4f(0, 0.5f, 0.5f, 0.6f), "water", "environment/underwater"),
                new WaterProps(new Vec4f(0, 0.2f, 0.5f, 0.6f), "seawater", "environment/underwater"),
            };

            api.Event.RegisterGameTickListener(dt => 
            {
                GetPlayerWaterDepth(api, out WaterProps prop, out float num);
                if (num > 0)
                {
                    activeColor.X = prop.Color.X;
                    activeColor.Y = prop.Color.Y;
                    activeColor.Z = prop.Color.Z;
                    activeColor.W = GameMath.Max(prop.Color.W, GameMath.Min(activeColor.W * (num*0.5f), 0.8f));
                }
                else
                {
                    activeColor.W = 0;
                }
            }, 7);
        }

        public void GetPlayerWaterDepth(ICoreClientAPI api, out WaterProps prop, out float num)
        {
            Vec3d CameraPos = api.World.Player?.Entity?.CameraPos;
            num = 0;

            if (CameraPos != null)
            {
                BlockPos pos = CameraPos.AddCopy(0,0.8,0).AsBlockPos;

                foreach (var val in AllowedLiquids)
                {
                    if (pos.GetBlock(api).LiquidCode == val.LiquidCode)
                    {
                        while (pos.Up(1).GetBlock(api).LiquidCode == val.LiquidCode && num < 256)
                        {
                            num++;
                        }
                        prop = val.Copy();
                        return;
                    }
                }
            }
            prop = new WaterProps(new Vec4f());
        }
    }

    class WaterProps
    {
        public WaterProps(Vec4f color, string liquidCode = "", string sound = "environment/underwater")
        {
            LiquidCode = liquidCode;
            Sound = sound;
            Color = color;
        }

        public WaterProps Copy()
        {
            WaterProps copy = new WaterProps(Color.Clone(), LiquidCode, Sound);
            return copy;
        }

        public string LiquidCode { get; set; } = "water";
        public string Sound { get; set; } = "environment/underwater";
        public Vec4f Color { get; set; } = new Vec4f(0, 0.5f, 0.5f, 0.6f);
    }
}
