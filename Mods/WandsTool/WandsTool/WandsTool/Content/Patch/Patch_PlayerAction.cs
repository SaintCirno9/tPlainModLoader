using HarmonyLib;
using Terraria;

namespace WandsTool.Content.Patch
{
    /// <summary>
    /// 拦截魔棒模式下的原版手持物动作与世界交互，防止手持物品动作和魔棒选区动作同时触发
    /// </summary>
    [HarmonyPatch(typeof(Player))]
    internal class Patch_PlayerAction
    {
        /// <summary>
        /// 拦截原版 ItemCheck（物块放置、工具挖掘、武器挥舞/射击、药水使用等）
        /// </summary>
        [HarmonyPatch(nameof(Player.ItemCheck))]
        [HarmonyPrefix]
        public static bool ItemCheck_Prefix(Player __instance)
        {
            if (__instance.whoAmI == Main.myPlayer && gameMain.Wand_isEnable)
            {
                // 将动作状态强制归零，防止手臂僵直、误挥或引导型武器（如终极棱镜）持续消耗
                __instance.itemAnimation = 0;
                __instance.itemTime = 0;
                __instance.reuseDelay = 0;
                __instance.channel = false;

                // 阻断原版 ItemCheck 执行
                return false;
            }

            return true;
        }

        /// <summary>
        /// 拦截近距离右键世界交互（开箱、开门、开关、标牌等），使右键仅响应魔棒的取消选区与设置轮盘
        /// </summary>
        [HarmonyPatch(nameof(Player.TileInteractionsCheck), typeof(int), typeof(int))]
        [HarmonyPrefix]
        public static bool TileInteractionsCheck_Prefix(Player __instance)
        {
            if (__instance.whoAmI == Main.myPlayer && gameMain.Wand_isEnable)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 拦截远距离智能右键交互
        /// </summary>
        [HarmonyPatch("TileInteractionsCheckLongDistance", typeof(int), typeof(int))]
        [HarmonyPrefix]
        public static bool TileInteractionsCheckLongDistance_Prefix(Player __instance)
        {
            if (__instance.whoAmI == Main.myPlayer && gameMain.Wand_isEnable)
            {
                return false;
            }

            return true;
        }
    }
}
