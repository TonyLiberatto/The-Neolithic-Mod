using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.ServerMods.NoObf;

namespace Neolithic
{
    class InWorldCraftingSystem : ModSystem
    {
        ICoreServerAPI api;
        ICoreClientAPI capi;
        public Dictionary<AssetLocation, InWorldCraftingRecipe[]> InWorldCraftingRecipes { get; set; } = new Dictionary<AssetLocation, InWorldCraftingRecipe[]>();
        public override double ExecuteOrder() => 1;

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.api = api;
            api.Event.SaveGameLoaded += OnSaveGameLoaded;
            api.RegisterCommand("fireiwcinteract", "", "", (a, b, c) => OnPlayerInteract(a, a.CurrentBlockSelection));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            this.capi = api;
            api.Event.MouseDown += FireCommand;
        }

        private void FireCommand(MouseEvent e)
        {
            if (e.Button == EnumMouseButton.Right)
            {
                capi.SendChatMessage("/fireiwcinteract");
                capi.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
            }
        }

        public void OnSaveGameLoaded()
        {
            InWorldCraftingRecipes = api.Assets.GetMany<InWorldCraftingRecipe[]>(api.Server.Logger, "recipes/inworld");
        }

        private bool OnPlayerInteract(IServerPlayer byPlayer, BlockSelection blockSel)
        {
            BlockPos pos = blockSel?.Position;
            Block block = pos?.GetBlock(api);
            ItemSlot slot = byPlayer?.InventoryManager?.ActiveHotbarSlot;

            if (block == null || slot?.Itemstack == null) return false;
            bool shouldbreak = false;

            foreach (var val in InWorldCraftingRecipes)
            {
                foreach (var recipe in val.Value)
                {
                    if (recipe.Disabled || (recipe.Takes.AllowedVariants != null && !block.WildCardMatch(recipe.Takes.AllowedVariants)) || (recipe.Tool.AllowedVariants != null && !slot.Itemstack.Collectible.WildCardMatch(recipe.Tool.AllowedVariants))) continue;

                    if (block.WildCardMatch(recipe.Takes.Code))
                    {
                        if (IsValid(recipe, slot))
                        {
                            if (recipe.Mode == EnumInWorldCraftingMode.Swap)
                            {
                                var make = recipe.Makes[0];
                                make.Resolve(api.World, null);
                                if (make.Type == EnumItemClass.Block)
                                {
                                    if (recipe.Remove) api.World.BlockAccessor.SetBlock(0, pos);
                                    api.World.BlockAccessor.SetBlock(make.ResolvedItemstack.Block.BlockId, pos);
                                    TakeOrDamage(recipe, slot, byPlayer);
                                    shouldbreak = true;
                                }
                            }
                            else if (recipe.Mode == EnumInWorldCraftingMode.Create)
                            {
                                foreach (var make in recipe.Makes)
                                {
                                    make.Resolve(api.World, null);
                                    api.World.SpawnItemEntity(make.ResolvedItemstack, pos.MidPoint(), new Vec3d(0.0, 0.1, 0.0));
                                }
                                TakeOrDamage(recipe, slot, byPlayer);
                                if (recipe.Remove) api.World.BlockAccessor.SetBlock(0, pos);
                                shouldbreak = true;
                            }
                            api.World.PlaySoundAt(recipe.CraftSound, pos);
                        }
                        break;
                    }
                    slot.MarkDirty();
                }
                if (shouldbreak) return true;
            }
            return false;
        }

        public void TakeOrDamage(InWorldCraftingRecipe recipe, ItemSlot slot, IServerPlayer byPlayer)
        {
            if (recipe.IsTool)
            {
                slot.Itemstack.Collectible.DamageItem(api.World, byPlayer.Entity, slot);
            }
            else
            {
                slot.TakeOut(recipe.Tool.StackSize);
            }
        }

        public bool IsValid(InWorldCraftingRecipe recipe, ItemSlot slot) => 
            (recipe.Tool.Code.ToString() == slot.Itemstack?.Collectible?.Code?.ToString() && slot.Itemstack?.StackSize >= recipe.Tool.StackSize) || 
            (recipe.Tool.Code.IsWildCard && recipe.Tool.Code.GetMatches(api).Any(t => t.ToString() == slot.Itemstack?.Collectible?.Code?.ToString() && slot.Itemstack?.StackSize >= recipe.Tool.StackSize));
    }

    class InWorldCraftingRecipe
    {
        public EnumInWorldCraftingMode Mode { get; set; } = EnumInWorldCraftingMode.Swap;
        public JsonAllowedVariants Takes { get; set; }
        public JsonAllowedVariants Tool { get; set; }
        public JsonItemStack[] Makes { get; set; }
        public AssetLocation CraftSound { get; set; } = new AssetLocation("sounds/block/planks");
        public bool IsTool { get; set; } = false;
        public bool Disabled { get; set; } = false;
        public bool Remove { get; set; } = false;
        public float MakeTime { get; set; } = 0f;
    }

    class JsonAllowedVariants : JsonItemStack
    {
        public AssetLocation[] AllowedVariants { get; set; }
    }

    enum EnumInWorldCraftingMode
    {
        Swap, Create
    }
}
