using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using TPML.Core.Logging;

namespace TPML.Content.Fusion
{
    /// <summary>
    /// TPML 框架级全量背包融合底层 Hook 门控矩阵（基于 HookGen 强类型 On_ 门控，零反射，100% 对齐规范）：<br/>
    /// 1. 统一拦截 HasItem、CountItem、ConsumeItem 等标准查询与消耗方法，调度已注册的 <see cref="IFusionItemSource"/>；<br/>
    /// 2. 统一拦截经典魔杖（tileWand）、万能魔杖（FlexibleTileWand）、油漆涂料（FindPaintOrCoating）等原版硬编码背包遍历的特殊系统；<br/>
    /// 3. 统一拦截原版制作系统（Recipe._ownedItems 统计、CraftingRequests.CanCraftLocally 与 Consume 扣料），使外部融合容器直接参与合成与材料查找。
    /// 作者: SaintCirno9
    /// </summary>
    public static class UnifiedInventoryFusionHooks
    {
        private static readonly ILogger Logger = LogManager.GetLogger("Fusion");
        private static bool _registered = false;

        /// <summary>集中注册全部融合补丁（由 ContentHookDispatcher.ApplyOnDemandPatches 调用）</summary>
        public static void RegisterAll()
        {
            if (_registered) return;

            // 1. HasItem 系列
            On_Player.HasItem_int += Hook_HasItem_int;
            On_Player.HasItemInInventoryOrOpenVoidBag += Hook_HasItemInInventoryOrOpenVoidBag;
            On_Player.HasItemInAnyInventory += Hook_HasItemInAnyInventory;

            // 2. CountItem 数量统计
            On_Player.CountItem += Hook_CountItem;

            // 3. ConsumeItem 自动消耗扣除
            On_Player.ConsumeItem += Hook_ConsumeItem;

            // 4. 经典魔杖与挥动可用性
            On_Player.PlaceThing_Tiles_CheckWandUsability += Hook_PlaceThing_Tiles_CheckWandUsability;
            On_Player.ItemCheck_CheckCanUse_Inner += Hook_ItemCheck_CheckCanUse_Inner;
            On_Player.ItemCheck += Hook_ItemCheck;

            // 5. 万能魔杖
            On_FlexibleTileWand.TryGetPlacementOption += Hook_TryGetPlacementOption;
            On_Player.PlaceThing_Tiles_PlaceIt_ConsumeFlexibleWandMaterial += Hook_PlaceThing_Tiles_PlaceIt_ConsumeFlexibleWandMaterial;

            // 6. 油漆与涂料
            On_Player.FindPaintOrCoating += Hook_FindPaintOrCoating;
            On_Player.ApplyPaint += Hook_ApplyPaint;
            On_Player.ApplyCoating += Hook_ApplyCoating;

            // 7. 原版制作系统
            On_Recipe.CollectItemsToCraftWithFrom += Hook_CollectItemsToCraftWithFrom;
            On_CraftingRequests.CanCraftLocally += Hook_CanCraftLocally;
            On_CraftingRequests.Consume += Hook_Consume;
            On_CraftingRequests.CraftItem += Hook_CraftItem;

            _registered = true;
            Logger.Info("★ 背包融合系统 (Fusion) 强类型 On_ 门控已全部就绪");
        }

        /// <summary>集中反注册全部融合补丁（由 ContentHookDispatcher.Clear 调用）</summary>
        public static void UnregisterAll()
        {
            if (!_registered) return;

            On_Player.HasItem_int -= Hook_HasItem_int;
            On_Player.HasItemInInventoryOrOpenVoidBag -= Hook_HasItemInInventoryOrOpenVoidBag;
            On_Player.HasItemInAnyInventory -= Hook_HasItemInAnyInventory;

            On_Player.CountItem -= Hook_CountItem;
            On_Player.ConsumeItem -= Hook_ConsumeItem;

            On_Player.PlaceThing_Tiles_CheckWandUsability -= Hook_PlaceThing_Tiles_CheckWandUsability;
            On_Player.ItemCheck_CheckCanUse_Inner -= Hook_ItemCheck_CheckCanUse_Inner;
            On_Player.ItemCheck -= Hook_ItemCheck;

            On_FlexibleTileWand.TryGetPlacementOption -= Hook_TryGetPlacementOption;
            On_Player.PlaceThing_Tiles_PlaceIt_ConsumeFlexibleWandMaterial -= Hook_PlaceThing_Tiles_PlaceIt_ConsumeFlexibleWandMaterial;

            On_Player.FindPaintOrCoating -= Hook_FindPaintOrCoating;
            On_Player.ApplyPaint -= Hook_ApplyPaint;
            On_Player.ApplyCoating -= Hook_ApplyCoating;

            On_Recipe.CollectItemsToCraftWithFrom -= Hook_CollectItemsToCraftWithFrom;
            On_CraftingRequests.CanCraftLocally -= Hook_CanCraftLocally;
            On_CraftingRequests.Consume -= Hook_Consume;
            On_CraftingRequests.CraftItem -= Hook_CraftItem;

            _registered = false;
        }

