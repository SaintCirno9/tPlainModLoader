using Terraria;

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
    }
}
