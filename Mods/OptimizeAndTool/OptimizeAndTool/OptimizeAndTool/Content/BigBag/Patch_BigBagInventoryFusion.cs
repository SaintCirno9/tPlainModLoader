using HarmonyLib;
using System;
using Terraria;

namespace OptimizeAndTool.Content.BigBag
{
    /// <summary>
    /// 大背包透明接入原版背包系统：<br/>
    /// 拦截 HasItem、CountItem、ConsumeItem、CanAfford 等核心查询与消耗方法，<br/>
    /// 使任何调用原版背包检测的系统（任务检测、NPC交互、钥匙开锁、工具判定、消费结算等）自然识别大背包中的物品。
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch]
    internal static class Patch_BigBagInventoryFusion
    {
        private static bool ShouldFusion(Player player)
        {
            if (!BigBag.EnableBigBag.val) return false;
            if (player == null || !player.active || player.whoAmI != Main.myPlayer) return false;
            return BigBag.Slots != null && BigBag.Slots.Length > 0;
        }

        #region 1. HasItem 系列查询拦截

        [HarmonyPatch(typeof(Player), nameof(Player.HasItem), typeof(int))]
        [HarmonyPostfix]
        private static void HasItemPostfix(Player __instance, int type, ref bool __result)
        {
            if (__result) return;
            if (!ShouldFusion(__instance)) return;

            Item[] slots = BigBag.Slots;
            for (int i = 0; i < slots.Length; i++)
            {
                Item it = slots[i];
                if (it != null && !it.IsAir && it.type == type && it.stack > 0)
                {
                    __result = true;
                    return;
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.HasItemInInventoryOrOpenVoidBag))]
        [HarmonyPostfix]
        private static void HasItemInInventoryOrOpenVoidBagPostfix(Player __instance, int type, ref bool __result)
        {
            if (__result) return;
            if (!ShouldFusion(__instance)) return;

            Item[] slots = BigBag.Slots;
            for (int i = 0; i < slots.Length; i++)
            {
                Item it = slots[i];
                if (it != null && !it.IsAir && it.type == type && it.stack > 0)
                {
                    __result = true;
                    return;
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.HasItemInAnyInventory))]
        [HarmonyPostfix]
        private static void HasItemInAnyInventoryPostfix(Player __instance, int type, ref bool __result)
        {
            if (__result) return;
            if (!ShouldFusion(__instance)) return;

            Item[] slots = BigBag.Slots;
            for (int i = 0; i < slots.Length; i++)
            {
                Item it = slots[i];
                if (it != null && !it.IsAir && it.type == type && it.stack > 0)
                {
                    __result = true;
                    return;
                }
            }
        }

        #endregion

        #region 2. CountItem 数量统计拦截

        [HarmonyPatch(typeof(Player), nameof(Player.CountItem), typeof(int), typeof(int))]
        [HarmonyPostfix]
        private static void CountItemPostfix(Player __instance, int type, int stopCountingAt, ref int __result)
        {
            if (!ShouldFusion(__instance)) return;
            if (stopCountingAt > 0 && __result >= stopCountingAt) return;

            Item[] slots = BigBag.Slots;
            for (int i = 0; i < slots.Length; i++)
            {
                Item it = slots[i];
                if (it != null && !it.IsAir && it.type == type && it.stack > 0)
                {
                    __result += it.stack;
                    if (stopCountingAt > 0 && __result >= stopCountingAt)
                    {
                        return;
                    }
                }
            }
        }

        #endregion

        #region 3. ConsumeItem 自动消耗扣除拦截

        [HarmonyPatch(typeof(Player), nameof(Player.ConsumeItem))]
        [HarmonyPostfix]
        private static void ConsumeItemPostfix(Player __instance, int type, bool reverseOrder, bool includeVoidBag, ref bool __result)
        {
            if (__result) return;
            if (!ShouldFusion(__instance)) return;

            Item[] slots = BigBag.Slots;
            int start = reverseOrder ? slots.Length - 1 : 0;
            int end = reverseOrder ? -1 : slots.Length;
            int step = reverseOrder ? -1 : 1;

            for (int i = start; i != end; i += step)
            {
                Item it = slots[i];
                if (it != null && !it.IsAir && it.type == type && it.stack > 0)
                {
                    it.stack--;
                    if (it.stack <= 0)
                    {
                        slots[i] = new Item();
                    }

                    BigBagStorage.SaveNow();
                    BigBag.NotifySlotsChanged();
                    __result = true;
                    return;
                }
            }
        }

        #endregion
    }
}
