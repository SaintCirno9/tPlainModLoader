using HarmonyLib;
using Terraria;

namespace AccessoryBox.Common
{
    /// <summary>
    /// 当玩家光标在 UI 界面或饰品箱窗口上时，阻止鼠标滚轮误切换快捷栏物品
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch(typeof(Player), "HandleHotbarControls")]
    public class Patch_HotbarScroll
    {
        [HarmonyPrefix]
        public static bool Prefix(Player __instance)
        {
            // 当鼠标在 UI 交互界面上、或者饰品箱打开且鼠标悬停在窗口内时，跳过快捷栏滚轮处理
            if (__instance.mouseInterface)
            {
                return false;
            }

            if (ModifyInterfaceLayers.BoxIsOpen && ModifyInterfaceLayers.BoxIsHovering)
            {
                return false;
            }

            return true;
        }
    }
}
