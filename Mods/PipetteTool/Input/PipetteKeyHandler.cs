using System;
using System.Collections.Generic;
using System.Linq;
using tContentPatch.Input;
using Terraria.GameInput;

namespace PipetteTool.Input
{
    /// <summary>
    /// 吸管工具按键输入捕获与分发器（已接入 tPlainModLoader 统一快捷键系统）
    /// </summary>
    public static class PipetteKeyHandler
    {
        /// <summary>
        /// 注册到 tpml 统一系统的 ModKeybind 实例
        /// </summary>
        public static ModKeybind PickKeybind { get; private set; }

        /// <summary>
        /// 显式初始化并注册快捷键
        /// </summary>
        public static void Initialize()
        {
            if (PickKeybind == null)
            {
                PickKeybind = KeybindLoader.RegisterKeybind(
                    modName: "PipetteTool",
                    name: "PickColor",
                    defaultBinding: "Q",
                    displayName: "吸取物块与颜色样式 (Pick Block)"
                );
            }
        }

        private static bool _hasTriggeredThisPress = false;

        /// <summary>
        /// 每帧更新输入状态并触发回调（严格保证物理单次按下触发一次）
        /// </summary>
        public static void UpdateInput(Action onTrigger)
        {
            if (PickKeybind == null) return;

            // 基于 Current 状态 + 显式物理按压锁，确保按下过程 100% 仅调度一次
            if (PickKeybind.Current)
            {
                if (!_hasTriggeredThisPress)
                {
                    _hasTriggeredThisPress = true;
                    onTrigger?.Invoke();
                }
            }
            else
            {
                _hasTriggeredThisPress = false;
            }
        }

        /// <summary>
        /// 获取当前绑定的主按键名称（供 ModSetting 界面显示使用）
        /// </summary>
        public static string GetCurrentBoundKey()
        {
            if (PickKeybind == null) return "Q";
            var keys = PickKeybind.GetAssignedKeys(InputMode.Keyboard);
            return keys != null && keys.Count > 0 ? keys[0] : "None";
        }
    }
}
