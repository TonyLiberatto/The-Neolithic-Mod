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
        public static bool Hit(IWorldAccessor world, EntityAgent byEntity, float secondsUsed)
        {
            if (world.Side == EnumAppSide.Client)
            {
                ModelTransform tf = Transform();

                tf.Rotation.X -= (float)Math.Sin(secondsUsed * 6) * 90;

                byEntity.Controls.UsingHeldItemTransformAfter = tf;
                return tf.Rotation.X > -80;
            }
            return true;
        }

        public static bool Slaughter(IWorldAccessor world, EntityAgent byEntity, float secondsUsed)
        {
            if (world.Side == EnumAppSide.Client)
            {
                ModelTransform tf = Transform();
                tf.Rotation.Y -= (float)Math.Sin(secondsUsed * 6) * 90;
                tf.Rotation.X -= (float)Math.Sin(secondsUsed * 6) * 90;
                tf.Translation.Z -= (float)Math.Sin(secondsUsed * 6);

                byEntity.Controls.UsingHeldItemTransformAfter = tf;
                return tf.Rotation.Y > -89;
            }
            return true;
        }

        public static bool Collect(IWorldAccessor world, EntityAgent byEntity, float secondsUsed)
        {
            if (world.Side == EnumAppSide.Client)
            {
                ModelTransform tf = Transform();
                float scale = ((float)Math.Sin(secondsUsed*2) * 0.5f);
                tf.ScaleXYZ.Add(-scale, -scale, -scale);
                tf.Translation.Z -= (scale*2);
                byEntity.Controls.UsingHeldItemTransformBefore = tf;

                return scale > 0.0;
            }
            return true;
        }

        public static ModelTransform Transform()
        {
            ModelTransform tf = new ModelTransform();
            tf.EnsureDefaultValues();
            tf.Origin.Set(0f, 0f, 0f);
            tf.Translation.Set(0f, 0f, 0f);

            return tf;
        }
    }
}
