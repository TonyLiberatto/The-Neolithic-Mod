using Vintagestory.API.Common;

namespace TheNeolithicMod
{
    class CraftingProp
    {
        public JsonItemStack input { get; set; }
        public JsonItemStack[] output { get; set; }
        public EnumTool? tool { get; set; } = EnumTool.Axe;
        public string craftSound { get; set; }
        public int craftTime { get; set; } = 500;
    }
}