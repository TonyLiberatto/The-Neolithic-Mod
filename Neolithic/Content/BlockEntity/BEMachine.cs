using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace TheNeolithicMod
{
    class BlockEntityMachine : BlockEntityAnimatable, IBlockEntityContainer
    {
        public IInventory Inventory { get; }
        public string InventoryClassName { get; }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
        }
    }
}
