using Terraria;
using Terraria.DataStructures;

namespace tContentPatch
{
    /// <summary/>
    public abstract class PatchItem
    {
        /// <summary>
        /// <see cref="Mod.Loaded"/>后调用
        /// </summary>
        public virtual void Initialize() { }
        /// <summary>
        /// <see cref="Item.UpdateItem(int)"/>前调用
        /// </summary>
        public virtual void UpdateItemPrefix(Item This, int i) { }
        /// <summary>
        /// <see cref="Item.UpdateItem(int)"/>后调用
        /// </summary>
        public virtual void UpdateItemPostfix(Item This, int i) { }
        /// <summary>
        /// <see cref="Item.NewItem(IEntitySource, int, int, int, int, int, int, bool, int, bool, bool)"/>后调用
        /// </summary>
        public virtual void NewItemPostfix(int __result, IEntitySource source,
            int X, int Y, int Width, int Height, int Type, int Stack,
            bool noBroadcast, int pfix, bool noGrabDelay, bool reverseLookup)
        { }
    }
}
