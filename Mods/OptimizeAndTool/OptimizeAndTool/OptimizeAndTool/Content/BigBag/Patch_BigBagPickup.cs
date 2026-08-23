using HarmonyLib;
using Terraria;

namespace OptimizeAndTool.Content.BigBag
{
    /// <summary>
    /// 拾取物品时自动堆叠入巨大背包钩子
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.GetItem), typeof(Item), typeof(GetItemSettings))]
    internal static class Patch_BigBagPickup
    {
        [HarmonyPrefix]
        public static bool GetItemPrefix(Player __instance, Item newItem, GetItemSettings settings, ref Item __result)
        {
            if (__instance == null || __instance.whoAmI != Main.myPlayer) return true;
            if (newItem == null || newItem.IsAir || newItem.type <= 0) return true;
            if (!BigBag.EnableBigBag.val || !BigBag.AutoStackOnPickup.val) return true;

            // 钱币优先走玩家原生钱币槽
            if (newItem.IsACoin) return true;

            bool fullyStacked = BigBag.TryAutoStackPickup(newItem);
            if (fullyStacked)
            {
                __result = new Item();
                return false;
            }

            return true;
        }
    }
}
