using System;
using Terraria;

namespace tContentPatch
{
    /// <summary>
    /// Chest 商店兼容基类。
    /// 作者: SaintCirno9
    /// </summary>
    public abstract class PatchChest
    {
        /// <summary>
        /// <see cref="Mod.Loaded"/>后调用
        /// </summary>
        public virtual void Initialize() { }
        /// <summary>
        /// 设置商店时, <paramref name="type"/>是商店类型
        /// </summary>
        public virtual void SetupShop(Chest This, int type) { }
    }
}
