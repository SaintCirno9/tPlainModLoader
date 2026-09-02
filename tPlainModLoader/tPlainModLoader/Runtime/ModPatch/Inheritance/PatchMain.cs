using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;
using TPML.Content;

namespace tContentPatch
{
    /// <summary>
    /// 已废弃的生命周期 Patch 基类。请迁移至 <see cref="TPML.Content.ModSystem"/>。
    /// 作者: SaintCirno9
    /// </summary>
    [Obsolete("TPML 现代体系已全面替代 Patch*，请继承 ModSystem / ModPlayer / Global*")]
    public abstract class PatchMain : TPML.Content.ModSystem
    {
    }
}
