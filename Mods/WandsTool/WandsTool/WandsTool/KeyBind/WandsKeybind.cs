using System.Collections.Generic;
using System.Linq;
using tContentPatch.Input;
using Terraria.GameInput;

namespace WandsTool.KeyBind
{
    /// <summary>
    /// 魔杖工具快捷键管理器（已完全接入 tPlainModLoader 统一 ModKeybind 系统）
    /// 作者: SaintCirno9
    /// </summary>
    public static class WandsKeybind
    {
        /// <summary>
        /// 开关魔杖模式快捷键
        /// </summary>
        public static ModKeybind ToggleWand { get; private set; }

        /// <summary>
        /// 蓝图放置模式: 水平镜像翻转
        /// </summary>
        public static ModKeybind FlipHorizontal { get; private set; }

        /// <summary>
        /// 蓝图放置模式: 垂直镜像翻转
        /// </summary>
        public static ModKeybind FlipVertical { get; private set; }

        /// <summary>
        /// 施工一键撤销: 撤销上一次魔杖批量操作（物块/墙/液体），并智能回滚消耗物料
        /// </summary>
        public static ModKeybind UndoAction { get; private set; }

        /// <summary>
        /// 显式初始化并向统一系统注册快捷键
        /// </summary>
        public static void Initialize()
        {
            if (ToggleWand == null)
            {
                ToggleWand = KeybindLoader.RegisterKeybind(
                    modName: "WandsTool",
                    name: "ToggleWand",
                    defaultBinding: "Z",
                    displayName: "开关魔杖模式 (Toggle Wand Mode)"
                );
            }

            if (FlipHorizontal == null)
            {
                FlipHorizontal = KeybindLoader.RegisterKeybind(
                    modName: "WandsTool",
                    name: "FlipHorizontal",
                    defaultBinding: "H",
                    displayName: "魔杖蓝图: 水平镜像翻转 (Flip Horizontal)"
                );
            }

            if (FlipVertical == null)
            {
                FlipVertical = KeybindLoader.RegisterKeybind(
                    modName: "WandsTool",
                    name: "FlipVertical",
                    defaultBinding: "V",
                    displayName: "魔杖蓝图: 垂直镜像翻转 (Flip Vertical)"
                );
            }

            if (UndoAction == null)
            {
                UndoAction = KeybindLoader.RegisterKeybind(
                    modName: "WandsTool",
                    name: "UndoAction",
                    defaultBinding: "U",
                    displayName: "魔杖施工: 一键撤销 (Undo Last Build)"
                );
            }
        }

        /// <summary>
        /// 获取当前绑定的主按键名称（支持原版设置中改键）
        /// </summary>
        public static string GetCurrentBoundKey()
        {
            if (ToggleWand == null) return "Z";
            var keys = ToggleWand.GetAssignedKeys(InputMode.Keyboard);
            return keys != null && keys.Count > 0 ? keys[0] : "None";
        }
    }
}
