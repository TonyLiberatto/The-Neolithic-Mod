using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TheNeolithicMod
{
    class MachineNetwork : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass("BlockMachine", typeof(BlockMachine));
            api.RegisterBlockEntityClass("Machine", typeof(BlockEntityMachine));
        }
    }

    class PowerDevice
    {
        public double StoredPower { get; set; } = 0;
        public EnumPowerType PowerType = EnumPowerType.Mechanical;
        public WorkItem ContainedWorkItem { get; set; } = null;
    }

    class WorkItem
    {
        public List<ItemStack> Required { get; set; } = new List<ItemStack>();
        public List<ItemStack> Output { get; set; } = new List<ItemStack>();
        public int WorkRequired { get; set; } = 256;
        public int WorkLeft { get; set; } = 256;
    }

    class BlockEntityMachine : BlockEntityAnimatable
    {
        PowerDevice device;
        Block block;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            block = pos.GetBlock(api);
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
        }

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            if (tree.GetString("powerdevice") != null)
            {
                device = JsonConvert.DeserializeObject<PowerDevice>(tree.GetString("powerdevice"));
            }
            else device = new PowerDevice();
            base.FromTreeAtributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            if (device != null)
            {
                tree.SetString("powerdevice", JsonConvert.SerializeObject(device));
            }
            base.ToTreeAttributes(tree);
        }
    }

    class BlockMachine : Block
    {
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
        {
            base.OnBlockRemoved(world, pos);
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            return base.GetPlacedBlockInfo(world, pos, forPlayer);
        }
    }

    public enum EnumPowerType
    {
        Mechanical,
        Electrical
    }
}
