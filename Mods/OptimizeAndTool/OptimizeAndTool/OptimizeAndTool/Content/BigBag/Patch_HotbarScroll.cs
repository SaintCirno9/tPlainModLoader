using HarmonyLib;
using OptimizeAndTool.Content.Creative;
using Terraria;
using Terraria.GameInput;

namespace OptimizeAndTool.Content.BigBag
{
    /// <summary>
    /// 当玩家光标在自定义悬浮窗口（大背包/饰品箱/物品浏览器）上时，阻止滚轮误切快捷栏
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch(typeof(Player), "HandleHotbarControls")]
    public class Patch_HotbarScroll
    {
        [HarmonyPrefix]
        public static void Prefix(Player __instance)
        {
            // 当大背包、饰品箱或物品浏览器打开且鼠标悬停在窗口内时，清空当前帧快捷栏滚轮增量
            if ((ModifyInterfaceLayers.BigBagIsOpen && ModifyInterfaceLayers.BigBagIsHovering) ||
                (ModifyInterfaceLayers.BoxIsOpen && ModifyInterfaceLayers.BoxIsHovering) ||
                (CreativeInventory.IsOpen && CreativeInventory.IsHovering))
            {
                PlayerInput.ScrollWheelDelta = 0;
            }
        }
    }
}
