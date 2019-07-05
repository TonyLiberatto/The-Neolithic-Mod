using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TheNeolithicMod
{
    class MachineNetwork : ModSystem
    {

    }

    class PowerDevice
    {
        public double StoredPower { get; set; } = 0;
        public double PowerRate { get => PowerAmount / PowerResistance; set => PowerResistance = value; }
        public double PowerResistance { get => PowerAmount / PowerRate; set => PowerResistance = value; }
        public double PowerAmount { get => PowerRate * PowerResistance; set => PowerAmount = value; }
        public double Power { get => PowerRate * PowerAmount; }
        public EnumPowerType PowerType = 0;
        public InventoryGeneric InputInventory { get; set; }
        public InventoryGeneric OutputInventory { get; set; }
    }

    class BlockEntityMachine : BlockEntityAnimatable
    {
        PowerDevice device;
        public string key { get => "PowerDevice: " + pos; }

        byte[] Serialized => SerializerUtil.Serialize(device);

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            Block block = api.World.BlockAccessor.GetBlock(pos);

            if (api.World.Side.IsServer())
            {
                ICoreServerAPI sapi = (ICoreServerAPI)api;
                if (sapi.WorldManager.SaveGame.GetData(key) != null)
                {
                    device = SerializerUtil.Deserialize<PowerDevice>(sapi.WorldManager.SaveGame.GetData(key));
                }
                else
                {
                    device = new PowerDevice();
                }
                if (device.InputInventory == null)
                {
                    device.InputInventory = new InventoryGeneric(block.Attributes["InputSlots"].AsInt(0), key + "input", api);
                    device.OutputInventory = new InventoryGeneric(block.Attributes["InputSlots"].AsInt(0), key + "output", api);
                }

                sapi.WorldManager.SaveGame.StoreData(key, SerializerUtil.Serialize(device));
            }
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
            if (world.Side.IsServer())
            {
                ICoreServerAPI sapi = (ICoreServerAPI)api;
                BlockEntityMachine machine = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityMachine;
                if (machine != null)
                {
                    sapi.WorldManager.SaveGame.StoreData(machine.key, null);
                }
            }
        }
    }

    public enum EnumPowerType
    {
        Mechanical,
        Electrical
    }
}
