using Microsoft.Xna.Framework;
using OptimizeAndTool.Content.Storage.ItemContainer;
using Terraria;
using Terraria.ID;
using TPML.Core.Diagnostics;

namespace OptimizeAndTool.Content.BigBag
{
    /// <summary>
    /// 拾取物品时自动堆叠与满包自动溢出入巨大背包钩子（基于 HookGen 强类型 On_ 门控）
    /// 作者: SaintCirno9
    /// </summary>
    internal static class BigBagPickupHooks
    {
        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_Player.GetItem += Hook_GetItem;
            On_Player.ItemSpace_Item += Hook_ItemSpace;
            On_Player.GetItem_VoidVault += Hook_GetItem_VoidVault;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_Player.GetItem -= Hook_GetItem;
            On_Player.ItemSpace_Item -= Hook_ItemSpace;
            On_Player.GetItem_VoidVault -= Hook_GetItem_VoidVault;
            _registered = false;
        }

        private static Item Hook_GetItem(On_Player.orig_GetItem orig, Player self, Item newItem, GetItemSettings settings)
        {
            if (self == null || self.whoAmI != Main.myPlayer || ItemContainerItem.IsTransferringOut ||
                newItem == null || newItem.IsAir || newItem.type <= 0 || !BigBag.EnableBigBag.val ||
                settings.NoText)
            {
                return orig(self, newItem, settings);
            }

            // 0. 拾取即变现（就地折现）：若开启自动售卖且为符合条件的带前缀装备/工具，且全量持有数 >= 保留阈值
            if (BigBag.CurrentAutoSellPrefixed && BigBag.IsSellablePrefixedItem(newItem) &&
                BigBag.CountTotalItemCopies(self, newItem.type, newItem) >= BigBag.CurrentKeepCopiesThreshold)
            {
                Item itemInfo = newItem.Clone();
                int originalStack = newItem.stack;
                if (BigBag.SellSingleItem(self, newItem, out long earned))
                {
                    newItem.TurnToAir();
                    Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Coins);
                    Vector2 pos = self.Center;
                    PopupText.NewText(PopupTextContext.RegularItemPickup, itemInfo, pos, originalStack, false, false);
                    if (earned > 0)
                    {
                        BigBag.PopupCoins(pos, earned);
                    }
                    return new Item();
                }
            }

            // 1. 若开启「拾取自动堆叠」：大背包已有同类物品优先堆入（钱币除外，钱币优先走原生专用槽）
            if (BigBag.AutoStackOnPickup.val && !newItem.IsACoin)
            {
                using (PerformanceProfiler.Measure("OptimizeAndTool", "BigBag.AutoStackPickup"))
                {
                    bool fullyStacked = BigBag.TryAutoStackPickup(newItem);
                    if (fullyStacked)
                    {
                        return new Item();
                    }
                }
            }

            // 2. 原版 GetItem 执行
            Item result = orig(self, newItem, settings);

            // 3. 原版 GetItem 执行完毕后，若仍有未装入本体背包的剩余物品，且开启了「满包拾取溢出」：
            // 尝试将剩余物品溢出存入巨大背包（包含钱币/弹药等全品类）
            if (result != null && !result.IsAir && result.type > 0 && result.stack > 0 && BigBag.PickupOverflowToBigBag.val)
            {
                using (PerformanceProfiler.Measure("OptimizeAndTool", "BigBag.OverflowPickup"))
                {
                    bool fullyPlaced = BigBag.TryOverflowPickup(result);
                    if (fullyPlaced)
                    {
                        return new Item();
                    }
                }
            }

            return result;
        }

        private static Player.ItemSpaceStatus Hook_ItemSpace(On_Player.orig_ItemSpace_Item orig, Player self, Item newItem)
        {
            Player.ItemSpaceStatus status = orig(self, newItem);
            if (self == null || self.whoAmI != Main.myPlayer || status.CanTakeItem ||
                newItem == null || newItem.IsAir || newItem.type <= 0)
            {
                return status;
            }

            if (BigBag.CanBigBagAccept(newItem))
            {
                return new Player.ItemSpaceStatus(CanTakeItem: true);
            }

            return status;
        }

        private static bool Hook_GetItem_VoidVault(On_Player.orig_GetItem_VoidVault orig, Player self, Item[] inventory, Item newItem, GetItemSettings settings, Item returnItem)
        {
            if (self == null || self.whoAmI != Main.myPlayer ||
                returnItem == null || returnItem.IsAir || returnItem.type <= 0 || returnItem.stack <= 0 ||
                !BigBag.EnableBigBag.val || !BigBag.PickupOverflowToBigBag.val)
            {
                return orig(self, inventory, newItem, settings, returnItem);
            }

            // 虚空袋触发前，优先溢出存入巨大背包
            bool fullyPlaced = BigBag.TryOverflowPickup(returnItem);
            if (fullyPlaced)
            {
                return true; // 大背包已完全吸收，跳过虚空袋
            }

            // 大背包未完全吸收，继续交由虚空袋处理剩余物品
            return orig(self, inventory, newItem, settings, returnItem);
        }
    }
}