        private static bool ShouldFusion(Player player)
        {
            if (player == null || !player.active || player.whoAmI != Main.myPlayer) return false;
            return true;
        }

        #region 1. HasItem 系列查询拦截

        private static bool Hook_HasItem_int(On_Player.orig_HasItem_int orig, Player self, int type)
        {
            bool result = orig(self, type);
            if (result || !ShouldFusion(self)) return result;

            return InventoryFusionManager.HasItem(self, type);
        }

        private static bool Hook_HasItemInInventoryOrOpenVoidBag(On_Player.orig_HasItemInInventoryOrOpenVoidBag orig, Player self, int type)
        {
            bool result = orig(self, type);
            if (result || !ShouldFusion(self)) return result;

            return InventoryFusionManager.HasItem(self, type);
        }

        private static bool Hook_HasItemInAnyInventory(On_Player.orig_HasItemInAnyInventory orig, Player self, int type)
        {
            bool result = orig(self, type);
            if (result || !ShouldFusion(self)) return result;

            return InventoryFusionManager.HasItem(self, type);
        }

        #endregion

        #region 2. CountItem 数量统计拦截

        private static int Hook_CountItem(On_Player.orig_CountItem orig, Player self, int type, int stopCountingAt)
        {
            int result = orig(self, type, stopCountingAt);
            if (!ShouldFusion(self)) return result;
            if (stopCountingAt > 0 && result >= stopCountingAt) return result;

            int externalCount = InventoryFusionManager.CountItem(self, type, stopCountingAt > 0 ? stopCountingAt - result : 0);
            return result + externalCount;
        }

        #endregion

        #region 3. ConsumeItem 自动消耗扣除拦截

        private static bool Hook_ConsumeItem(On_Player.orig_ConsumeItem orig, Player self, int type, bool reverseOrder, bool includeVoidBag)
        {
            bool result = orig(self, type, reverseOrder, includeVoidBag);
            if (result || !ShouldFusion(self)) return result;

            return InventoryFusionManager.ConsumeItem(self, type, reverseOrder);
        }

        #endregion

        #region 4. 经典魔杖（tileWand）与漆刷等原版硬编码放置/使用/消耗支持

