using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace TheNeolithicMod
{
    class LootVesselFix : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            BlockLootVessel.lootLists.Clear();
            VesselDrops drops = api.Assets.TryGet("config/vesseldrops.json").ToObject<VesselDrops>();
            foreach (var val in drops.vessels)
            {
                BlockLootVessel.lootLists[val.name] = LootList.Create(val.tries, val.ToLootItems());
            }
        }
    }

    class LootVesselDrop
    {
        public float chance { get; set; }
        public int minQuantity { get; set; }
        public int maxQuantity { get; set; }
        public EnumItemClass type { get; set; } = EnumItemClass.Item;
        public List<string> items { get; set; }
    }

    class Vessels
    {
        public string name { get; set; }
        public float tries { get; set; }
        public List<LootVesselDrop> drops { get; set; }

        public LootItem[] ToLootItems()
        {
            List<LootItem> lootItems = new List<LootItem>();
            foreach (var val in drops)
            {
                LootItem item = new LootItem();
                item.chance = val.chance;
                item.codes = val.items.ToAssets();
                item.minQuantity = val.minQuantity;
                item.maxQuantity = val.maxQuantity;
                item.type = val.type;
                lootItems.Add(item);
            }
            return lootItems.ToArray();
        }
    }

    class VesselDrops
    {
        public List<Vessels> vessels { get; set; }
    }
}
