using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;
using TPML.Content;

namespace tContentPatch
{
    /// <summary>
    /// 生命周期兼容基类（建议直接继承 <see cref="TPML.Content.ModSystem"/>）。
    /// 作者: SaintCirno9
    /// </summary>
    public abstract class PatchMain : TPML.Content.ModSystem
    {
    }
}
