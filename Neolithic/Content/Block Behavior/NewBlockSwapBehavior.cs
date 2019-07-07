﻿using Newtonsoft.Json;
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

namespace TheNeolithicMod
{
    class SwapSystem : ModSystem
    {
        public Dictionary<string, SwapBlocks> SwapPairs { get; set; } = new Dictionary<string, SwapBlocks>();
    }

    class SwapBlocks
    {
        public string Takes = "";
        public string Makes = "";
        public string Tool = null;
        public int Count = 0;
    }

    class NewBlockSwapBehavior : BlockBehavior
    {
        ICoreAPI api;
        Vec3d particleOrigin = new Vec3d(0.5, 0.5, 0.5);
        bool requireSneak = false;
        bool disabled = false;
        int pRadius = 2;
        int pQuantity = 16;

        public NewBlockSwapBehavior(Block block) : base(block) { }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            this.api = api;
            PostOLInit(block.GetBehavior<NewBlockSwapBehavior>().properties);
        }

        public void PostOLInit(JsonObject properties)
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
                try
                {
                    SwapBlocks[] swapBlocks = properties["swapBlocks"].AsObject<SwapBlocks[]>();
                    foreach (var val in swapBlocks)
                    {
                        if (!swapSystem.SwapPairs.ContainsKey(GetKey(val.Makes)) && !val.Makes.Contains("{"))
                        {
                            if (!(val.Takes != null && !val.Takes.Contains("{")))
                            {
                                swapSystem.SwapPairs.Add(GetKey(val.Tool), val);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    disabled = true;
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
            string combined = "";
            combined += holdingstack;
            combined += block.Code.ToString();
            return GameMath.Md5Hash(combined);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            if (disabled) return base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
            SwapSystem swapSystem = api.ModLoader.GetModSystem<SwapSystem>();
            handling = EnumHandling.PreventDefault;
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            BlockPos pos = blockSel.Position;

            if (requireSneak && !byPlayer.Entity.Controls.Sneak) return true;

            if (slot.Itemstack != null)
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
                        ((byPlayer.Entity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

                        string toCode = swap.Makes;
                        if (toCode.IndexOf(":") == -1) toCode = block.Code.Domain + ":" + toCode;
                        AssetLocation toAsset = new AssetLocation(toCode);
                        Block toBlock = toAsset.GetBlock(world.Api);

                        int count = 0;
                        try { count = Convert.ToInt32(swap.Count); } catch (Exception) { }

                        if (count > 0 && slot.Itemstack.StackSize >= count)
                        {
                            if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Survival) slot.TakeOut(count); slot.MarkDirty();
                            world.BlockAccessor.SetBlock(toBlock.BlockId, pos);
                            PlaySoundDispenseParticles(world, pos, slot);
                        }
                        else if (count == 0)
                        {
                            world.BlockAccessor.SetBlock(toBlock.BlockId, pos);
                            PlaySoundDispenseParticles(world, pos, slot);
                        }
                        else if (count < 0)
                        {
                            ItemStack withCount = slot.Itemstack.Clone();
                            withCount.StackSize = Math.Abs(count);
                            byPlayer.InventoryManager.TryGiveItemstack(withCount); slot.MarkDirty();
                            world.BlockAccessor.SetBlock(toBlock.BlockId, pos);
                            PlaySoundDispenseParticles(world, pos, slot);
                        }
                    }
                }
            }
            return true;
        }

        public void PlaySoundDispenseParticles(IWorldAccessor world, BlockPos pos, ItemSlot slot)
        {
            if (world.Side.IsServer())
            {
                try
                {
                    world.SpawnCubeParticles(pos, pos.ToVec3d().Add(particleOrigin), pRadius, pQuantity);
                    world.SpawnCubeParticles(pos.ToVec3d().Add(particleOrigin), slot.Itemstack, pRadius, pQuantity);
                }
                catch (Exception) { }
            }
            else
            {
                world.PlaySoundAt(block.Sounds.Place, pos.X, pos.Y, pos.Z);
            }
        }
    }
}