        /// <summary>
        /// 拦截物块放置时的经典魔杖（生命木魔棒、树叶魔棒、骨头魔棒等）可用性判定
        /// </summary>
        private static bool Hook_PlaceThing_Tiles_CheckWandUsability(On_Player.orig_PlaceThing_Tiles_CheckWandUsability orig, Player self, bool canUse)
        {
            bool result = orig(self, canUse);
            if (result || !ShouldFusion(self)) return result;

            Item held = self.inventory[self.selectedItem];
            if (held != null && held.tileWand > 0 && InventoryFusionManager.HasItem(self, held.tileWand))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 拦截挥动检测中的经典魔杖与漆刷/涂料刷可用性判定
        /// </summary>
        private static bool Hook_ItemCheck_CheckCanUse_Inner(On_Player.orig_ItemCheck_CheckCanUse_Inner orig, Player self, Item sItem, bool ignoreCursed)
        {
            bool result = orig(self, sItem, ignoreCursed);
            if (result || !ShouldFusion(self) || sItem == null) return result;

            // 经典魔杖判定（生命木魔棒等）
            if (sItem.tileWand > 0 && InventoryFusionManager.HasItem(self, sItem.tileWand))
            {
                return true;
            }

            // 漆刷 / 滚筒 / 涂料刷判定 (1071=漆刷, 1072=滚筒, 1543=漆铲, 1544=涂料刷)
            if ((sItem.type == 1071 || sItem.type == 1072 || sItem.type == 1543 || sItem.type == 1544) &&
                InventoryFusionManager.HasMatchingItem(self, i => i.PaintOrCoating))
            {
                return true;
            }

            return false;
        }

        private static int CountInventoryItem(Player player, int type)
        {
            int count = 0;
            if (player?.inventory == null) return 0;
            int n = Math.Min(58, player.inventory.Length);
            for (int i = 0; i < n; i++)
            {
                Item it = player.inventory[i];
                if (it != null && it.type == type && it.stack > 0)
                {
                    count += it.stack;
                }
            }
            return count;
        }

        /// <summary>
        /// 拦截魔杖挥动放置物块时的消耗逻辑：<br/>
        /// 原版在 ItemCheck 内部硬编码扣除 inventory[0..57]，若背包中无材料而外部融合源中有，在此扣除外部源中的 1 个材料。
        /// </summary>
        private static void Hook_ItemCheck(On_Player.orig_ItemCheck orig, Player self)
        {
            if (!ShouldFusion(self))
            {
                orig(self);
                return;
            }

            Item item = self.inventory[self.selectedItem];
            bool shouldCheckWand = false;
            int wandType = 0;
            int inventoryCountBefore = 0;

            if (item != null && item.tileWand > 0 && !self.dontConsumeWand && self.itemTimeMax != 0 && self.itemTime == self.itemTimeMax)
            {
                wandType = item.tileWand;
                inventoryCountBefore = CountInventoryItem(self, wandType);
                shouldCheckWand = true;
            }

            orig(self);

            // 原版已从背包扣料则不再扣外部源，避免「背包最后 1 个被原版消耗后又从融合源多扣 1」
            if (shouldCheckWand && !self.dontConsumeWand && CountInventoryItem(self, wandType) >= inventoryCountBefore)
            {
                InventoryFusionManager.ConsumeItem(self, wandType);
            }
        }

        #endregion

        #region 5. 万能魔杖（FlexibleTileWand）外部源支持

        /// <summary>
        /// 拦截 1.4.4 万能魔杖（碎石放置器 Rubble Maker、便携式熔炉等）的放置选项与材料匹配
        /// </summary>
        private static bool Hook_TryGetPlacementOption(On_FlexibleTileWand.orig_TryGetPlacementOption orig, FlexibleTileWand self, Player player, int randomSeed, int selectCycleOffset, out FlexibleTileWand.PlacementOption option, out Item itemToConsume)
        {
            if (orig(self, player, randomSeed, selectCycleOffset, out option, out itemToConsume))
            {
                return true;
            }

            if (!ShouldFusion(player))
            {
                option = default;
                itemToConsume = null;
                return false;
            }

            var sources = InventoryFusionManager.GetActiveSources(player);
            for (int s = 0; s < sources.Count; s++)
            {
                var src = sources[s];
                Item[] slots = src.GetSlots(player);
                if (slots == null) continue;

                for (int i = 0; i < slots.Length; i++)
                {
                    Item it = slots[i];
                    if (it != null && !it.IsAir && (self.CanConsumeFavorites || !it.favorited) && self._options.TryGetValue(it.type, out var bucket))
                    {
                        self._random.SetSeed(randomSeed);
                        option = bucket.GetOptionWithCycling(selectCycleOffset);
                        itemToConsume = it;
                        return true;
                    }
                }
            }

            option = default;
            itemToConsume = null;
            return false;
        }

        /// <summary>
        /// 万能魔杖消耗材料后同步保存外部源数据
        /// </summary>
        private static void Hook_PlaceThing_Tiles_PlaceIt_ConsumeFlexibleWandMaterial(On_Player.orig_PlaceThing_Tiles_PlaceIt_ConsumeFlexibleWandMaterial orig, Player self)
        {
            orig(self);
            if (ShouldFusion(self))
            {
                InventoryFusionManager.NotifyAllActiveModified(self);
            }
        }

        #endregion

        #region 6. 油漆与涂料（FindPaintOrCoating）外部源支持

        /// <summary>
        /// 拦截油漆与涂料查找，使其能识别并使用外部融合源中的油漆
        /// </summary>
        private static Item Hook_FindPaintOrCoating(On_Player.orig_FindPaintOrCoating orig, Player self)
        {
            Item result = orig(self);
            if (result != null || !ShouldFusion(self)) return result;

            return InventoryFusionManager.FindMatchingItem(self, i => i.PaintOrCoating, out _);
        }

        private static void Hook_ApplyPaint(On_Player.orig_ApplyPaint orig, Player self, int x, int y, bool paintingAWall, bool applyItemAnimation, Item targetItem)
        {
            orig(self, x, y, paintingAWall, applyItemAnimation, targetItem);
            if (ShouldFusion(self))
            {
                InventoryFusionManager.NotifyAllActiveModified(self);
            }
        }

        private static void Hook_ApplyCoating(On_Player.orig_ApplyCoating orig, Player self, int x, int y, bool paintingAWall, bool applyItemAnimation, Item targetItem)
        {
            orig(self, x, y, paintingAWall, applyItemAnimation, targetItem);
            if (ShouldFusion(self))
            {
                InventoryFusionManager.NotifyAllActiveModified(self);
            }
        }

        #endregion

        #region 7. 原版制作系统（Recipe 统计与 CraftingRequests 扣料）外部融合源支持

        /// <summary>
        /// 拦截制作系统材料收集：将所有激活的外部融合源（如饰品袋、大背包等）中的未收藏材料累加进 Recipe._ownedItems，并重新计算配方组
        /// </summary>
        private static void Hook_CollectItemsToCraftWithFrom(On_Recipe.orig_CollectItemsToCraftWithFrom orig, Player player)
        {
            orig(player);

            if (!ShouldFusion(player)) return;

            // 将所有外部融合源的未收藏物品累加进 _ownedItems
            InventoryFusionManager.CollectUnfavoritedItems(player, (type, stack) =>
            {
                if (Recipe._ownedItems.TryGetValue(type, out int existing))
                {
                    Recipe._ownedItems[type] = existing + stack;
                }
                else
                {
                    Recipe._ownedItems[type] = stack;
                }
            });

            // 重新刷新配方组统计（确保木材、铁锭等 RecipeGroup 能识别外部源材料）
            Recipe.AddFakeCountsForItemGroups();
        }

        /// <summary>
        /// 拦截本地可制作判定：当背包+箱子材料不足而加上外部融合源满足时，允许本地直接合成
        /// </summary>
        private static bool Hook_CanCraftLocally(On_CraftingRequests.orig_CanCraftLocally orig, Recipe.RequiredItemEntry req, List<Chest> chests)
        {
            bool vanillaCan = orig(req, chests);
            if (vanillaCan) return true;
            if (!ShouldFusion(Main.LocalPlayer)) return false;

            int externalCount = req.IsRecipeGroup
                ? InventoryFusionManager.CountUnfavoritedMatching(Main.LocalPlayer, req.Matches, req.stack)
                : InventoryFusionManager.CountUnfavoritedItem(Main.LocalPlayer, req.itemIdOrRecipeGroup, req.stack);

            if (externalCount <= 0) return false;

            int current = CraftingRequests.CountMatches(req, Main.LocalPlayer.inventory, 58);
            if (chests != null)
            {
                for (int i = 0; i < chests.Count; i++)
                {
                    Chest chest = chests[i];
                    if (chest != null && CraftingRequests.IsLocallyAccessible(chest))
                    {
                        current += CraftingRequests.CountMatches(req, chest.item, chest.maxItems);
                    }
                }
            }

            return (current + externalCount >= req.stack);
        }

        /// <summary>
        /// 拦截制作消耗扣料：原版背包与箱子扣除后若仍有剩余需求，从外部融合源按优先级扣除未收藏材料
        /// </summary>
        private static int Hook_Consume(On_CraftingRequests.orig_Consume orig, Recipe.RequiredItemEntry req, List<Chest> chests, List<Item> consumedItems, bool fromChests)
        {
            int remaining = orig(req, chests, consumedItems, fromChests);
            if (remaining <= 0 || !ShouldFusion(Main.LocalPlayer)) return remaining;

            int taken = req.IsRecipeGroup
                ? InventoryFusionManager.ConsumeUnfavoritedMatching(Main.LocalPlayer, req.Matches, remaining)
                : InventoryFusionManager.ConsumeUnfavoritedItem(Main.LocalPlayer, req.itemIdOrRecipeGroup, remaining);

            return remaining - taken;
        }

        /// <summary>
        /// 拦截制作完成触发：制作完成后统一触发激活数据源持久化通知
        /// </summary>
        private static void Hook_CraftItem(On_CraftingRequests.orig_CraftItem orig, Recipe recipe, int num, bool flag)
        {
            orig(recipe, num, flag);
            if (ShouldFusion(Main.LocalPlayer))
            {
                InventoryFusionManager.NotifyAllActiveModified(Main.LocalPlayer);
            }
        }

        #endregion
    }
}
