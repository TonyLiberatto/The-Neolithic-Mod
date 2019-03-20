using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace TheNeolithicMod
{
    public static class HandAnimations
    {
        public static bool Hit(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.Side == EnumAppSide.Client)
            {
                ModelTransform tf = new ModelTransform();

                tf.EnsureDefaultValues();

                tf.Origin.Set(0f, 0f, 0f);
                tf.Translation.Set(0f, 0f, 0f);

                tf.Rotation.X -= (float)Math.Sin(secondsUsed * 6) * 90;

                byPlayer.Entity.Controls.UsingHeldItemTransformAfter = tf;
                return tf.Rotation.X > -80;
            }
            return true;
        }
    }
}
