using System;
using Terraria;

namespace tContentPatch
{
    /// <summary>
    /// 已废弃的 Chest Patch 基类。
    /// 作者: SaintCirno9
    /// </summary>
    [Obsolete("TPML 现代体系已全面替代 Patch*，请继承 ModSystem / ModPlayer / Global*")]
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
