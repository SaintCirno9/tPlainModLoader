using System;
using System.Collections.Generic;
using Terraria.GameInput;

namespace tContentPatch.Input
{
    /// <summary>
    /// 表示一个已注册的模组快捷键（旧式 tContentPatch 命名空间兼容垫片）
    /// 作者: SaintCirno9
    /// </summary>
    [Obsolete("请使用 TPML.Content.ModKeybind")]
    public class ModKeybind : TPML.Content.ModKeybind
    {
        public ModKeybind(string modName, string name, string defaultBinding, string displayName = null)
            : base(modName, name, defaultBinding, displayName)
        {
        }
    }
}

