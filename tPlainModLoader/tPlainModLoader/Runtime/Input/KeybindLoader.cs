using System;
using System.Collections.Generic;

namespace tContentPatch.Input
{
    /// <summary>
    /// 模组快捷键注册中心与生命周期管理器（旧式 tContentPatch 命名空间兼容垫片）
    /// 作者: SaintCirno9
    /// </summary>
    [Obsolete("请使用 TPML.Content.KeybindLoader")]
    public static class KeybindLoader
    {
        /// <summary>
        /// 已注册的所有模组快捷键集合
        /// </summary>
        public static IEnumerable<TPML.Content.ModKeybind> Keybinds => TPML.Content.KeybindLoader.Keybinds;

        /// <summary>
        /// 注册一个模组快捷键（通过任意模组对象）
        /// </summary>
        public static TPML.Content.ModKeybind RegisterKeybind(object mod, string name, string defaultBinding, string displayName = null)
        {
            return TPML.Content.KeybindLoader.RegisterKeybind(mod, name, defaultBinding, displayName);
        }

        /// <summary>
        /// 注册一个模组快捷键（通过模组名称）
        /// </summary>
        public static TPML.Content.ModKeybind RegisterKeybind(string modName, string name, string defaultBinding, string displayName = null)
        {
            return TPML.Content.KeybindLoader.RegisterKeybind(modName, name, defaultBinding, displayName);
        }

        /// <summary>
        /// 根据 Trigger 全名查找对应的快捷键
        /// </summary>
        public static bool TryGetKeybind(string fullName, out TPML.Content.ModKeybind keybind)
        {
            return TPML.Content.KeybindLoader.TryGetKeybind(fullName, out keybind);
        }

        /// <summary>
        /// 将所有已注册的模组快捷键完整同步注入原版 PlayerInput 系统
        /// </summary>
        public static void SyncWithPlayerInput()
        {
            TPML.Content.KeybindLoader.SyncWithPlayerInput();
        }

        /// <summary>
        /// 模组卸载时清空注册表，并对称移除已注入到 PlayerInput 的条目
        /// </summary>
        public static void Unload()
        {
            TPML.Content.KeybindLoader.Unload();
        }
    }
}

