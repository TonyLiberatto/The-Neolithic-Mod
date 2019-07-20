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

namespace TheNeolithicMod
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class Swap
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
                .RegisterMessageType<Swap>()
                .SetMessageHandler<Swap>(a =>
                {
                    SwapPairs = JsonConvert.DeserializeObject<Dictionary<string, SwapBlocks>>(a.SwapPairs);
                });
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sChannel = api.Network.RegisterChannel("swapPairs").RegisterMessageType<Swap>();
            api.Event.PlayerJoin += PlayerJoin;
        }

        private void PlayerJoin(IServerPlayer byPlayer)
        {
            sChannel.SendPacket(new Swap() { SwapPairs = JsonConvert.SerializeObject(SwapPairs) }, byPlayer);
        }

        public Dictionary<string, SwapBlocks> SwapPairs { get; set; } = new Dictionary<string, SwapBlocks>();
    }

    class SwapBlocks
    {
        public string Takes { get; set; } = "";
        public string Makes { get; set; } = "";
        public string Tool { get; set; } = null;
        public int Count { get; set; } = 0;

        public SwapBlocks(string takes, string makes, string tool, int count)
        {
            Takes = takes; Makes = makes; Tool = tool; Count = count;
        }

        public SwapBlocks Copy()
        {
            SwapBlocks copy = new SwapBlocks(Takes, Makes, Tool, Count);
            return copy;
        }
    }

    class BlockSwapBehavior : BlockBehavior
    {
        ICoreAPI api;
        Vec3d particleOrigin = new Vec3d(0.5, 0.5, 0.5);
        bool requireSneak = false;
        bool disabled = false;
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

            if (properties["allowedVariants"].Exists)
            {
                string[] allowed = properties["allowedVariants"].AsArray<string>();
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
                                for (int i = 0; i < api.World.Blocks.Length; i++)
                                {
                                    Block iBlock = api.World.Blocks[i];
                                    if (iBlock != null && iBlock.WildCardMatch(new AssetLocation(val.Tool)))
                                    {
                                        SwapBlocks tmp = val.Copy();
                                        string code = iBlock.Code.ToString().IndexOf(":") == -1 ? code = "game:" + iBlock.Code.ToString() : iBlock.Code.ToString();
                                        tmp.Tool = code;
                                        if (!swapSystem.SwapPairs.ContainsKey(GetKey(tmp.Tool)))
                                        {
                                            swapSystem.SwapPairs.Add(GetKey(tmp.Tool), tmp);
                                        }
                                    }
                                }
                                for (int i = 0; i < api.World.Items.Length; i++)
                                {
                                    Item iItem = api.World.Items[i];
                                    if (iItem != null && iItem.WildCardMatch(new AssetLocation(val.Tool)))
                                    {
                                        SwapBlocks tmp = val.Copy();
                                        string code = iItem.Code.ToString().IndexOf(":") == -1 ? code = "game:" + iItem.Code.ToString() : iItem.Code.ToString();
                                        tmp.Tool = code;
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
            return GameMath.Md5Hash(holdingstack + block.Code.ToString() + block.Id);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            if (disabled) return base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
            SwapSystem swapSystem = api.ModLoader.GetModSystem<SwapSystem>();
            handling = EnumHandling.PreventDefault;
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            BlockPos pos = blockSel.Position;

            if (!(requireSneak && !byPlayer.Entity.Controls.Sneak) && slot.Itemstack != null)
            {
                string key = GetKey(slot.Itemstack.Collectible.Code.ToString());

                if (swapSystem.SwapPairs.TryGetValue(key, out SwapBlocks swap))
                {
                    if (swap.Takes != null && swap.Takes != block.Code.ToString())
                    {
                        return true;
                    }
                    AssetLocation asset = slot.Itemstack.Collectible.Code;
                    if (asset.ToString() == swap.Tool)
                    {
                        string toCode = swap.Makes;
                        if (toCode.IndexOf(":") == -1) toCode = block.Code.Domain + ":" + toCode;
                        AssetLocation toAsset = new AssetLocation(toCode);
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
                            else return true;
                        }

                        ((byPlayer.Entity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

                        if (block.EntityClass != null && toBlock.EntityClass != null)
                        {
                            if (toBlock.EntityClass == block.EntityClass)
                            {
                                world.BlockAccessor.ExchangeBlock(toBlock.BlockId, pos);
                            }
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
            return true;
        }

        public void PlaySoundDispenseParticles(IWorldAccessor world, BlockPos pos, ItemSlot slot)
        {
            try
            {
                if (world.Side.IsServer())
                {
                    world.SpawnCubeParticles(pos, pos.ToVec3d().Add(particleOrigin), pRadius, pQuantity);
                    world.SpawnCubeParticles(pos.ToVec3d().Add(particleOrigin), slot.Itemstack, pRadius, pQuantity);
                    world.PlaySoundAt(block.Sounds.Place, pos.X, pos.Y, pos.Z);
                }
            }
            catch (Exception) { }
        }
    }
}
