using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Utilities;

namespace tContentPatch
{
    /// <summary>
    /// 已废弃的物块光照 Patch 基类。请迁移至 <see cref="TPML.Content.ModSystem"/> 并使用 HookGen 门面。
    /// 作者: SaintCirno9
    /// </summary>
    [Obsolete("TPML 现代体系已全面替代 Patch*，请继承 ModSystem / ModPlayer / Global*")]
    public abstract class PatchTileLightScanner
    {
        /// <summary>
        /// <see cref="Mod.Loaded"/>后调用
        /// </summary>
        public virtual void Initialize() { }
        /// <summary/>
        public virtual void ApplyTileLightPrefix(Tile tile, int x, int y, ref FastRandom localRandom, ref Vector3 lightColor) { }
    }
}
