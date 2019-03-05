using Vintagestory.API.Common;
using Vintagestory.GameContent;


namespace TheNeolithicMod
{
    public class ItemSickle : ItemShears
    {
        string[] allowedPrefixes;

        public override int MultiBreakQuantity { get { return 2; } }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            allowedPrefixes = Attributes["codePrefixes"].AsStringArray();
        }

        public override bool CanMultiBreak(Block block)
        {
            for (int i = 0; i < allowedPrefixes.Length; i++)
            {
                if (block.Code.Path.StartsWith(allowedPrefixes[i])) return true;
            }
            return false;   
        }
    }
}
