using HarmonyLib;
using OptimizeAndTool.Content.Storage.ItemContainer;
using Terraria;
using TPML.Core.Diagnostics;

namespace OptimizeAndTool.Content.BigBag
{
    /// <summary>
    /// 拾取物品时自动堆叠与满包自动溢出入巨大背包钩子
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.GetItem), typeof(Item), typeof(GetItemSettings))]
    internal static class Patch_BigBagPickup
    {
        [HarmonyPrefix]
        public static bool GetItemPrefix(Player __instance, Item newItem, GetItemSettings settings, ref Item __result)
        {
            if (__instance == null || __instance.whoAmI != Main.myPlayer) return true;
            if (ItemContainerItem.IsTransferringOut) return true;
            if (newItem == null || newItem.IsAir || newItem.type <= 0) return true;
            if (!BigBag.EnableBigBag.val) return true;

            // 1. 若开启「拾取自动堆叠」：大背包已有同类物品优先堆入（钱币除外，钱币优先走原生专用槽）
            if (BigBag.AutoStackOnPickup.val && !newItem.IsACoin)
            {
                using (PerformanceProfiler.Measure("OptimizeAndTool", "BigBag.AutoStackPickup"))
                {
                    bool fullyStacked = BigBag.TryAutoStackPickup(newItem);
                    if (fullyStacked)
                    {
                        __result = new Item();
                        return false;
                    }
                }
            }

            return true;
        }

        [HarmonyPostfix]
        public static void GetItemPostfix(Player __instance, Item newItem, GetItemSettings settings, ref Item __result)
        {
            if (__instance == null || __instance.whoAmI != Main.myPlayer) return;
            if (ItemContainerItem.IsTransferringOut) return;
            if (__result == null || __result.IsAir || __result.type <= 0 || __result.stack <= 0) return;
            if (!BigBag.EnableBigBag.val || !BigBag.PickupOverflowToBigBag.val) return;

            // 2. 原版 GetItem 执行完毕后，若仍有未装入本体背包的剩余物品，且开启了「满包拾取溢出」：
            // 尝试将剩余物品溢出存入巨大背包（包含钱币/弹药等全品类）
            using (PerformanceProfiler.Measure("OptimizeAndTool", "BigBag.OverflowPickup"))
            {
                bool fullyPlaced = BigBag.TryOverflowPickup(__result);
                if (fullyPlaced)
                {
                    __result = new Item();
                }
            }
        }
    }

    /// <summary>
    /// 掉落物吸附与背包空间判定钩子：本体背包装满时若巨大背包有空位则允许吸附
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.ItemSpace), typeof(Item))]
    internal static class Patch_BigBagItemSpace
    {
        [HarmonyPostfix]
        public static void ItemSpacePostfix(Player __instance, Item newItem, ref Player.ItemSpaceStatus __result)
        {
            if (__instance == null || __instance.whoAmI != Main.myPlayer) return;
            if (__result.CanTakeItem) return;
            if (newItem == null || newItem.IsAir || newItem.type <= 0) return;

            if (BigBag.CanBigBagAccept(newItem))
            {
                __result = new Player.ItemSpaceStatus(CanTakeItem: true);
            }
        }
    }

    /// <summary>
    /// 原版虚空袋 (Void Vault) 拾取前置拦截：保证大背包优先级高于虚空袋
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.GetItem_VoidVault))]
    internal static class Patch_BigBagVoidVault
    {
        [HarmonyPrefix]
        public static bool GetItem_VoidVaultPrefix(Player __instance, Item[] inventory, Item newItem, GetItemSettings settings, Item returnItem, ref bool __result)
        {
            if (__instance == null || __instance.whoAmI != Main.myPlayer) return true;
            if (returnItem == null || returnItem.IsAir || returnItem.type <= 0 || returnItem.stack <= 0) return true;
            if (!BigBag.EnableBigBag.val || !BigBag.PickupOverflowToBigBag.val) return true;

            // 虚空袋触发前，优先溢出存入巨大背包
            bool fullyPlaced = BigBag.TryOverflowPickup(returnItem);
            if (fullyPlaced)
            {
                __result = true;
                return false; // 大背包已完全吸收，跳过虚空袋
            }

            // 大背包未完全吸收（如大背包已满或部分放入），继续交由虚空袋处理剩余物品
            return true;
        }
    }
}
