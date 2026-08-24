using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameInput;

namespace tContentPatch.Input
{
    /// <summary>
    /// 表示一个已注册的模组快捷键
    /// </summary>
    public class ModKeybind
    {
        /// <summary>
        /// 所属模组名称
        /// </summary>
        public string ModName { get; internal set; }

        /// <summary>
        /// 快捷键内部标识符
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// 快捷键完整 Trigger 标识（格式为 "ModName/KeybindName"）
        /// </summary>
        public string FullName => $"{ModName}/{Name}";

        /// <summary>
        /// 默认绑定的按键名称（如 "C", "LeftControl", "Mouse3" 等）
        /// </summary>
        public string DefaultBinding { get; internal set; }

        /// <summary>
        /// 在设置界面中显示的友好名称
        /// </summary>
        public string DisplayName { get; internal set; }

        /// <summary>
        /// 当前帧按键是否处于被按住状态（在打字、聊天、编辑告示牌或输入框聚焦时自动全局静默）
        /// </summary>
        public bool Current
        {
            get
            {
                if (Main.drawingPlayerChat || Main.editSign || Main.editChest || Main.blockInput || PlayerInput.WritingText) return false;
                if (PlayerInput.Triggers?.Current?.KeyStatus == null) return false;
                return PlayerInput.Triggers.Current.KeyStatus.TryGetValue(FullName, out bool val) && val;
            }
        }

        /// <summary>
        /// 当前帧按键是否刚被按下（边缘单次触发，具备多层冗余保障，在打字时自动全局静默）
        /// </summary>
        public bool JustPressed
        {
            get
            {
                if (Main.drawingPlayerChat || Main.editSign || Main.editChest || Main.blockInput || PlayerInput.WritingText) return false;
                if (PlayerInput.Triggers?.JustPressed?.KeyStatus != null &&
                    PlayerInput.Triggers.JustPressed.KeyStatus.TryGetValue(FullName, out bool val) && val)
                {
                    return true;
                }

                // 冗余容错保障：若原版 TriggersSet 差分字典未及时覆盖，直接由 Current 与 Old 状态计算
                if (PlayerInput.Triggers?.Current?.KeyStatus != null && PlayerInput.Triggers?.Old?.KeyStatus != null)
                {
                    bool cur = PlayerInput.Triggers.Current.KeyStatus.TryGetValue(FullName, out bool c) && c;
                    bool old = PlayerInput.Triggers.Old.KeyStatus.TryGetValue(FullName, out bool o) && o;
                    return cur && !old;
                }

                return false;
            }
        }

        /// <summary>
        /// 当前帧按键是否刚被释放（在打字时自动全局静默）
        /// </summary>
        public bool JustReleased
        {
            get
            {
                if (Main.drawingPlayerChat || Main.editSign || Main.editChest || Main.blockInput || PlayerInput.WritingText) return false;
                if (PlayerInput.Triggers?.JustReleased?.KeyStatus != null &&
                    PlayerInput.Triggers.JustReleased.KeyStatus.TryGetValue(FullName, out bool val) && val)
                {
                    return true;
                }

                if (PlayerInput.Triggers?.Current?.KeyStatus != null && PlayerInput.Triggers?.Old?.KeyStatus != null)
                {
                    bool cur = PlayerInput.Triggers.Current.KeyStatus.TryGetValue(FullName, out bool c) && c;
                    bool old = PlayerInput.Triggers.Old.KeyStatus.TryGetValue(FullName, out bool o) && o;
                    return !cur && old;
                }

                return false;
            }
        }

        /// <summary>
        /// 上一帧按键是否处于被按住状态（在打字时自动全局静默）
        /// </summary>
        public bool Old
        {
            get
            {
                if (Main.drawingPlayerChat || Main.editSign || Main.editChest || Main.blockInput || PlayerInput.WritingText) return false;
                if (PlayerInput.Triggers?.Old?.KeyStatus == null) return false;
                return PlayerInput.Triggers.Old.KeyStatus.TryGetValue(FullName, out bool val) && val;
            }
        }

        internal ModKeybind(string modName, string name, string defaultBinding, string displayName = null)
        {
            ModName = modName ?? throw new ArgumentNullException(nameof(modName));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DefaultBinding = defaultBinding ?? "";
            DisplayName = displayName ?? name;
        }

        /// <summary>
        /// 获取指定输入模式下当前配置绑定的按键名称列表
        /// </summary>
        public List<string> GetAssignedKeys(InputMode mode = InputMode.Keyboard)
        {
            if (PlayerInput.CurrentProfile != null &&
                PlayerInput.CurrentProfile.InputModes.TryGetValue(mode, out var config) &&
                config.KeyStatus.TryGetValue(FullName, out var keys))
            {
                return new List<string>(keys);
            }
            return new List<string>();
        }

        public override string ToString() => FullName;
    }
}
