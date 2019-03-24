using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TheNeolithicMod
{
    class BEBucketOverride : BlockEntityBucket
    {
        new ICoreAPI api;
        BlockBucket bucket;
        public double updateTime;
        long id;

        public override void Initialize(ICoreAPI api)
        {
            this.api = api;

            bucket = new BlockBucket();
            if (api.World.Side.IsServer())
            {
                if (updateTime == 0) updateTime = ResetTimer();
                id = api.World.RegisterGameTickListener(OnGameTick, 30);
            }
            base.Initialize(api);
        }

        public double ResetTimer()
        {
            return api.World.Calendar.TotalHours + 42;
        }

        public void OnGameTick(float dt)
        {
            if (updateTime < api.World.Calendar.TotalHours)
            {
                ItemStack contents = bucket.GetContent(api.World, pos);
                if (contents != null)
                {
                    if (contents.Item.FirstCodePart() == "milkportion")
                    {
                        ItemStack curds = new ItemStack(api.World.GetItem(new AssetLocation("game:curdsportion")), 1);
                        curds.StackSize = contents.StackSize;
                        bucket.SetContent(api.World, pos, curds);
                    }
                }
                updateTime = ResetTimer();
            }
        }

        public override void OnBlockUnloaded()
        {
            Unregister();
            base.OnBlockUnloaded();
        }

        public override void OnBlockRemoved()
        {
            Unregister();
            base.OnBlockRemoved();
        }

        public void Unregister()
        {
            if (id != 0)
            {
                api.World.UnregisterGameTickListener(id);
            }
        }
    }
}
