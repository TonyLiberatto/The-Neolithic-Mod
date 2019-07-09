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
        public double MaxPower { get; set; } = 50;
        public double PowerRate { get; set; } = 0; //{ get => PowerAmount / PowerResistance; set => PowerResistance = value; }
        public double PowerResistance { get; set; } = 0; //{ get => PowerAmount / PowerRate; set => PowerResistance = value; }
        public double PowerAmount { get; set; } = 0; //{ get => PowerRate * PowerResistance; set => PowerAmount = value; }
        public double Power { get => PowerRate * PowerAmount; }
        public EnumPowerType PowerType = 0;
        public ItemStack[] InputInventory { get; set; }
        public ItemStack[] ProcessingInventory { get; set; }
        public ItemStack[] OutputInventory { get; set; }
    }

    class BlockEntityMachine : BlockEntityAnimatable
    {
        public PowerDevice Device { get; private set; }
        public string Key { get => "PowerDevice: " + pos; }
        InventoryGeneric input;
        InventoryGeneric processing;
        InventoryGeneric output;

        byte[] Serialized() {
            return SerializerUtil.Serialize(JsonConvert.SerializeObject(Device));
        }
        
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            Block block = api.World.BlockAccessor.GetBlock(pos);

            if (api.World.Side.IsServer())
            {
                ICoreServerAPI sapi = (ICoreServerAPI)api;
                if (sapi.WorldManager.SaveGame.GetData(Key) != null)
                {
                    Device = JsonConvert.DeserializeObject<PowerDevice>(SerializerUtil.Deserialize<string>(sapi.WorldManager.SaveGame.GetData(Key)));
                }
                else
                {
                    Device = new PowerDevice();
                }
                if (Device.InputInventory == null)
                {
                    UpdateInv(sapi, block);
                }

                sapi.WorldManager.SaveGame.StoreData(Key, Serialized());
            }
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
        }

        public void UpdateForAllPlayers(ICoreServerAPI sapi)
        {
            foreach (var val in api.World.AllPlayers)
            {
                ITreeAttribute attribs = val.Entity.WatchedAttributes.GetOrAddTreeAttribute("Machines");
                attribs.SetString(Key, JsonConvert.SerializeObject(Device));
                val.Entity.WatchedAttributes.MarkPathDirty("Machines");
            }
            sapi.WorldManager.SaveGame.StoreData(Key, Serialized());
        }

        public void UpdateInv(ICoreServerAPI sapi, Block block)
        {
            input = new InventoryGeneric(block.Attributes["InputSlots"].AsInt(1), null, null, null);
            processing = new InventoryGeneric(block.Attributes["ProcessingSlots"].AsInt(1), null, null, null);
            output = new InventoryGeneric(block.Attributes["OutputSlots"].AsInt(1), null, null, null);
            Device.InputInventory = ToStackList(input);
            Device.ProcessingInventory = ToStackList(processing);
            Device.OutputInventory = ToStackList(output);
            sapi.WorldManager.SaveGame.StoreData(Key, Serialized());
        }

        public ItemStack[] ToStackList(InventoryGeneric inv)
        {
            List<ItemStack> items = new List<ItemStack>();
            foreach (var val in inv)
            {
                items.Add(val.Itemstack);
            }
            return items.ToArray();
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
            if (world.Side.IsServer())
            {
                ICoreServerAPI sapi = (ICoreServerAPI)api;
                BlockEntityMachine machine = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityMachine;
                if (machine != null)
                {
                    sapi.WorldManager.SaveGame.StoreData(machine.Key, null);

                    foreach (var val in api.World.AllPlayers)
                    {
                        ITreeAttribute attribs = val.Entity.WatchedAttributes.GetOrAddTreeAttribute("Machines");
                        attribs.SetString(machine.Key, "");
                    }
                }
            }
            base.OnBlockRemoved(world, pos);
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            ITreeAttribute attribs = forPlayer.Entity.WatchedAttributes.GetOrAddTreeAttribute("Machines");
            BlockEntityMachine machine = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityMachine;
            if (machine == null) return base.GetPlacedBlockInfo(world, pos, forPlayer) + "Power: " + 0.00f;

            PowerDevice device = JsonConvert.DeserializeObject<PowerDevice>(attribs.GetString(machine.Key));

            float power = (float)Math.Round(device.StoredPower, 2);
            return base.GetPlacedBlockInfo(world, pos, forPlayer) + "Power: " + power;
        }
    }

    public enum EnumPowerType
    {
        Mechanical,
        Electrical
    }
}
