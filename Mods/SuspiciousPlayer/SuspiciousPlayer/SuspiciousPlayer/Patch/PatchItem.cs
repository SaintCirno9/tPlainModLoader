using Terraria.DataStructures;

namespace SuspiciousPlayer.Patch
{
    internal class PatchItem : tContentPatch.PatchItem
    {
        public delegate void EventNewItem(int x, int y, int type);
        public static EventNewItem OnNewItem = null;

        public override void NewItemPostfix(int __result, IEntitySource source, int X, int Y, int Width, int Height, int Type, int Stack, bool noBroadcast, int pfix, bool noGrabDelay)
        {
            OnNewItem?.Invoke(X, Y, Type);
        }
    }
}
