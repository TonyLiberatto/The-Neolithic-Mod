using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace TheNeolithicMod
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class SwapMessage
    {
        public string SwapPairs;
    }

    class SwapSystem : ModSystem
    {
        IServerNetworkChannel sChannel;
        ICoreAPI api;

        public override void Start(ICoreAPI api)
        {
            this.api = api;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Network.RegisterChannel("swapPairs")
                .RegisterMessageType<SwapMessage>()
                .SetMessageHandler<SwapMessage>(a =>
                {
                    SwapPairs = JsonConvert.DeserializeObject<Dictionary<string, SwapBlocks>>(a.SwapPairs);
                });
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sChannel = api.Network.RegisterChannel("swapPairs").RegisterMessageType<SwapMessage>();
            api.Event.PlayerJoin += PlayerJoin;
        }

        private void PlayerJoin(IServerPlayer byPlayer)
        {
            sChannel.SendPacket(new SwapMessage() { SwapPairs = JsonConvert.SerializeObject(SwapPairs) }, byPlayer);
        }

        public Dictionary<string, SwapBlocks> SwapPairs { get; set; } = new Dictionary<string, SwapBlocks>();
    }

    class BlockSwapBehavior : BlockBehavior
    {
        ICoreAPI api;
        Vec3d particleOrigin = new Vec3d(0.5, 0.5, 0.5);
        bool requireSneak = false;
        bool disabled = false;
        bool playSound = true;
        int pRadius = 2;
        int pQuantity = 16;

        public BlockSwapBehavior(Block block) : base(block) { }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            this.api = api;
            PostOLInit();
        }

        public void PostOLInit()
        {
            SwapSystem swapSystem = api.ModLoader.GetModSystem<SwapSystem>();
            requireSneak = properties["requireSneak"].AsBool(requireSneak);
            particleOrigin = properties["particleOrigin"].Exists ? properties["particleOrigin"].AsObject<Vec3d>() : particleOrigin;
            pRadius = properties["particleRadius"].AsInt(pRadius);
            pQuantity = properties["particleQuantity"].AsInt(pQuantity);
            playSound = properties["playSound"].AsBool(true);

            if (properties["allowedVariants"].Exists)
            {
                string[] allowed = properties["allowedVariants"].AsArray<string>().WithDomain();

                disabled = true;
                if (allowed.Contains(block.Code.ToString()))
                {
                    disabled = false;
                }
                else return;
            }
            if (properties["swapBlocks"].Exists)
            {
                if (api.World.Side.IsServer())
                {
                    try
                    {
                        SwapBlocks[] swapBlocks = properties["swapBlocks"].AsObject<SwapBlocks[]>();
                        foreach (SwapBlocks val in swapBlocks)
                        {

                            if (val.Tool.Contains("*"))
                            {
                                for (int i = 0; i < api.World.Blocks.Count; i++)
                                {
                                    Block iBlock = api.World.Blocks[i];
                                    if (iBlock != null && iBlock.WildCardMatch(new AssetLocation(val.Tool)))
                                    {
                                        SwapBlocks tmp = val.Copy();
                                        tmp.Tool = iBlock.Code.ToString();
                                        if (!swapSystem.SwapPairs.ContainsKey(GetKey(tmp.Tool)))
                                        {
                                            swapSystem.SwapPairs.Add(GetKey(tmp.Tool), tmp);
                                        }
                                    }
                                }
                                for (int i = 0; i < api.World.Items.Count; i++)
                                {
                                    Item iItem = api.World.Items[i];
                                    if (iItem != null && iItem.WildCardMatch(new AssetLocation(val.Tool)))
                                    {
                                        SwapBlocks tmp = val.Copy();
                                        tmp.Tool = iItem.Code.ToString();
                                        if (!swapSystem.SwapPairs.ContainsKey(GetKey(tmp.Tool)))
                                        {
                                            swapSystem.SwapPairs.Add(GetKey(tmp.Tool), tmp);
                                        }
                                    }
                                }
                            }
                            else if (!swapSystem.SwapPairs.ContainsKey(GetKey(val.Tool)))
                            {
                                swapSystem.SwapPairs.Add(GetKey(val.Tool), val);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        disabled = true;
                        api.World.Logger.Notification("Deprecated or unsupported use of swapblocks in " + block.Code.ToString());
                    }
                }
            }
            else
            {
                disabled = true;
                return;
            }
        }

        public string GetKey(string holdingstack)
        {
            return holdingstack + block.Code.ToString() + block.Id;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handled)
        {
            handled = EnumHandling.PreventDefault;
            return true;
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handled)
        {
            handled = EnumHandling.PreventDefault;
            SwapSystem swapSystem = api.ModLoader.GetModSystem<SwapSystem>();
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            string key = GetKey(slot?.Itemstack?.Collectible?.Code?.ToString()) ?? "";

            ((byPlayer.Entity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
            if (swapSystem.SwapPairs.TryGetValue(key, out SwapBlocks swap))
            {
                if (world.Side.IsClient() && secondsUsed != 0 && swap.MakeTime != 0 && !block.HasBehavior<BlockCreateBehavior>())
                {
                    ((byPlayer.Entity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                    float animstep = (secondsUsed / swap.MakeTime) * 1.0f;
                    api.ModLoader.GetModSystem<ShaderTest>().progressBar = animstep;
                }
                return secondsUsed < swap.MakeTime;
            }
            return false;
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            BlockPos pos = blockSel.Position;
            ModSystemBlockReinforcement bR = api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
            if (disabled || bR.IsReinforced(pos) || bR.IsLocked(pos, byPlayer)) return;

            SwapSystem swapSystem = api.ModLoader.GetModSystem<SwapSystem>();
            handling = EnumHandling.PreventDefault;
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;


            if (!(requireSneak && !byPlayer.Entity.Controls.Sneak) && slot.Itemstack != null)
            {
                string key = GetKey(slot.Itemstack.Collectible.Code.ToString());

                if (swapSystem.SwapPairs.TryGetValue(key, out SwapBlocks swap))
                {
                    if (world.Side.IsClient()) api.ModLoader.GetModSystem<ShaderTest>().progressBar = 0;

                    if (swap.Takes != null && swap.Takes != block.Code.ToString() || secondsUsed < swap.MakeTime)
                    {
                        return;
                    }

                    AssetLocation asset = slot.Itemstack.Collectible.Code;
                    if (asset.ToString() == swap.Tool)
                    {
                        AssetLocation toAsset = new AssetLocation(swap.Makes.WithDomain());
                        Block toBlock = toAsset.GetBlock(world.Api);

                        int count = swap.Count;

                        if (count != 0)
                        {
                            if (count < 0)
                            {
                                ItemStack withCount = slot.Itemstack.Clone();
                                withCount.StackSize = Math.Abs(count);
                                if (!byPlayer.InventoryManager.TryGiveItemstack(withCount))
                                {
                                    world.SpawnItemEntity(withCount, pos.ToVec3d().Add(0.5, 0.5, 0.5));
                                }
                            }
                            else if (slot.Itemstack.StackSize >= count)
                            {
                                if (byPlayer.WorldData.CurrentGameMode.IsSurvival()) slot.TakeOut(count);
                            }
                            else return;
                        }

                        if ((block.EntityClass != null && toBlock.EntityClass != null) && (toBlock.EntityClass == block.EntityClass))
                        {
                            world.BlockAccessor.ExchangeBlock(toBlock.BlockId, pos);
                        }
                        else
                        {
                            world.BlockAccessor.SetBlock(toBlock.BlockId, pos);
                        }
                        slot.MarkDirty();
                        PlaySoundDispenseParticles(world, pos, slot);
                    }
                }
            }
        }

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handled)
        {
            if (world.Side.IsClient()) api.ModLoader.GetModSystem<ShaderTest>().progressBar = 0;
            return false;
        }

        public void PlaySoundDispenseParticles(IWorldAccessor world, BlockPos pos, ItemSlot slot)
        {
            try
            {
                if (world.Side.IsServer())
                {
                    world.SpawnCubeParticles(pos, pos.ToVec3d().Add(particleOrigin), pRadius, pQuantity);
                    world.SpawnCubeParticles(pos.ToVec3d().Add(particleOrigin), slot.Itemstack, pRadius, pQuantity);
                    if (playSound)
                    {
                        world.PlaySoundAt(block.Sounds.Place, pos.X, pos.Y, pos.Z);
                    }
                }
            }
            catch (Exception) { }
        }
    }
}
