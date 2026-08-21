using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;

namespace SuspiciousPlayer.Patch
{
    internal class PatchItem : tContentPatch.PatchItem
    {
        public delegate void EventNewItem(float x, float y, int type);
        public static EventNewItem OnNewItem = null;

        public override void NewItemPostfix(int __result, IEntitySource source, Vector2 center, int type, int stack, int prefix, NewItemOwnership ownership, Vector2? velocity, Item.NewItemModifier modifier, bool noBroadcast)
        {
            OnNewItem?.Invoke(center.X, center.Y, type);
        }
    }
}
