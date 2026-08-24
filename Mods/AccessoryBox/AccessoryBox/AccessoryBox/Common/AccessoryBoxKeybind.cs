using tContentPatch.Input;
using Terraria.GameInput;

namespace AccessoryBox.Common
{
    /// <summary>
    /// 饰品箱快捷键管理器（接入 tPlainModLoader 统一 ModKeybind 系统）
    /// 作者: SaintCirno9
    /// </summary>
    public static class AccessoryBoxKeybind
    {
        /// <summary>
        /// 注册到 tpml 统一系统的 ModKeybind 实例
        /// </summary>
        public static ModKeybind ToggleKeybind { get; private set; }

        /// <summary>
        /// 显式初始化并向统一系统注册快捷键
        /// </summary>
        public static void Initialize()
        {
            if (ToggleKeybind == null)
            {
                ToggleKeybind = KeybindLoader.RegisterKeybind(
                    modName: "AccessoryBox",
                    name: "ToggleAccessoryBox",
                    defaultBinding: "None",
                    displayName: "开关饰品箱 (Toggle Accessory Box)"
                );
            }
        }

        /// <summary>
        /// 获取当前绑定的主按键名称（支持原版设置中改键）
        /// </summary>
        public static string GetCurrentBoundKey()
        {
            if (ToggleKeybind == null) return "None";
            var keys = ToggleKeybind.GetAssignedKeys(InputMode.Keyboard);
            return keys != null && keys.Count > 0 ? keys[0] : "None";
        }
    }
}
