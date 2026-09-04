using CommandHelp;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 无限消耗规则门控（对齐 ImproveGame 语义，基于 HookGen 强类型 On_ 门控）：
    /// 1. 召唤物不消耗（BOSS 召唤物与事件召唤物，仅原版）；
    /// 2. 无限弹药：堆叠 ≥ 3996 的弹药不消耗；
    /// 3. 无限投掷物：堆叠 ≥ 3996 的投掷物不消耗；
    /// 4. 无限电线：堆叠 ≥ 3996 的电线放置不消耗。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class NoConsumeItemHooks
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

        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_Player.CanConsumeConsumableItem += Hook_CanConsumeConsumableItem;
            On_Player.ItemCheck_UseWiringTools += Hook_ItemCheck_UseWiringTools;
            On_Player.ConsumeItem += Hook_ConsumeItem;
            On_Player.PickAmmo += Hook_PickAmmo;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_Player.CanConsumeConsumableItem -= Hook_CanConsumeConsumableItem;
            On_Player.ItemCheck_UseWiringTools -= Hook_ItemCheck_UseWiringTools;
            On_Player.ConsumeItem -= Hook_ConsumeItem;
            On_Player.PickAmmo -= Hook_PickAmmo;
            _registered = false;
        }

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

        private static bool Hook_CanConsumeConsumableItem(On_Player.orig_CanConsumeConsumableItem orig, Player self, Item item)
        {
            if (IsNoConsumeTarget(item))
            {
                return false;
            }

            return orig(self, item);
        }

        private static void Hook_ItemCheck_UseWiringTools(On_Player.orig_ItemCheck_UseWiringTools orig, Player self, Item sItem)
        {
            int preWireStack = -1;
            if (NoConsumeWire.val)
            {
                preWireStack = CountItemTotal(self, ItemID.Wire);
            }

            try
            {
                orig(self, sItem);
            }
            finally
            {
                if (NoConsumeWire.val && preWireStack >= 0)
                {
                    int now = CountItemTotal(self, ItemID.Wire);
                    if (now < preWireStack && preWireStack >= NoConsumeThreshold)
                    {
                        int diff = preWireStack - now;
                        for (int i = 0; i < 58 && diff > 0; i++)
                        {
                            if (self.inventory[i] != null && self.inventory[i].type == ItemID.Wire)
                            {
                                int add = System.Math.Min(diff, self.inventory[i].maxStack - self.inventory[i].stack);
                                if (add > 0)
                                {
                                    self.inventory[i].stack += add;
                                    diff -= add;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool Hook_ConsumeItem(On_Player.orig_ConsumeItem orig, Player self, int type, bool reverseOrder, bool includeVoidBag)
        {
            if (NoConsumeWire.val && (type == ItemID.Wire || type == 849) && CountItemTotal(self, type) >= NoConsumeThreshold)
            {
                return true;
            }

            return orig(self, type, reverseOrder, includeVoidBag);
        }

        private static void Hook_PickAmmo(On_Player.orig_PickAmmo orig, Player self, Item sItem, ref int projToShoot, ref float speed, ref bool canShoot, ref int Damage, ref float KnockBack, out int usedAmmoItemId, bool dontConsume)
        {
            int[] snapshot = null;
            if (NoConsumeAmmo.val)
            {
                snapshot = new int[58];
                for (int i = 0; i < 58; i++)
                {
                    snapshot[i] = self.inventory[i]?.stack ?? 0;
                }
            }

            try
            {
                orig(self, sItem, ref projToShoot, ref speed, ref canShoot, ref Damage, ref KnockBack, out usedAmmoItemId, dontConsume);
            }
            finally
            {
                if (NoConsumeAmmo.val && snapshot != null)
                {
                    for (int i = 0; i < 58; i++)
                    {
                        Item item = self.inventory[i];
                        if (item != null && item.stack < snapshot[i] && snapshot[i] >= NoConsumeThreshold)
                        {
                            item.stack = snapshot[i];
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    internal static class NoConsumeItems
    {
        public const int NoConsumeThreshold = NoConsumeItemHooks.NoConsumeThreshold;
        public static HashSet<int> BossSummonItems => NoConsumeItemHooks.BossSummonItems;
        public static HashSet<int> EventSummonItems => NoConsumeItemHooks.EventSummonItems;

        public static GetSetReset<bool> NoConsumeSummonItem => NoConsumeItemHooks.NoConsumeSummonItem;
        public static GetSetReset<bool> NoConsumeAmmo => NoConsumeItemHooks.NoConsumeAmmo;
        public static GetSetReset<bool> NoConsumeProjectile => NoConsumeItemHooks.NoConsumeProjectile;
        public static GetSetReset<bool> NoConsumeWire => NoConsumeItemHooks.NoConsumeWire;

        public static List<CommandObject> GetCO() => NoConsumeItemHooks.GetCO();
        public static List<UIElement> GetUI() => NoConsumeItemHooks.GetUI();
    }
}
