using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Utilities;

namespace tContentPatch
{
    /// <summary>
    /// 物块光照兼容基类（建议直接继承 <see cref="TPML.Content.ModSystem"/> 并使用 HookGen 门面）。
    /// 作者: SaintCirno9
    /// </summary>
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
