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

    class PowerNetwork
    {
        public EnumPowerType PowerType { get; set; } = EnumPowerType.Mechanical;
        public List<PowerDevice> Connected { get; set; } = new List<PowerDevice>();

        public PowerNetwork(EnumPowerType powerType, List<PowerDevice> connected)
        {
            PowerType = powerType;
            Connected = connected;
        }
    }

    class PowerDevice
    {
        public double StoredPower { get; set; } = 0;
        public double PowerDelta { get; set; } = 0;
        public double StorageCap { get; set; } = 256.0;
        public EnumPowerType PowerType { get; set; } = EnumPowerType.Mechanical;
        public WorkItem ContainedWorkItem { get; set; } = null;

        public PowerDevice(EnumPowerType powerType = EnumPowerType.Mechanical, WorkItem containedWorkItem = null, double storedPower = 0, double storagecap = 256.0, double powerdelta = 0)
        {
            StoredPower = storedPower;
            PowerType = powerType;
            ContainedWorkItem = containedWorkItem;
            StorageCap = storagecap;
            PowerDelta = powerdelta;
        }

        public PowerDevice Copy()
        {
            WorkItem tmp = ContainedWorkItem != null ? ContainedWorkItem : new WorkItem(new List<ItemStack>(), new List<ItemStack>(), 256, 256);
            PowerDevice copy = new PowerDevice(PowerType, tmp.Copy(), StoredPower, StorageCap, PowerDelta);
            return copy;
        }
        
    }

    class WorkItem
    {
        public List<ItemStack> Required { get; set; } = new List<ItemStack>();
        public List<ItemStack> Output { get; set; } = new List<ItemStack>();
        public int WorkRequired { get; set; } = 256;
        public int WorkLeft { get; set; } = 256;

        public WorkItem(List<ItemStack> required, List<ItemStack> output, int workRequired, int workLeft)
        {
            Required = required;
            Output = output;
            WorkRequired = workRequired;
            WorkLeft = workLeft;
        }

        public WorkItem Copy()
        {
            WorkItem copy = new WorkItem(Required, Output, WorkRequired, WorkLeft);
            return copy;
        }
    }

    class BlockEntityMachine : BlockEntityAnimatable
    {
        public PowerDevice device;
        Block block;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            block = pos.GetBlock(api);
            if (device == null)
            {
                device = new PowerDevice(
                    (EnumPowerType)block.Attributes["PowerType"].AsInt(), null, 
                    block.Attributes["PowerStored"].AsDouble(), 
                    block.Attributes["PowerCap"].AsDouble(256),
                    block.Attributes["PowerDelta"].AsDouble());
            }
            Update();
            RegisterGameTickListener(OnGameTick, 30);
        }

        public void Update()
        {
            device.PowerType = (EnumPowerType)block.Attributes["PowerType"].AsInt();
            device.StorageCap = block.Attributes["PowerCap"].AsDouble(256);
            device.PowerDelta = block.Attributes["PowerDelta"].AsDouble();
        }

        public void OnGameTick(float dt)
        {
            if (device.StoredPower >= 0)
            {
                if (device.StoredPower < device.StorageCap)
                {
                    device.StoredPower += device.PowerDelta;
                }
                else
                {
                    device.StoredPower = device.StorageCap;
                }
            }
            else
            {
                device.StoredPower = 0;
            }

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
            string a = "";
            BlockEntityMachine be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityMachine;
            if (be != null)
            {
                a += "StoredPower: " + be.device.StoredPower;
            }
            return base.GetPlacedBlockInfo(world, pos, forPlayer) + a;
        }
    }

    public enum EnumPowerType
    {
        Mechanical,
        Electrical
    }
}
