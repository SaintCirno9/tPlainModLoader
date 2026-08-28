using CommandHelp;
using HarmonyLib;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 无限消耗规则（对齐 ImproveGame 语义）：
    /// 1. 召唤物不消耗（BOSS 召唤物与事件召唤物，仅原版）；
    /// 2. 无限弹药：堆叠 ≥ 3996 的弹药不消耗；
    /// 3. 无限投掷物：堆叠 ≥ 3996 的投掷物不消耗；
    /// 4. 无限电线：堆叠 ≥ 3996 的电线放置不消耗。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class NoConsumeItems
    {
        /// <summary>无限消耗判定阈值（ImproveGame 同款：4 × 999 满堆叠）</summary>
        public const int NoConsumeThreshold = 3996;

        /// <summary>BOSS 召唤物物品 ID（对齐 ImproveGame Lookups.BossSummonItems，仅原版物品）</summary>
        public static readonly HashSet<int> BossSummonItems = new HashSet<int> { 560, 43, 70, 1331, 1133, 5120, 4988, 544, 556, 557, 1293, 3601, 5334, 4961 };

        /// <summary>事件召唤物物品 ID（对齐 ImproveGame Lookups.EventSummonItems，仅原版物品）</summary>
        public static readonly HashSet<int> EventSummonItems = new HashSet<int> { 4271, 361, 3828, 602, 2767, 1315, 1844, 1958 };

        public static GetSetReset<bool> NoConsumeSummonItem = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> NoConsumeAmmo = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> NoConsumeProjectile = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> NoConsumeWire = new GetSetReset<bool>(false, false);

        /// <summary>是否为不消耗目标物品（走 Player.CanConsumeConsumableItem 通用消耗路径的）</summary>
        internal static bool IsNoConsumeTarget(Item item)
        {
            if (item == null || item.type <= 0) return false;

            if (NoConsumeSummonItem.val && (BossSummonItems.Contains(item.type) || EventSummonItems.Contains(item.type)))
            {
                return true;
            }
            if (NoConsumeProjectile.val && item.stack >= NoConsumeThreshold && item.shoot > 0 && item.ammo == 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>统计背包内指定物品总堆叠</summary>
        internal static int CountItemTotal(Player player, int type)
        {
            int total = 0;
            for (int i = 0; i < 58; i++)
            {
                if (player.inventory[i] != null && player.inventory[i].type == type)
                {
                    total += player.inventory[i].stack;
                }
            }
            return total;
        }

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("noConsumeSummonItem", NoConsumeSummonItem),
                CommandBuild.get2("noConsumeAmmo", NoConsumeAmmo),
                CommandBuild.get2("noConsumeProjectile", NoConsumeProjectile),
                CommandBuild.get2("noConsumeWire", NoConsumeWire)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(NoConsumeSummonItem, "使用 BOSS 召唤物与事件召唤物时不消耗物品（仅支持原版物品）", "Images/Item_560", "召唤物不消耗"),
                UIBuild.get2(NoConsumeAmmo, "堆叠数 ≥ 3996 的弹药射击时不消耗", "Images/Item_3104", "无限弹药"),
                UIBuild.get2(NoConsumeProjectile, "堆叠数 ≥ 3996 的投掷物投掷时不消耗", "Images/Item_42", "无限投掷物"),
                UIBuild.get2(NoConsumeWire, "堆叠数 ≥ 3996 的电线/作动器（批量布线）放置时不消耗", "Images/Item_530", "无限电线")
            };
        }
    }

    /// <summary>
    /// 召唤物/投掷物不消耗：拦截原版消耗豁免钩子 Player.CanConsumeConsumableItem
    /// （Player.cs:5488，恒返回 true，被 ItemCheck 通用扣减点 43636 / QuickHeal 5527 / 5611 调用），
    /// 对匹配目标返回 false 即跳过扣减。等价于 tML 的 GlobalItem.ConsumeItem 钩子。
    /// 注意：电线 530 非 consumable，不走此路径（由 Patch_NoConsumeWire 处理）。
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.CanConsumeConsumableItem))]
    internal static class Patch_NoConsumeConsumableItem
    {
        [HarmonyPrefix]
        internal static bool Prefix(Player __instance, Item item, ref bool __result)
        {
            if (!NoConsumeItems.IsNoConsumeTarget(item)) return true;
            __result = false;
            return false;
        }
    }

    /// <summary>
    /// 无限电线：电线 530 非 consumable，单格放置走 ItemCheck_UseWiringTools 的直扣
    /// （Player.cs:47184 等 inventory[..].stack--），批量布线走 Player.ConsumeItem(type)。
    /// 单格：Prefix 快照 530 总堆叠，Postfix 把扣减差值加回；批量：Prefix 拦截 ConsumeItem 直接放行。
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.ItemCheck_UseWiringTools))]
    internal static class Patch_NoConsumeWire_Single
    {
        private static int preWireStack = -1;

        [HarmonyPrefix]
        internal static void Prefix(Player __instance)
        {
            if (!NoConsumeItems.NoConsumeWire.val) return;
            preWireStack = NoConsumeItems.CountItemTotal(__instance, ItemID.Wire);
        }

        [HarmonyFinalizer]
        internal static void Finalizer(Player __instance)
        {
            if (!NoConsumeItems.NoConsumeWire.val || preWireStack < 0) return;
            int now = NoConsumeItems.CountItemTotal(__instance, ItemID.Wire);
            if (now < preWireStack && preWireStack >= NoConsumeItems.NoConsumeThreshold)
            {
                int diff = preWireStack - now;
                for (int i = 0; i < 58 && diff > 0; i++)
                {
                    if (__instance.inventory[i] != null && __instance.inventory[i].type == ItemID.Wire)
                    {
                        int add = System.Math.Min(diff, __instance.inventory[i].maxStack - __instance.inventory[i].stack);
                        if (add > 0)
                        {
                            __instance.inventory[i].stack += add;
                            diff -= add;
                        }
                    }
                }
            }
            preWireStack = -1;
        }
    }

    /// <summary>批量布线（五彩扳手拖拽）经 Player.ConsumeItem(530/849) 扣减，Prefix 拦截放行</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.ConsumeItem))]
    internal static class Patch_NoConsumeWire_Batch
    {
        [HarmonyPrefix]
        internal static bool Prefix(Player __instance, int type, ref bool __result)
        {
            if (!NoConsumeItems.NoConsumeWire.val) return true;
            if (type != ItemID.Wire && type != 849) return true;
            if (NoConsumeItems.CountItemTotal(__instance, type) < NoConsumeItems.NoConsumeThreshold) return true;
            __result = true;
            return false;
        }
    }

    /// <summary>
    /// 无限弹药：Player.PickAmmo 末尾统一扣减弹药（Player.cs:54319 的 item.stack--）。
    /// Prefix 快照全部背包格堆叠，Postfix 对被扣减且原堆叠 ≥ 阈值的格恢复，不动原版选择顺序。
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.PickAmmo))]
    internal static class Patch_InfiniteAmmo
    {
        private static int[] snapshot = null;

        [HarmonyPrefix]
        internal static void Prefix(Player __instance)
        {
            if (!NoConsumeItems.NoConsumeAmmo.val) return;
            snapshot = new int[58];
            for (int i = 0; i < 58; i++)
            {
                snapshot[i] = __instance.inventory[i]?.stack ?? 0;
            }
        }

        [HarmonyFinalizer]
        internal static void Finalizer(Player __instance)
        {
            if (!NoConsumeItems.NoConsumeAmmo.val || snapshot == null) return;
            for (int i = 0; i < 58; i++)
            {
                Item item = __instance.inventory[i];
                if (item != null && item.stack < snapshot[i] && snapshot[i] >= NoConsumeItems.NoConsumeThreshold)
                {
                    item.stack = snapshot[i];
                }
            }
            snapshot = null;
        }
    }
}
