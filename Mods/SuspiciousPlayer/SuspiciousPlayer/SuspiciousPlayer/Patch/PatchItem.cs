using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using TPML.Content;

namespace SuspiciousPlayer.Patch
{
    internal class PatchItem : Mod
    {
        public delegate void EventNewItem(float x, float y, int type);
        public static EventNewItem OnNewItem = null;

        public override void Load()
        {
            On_Item.NewItem_IEntitySource_Vector2_int_int_int_NewItemOwnership_Nullable1_NewItemModifier_bool += (orig, source, center, type, stack, prefix, ownership, velocity, modifier, noBroadcast) =>
            {
                int result = orig(source, center, type, stack, prefix, ownership, velocity, modifier, noBroadcast);
                OnNewItem?.Invoke(center.X, center.Y, type);
                return result;
            };
        }
    }
}


