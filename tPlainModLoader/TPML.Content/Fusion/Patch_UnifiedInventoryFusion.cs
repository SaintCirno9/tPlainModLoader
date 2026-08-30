using System;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.GameContent;
using TPML.Content.Engine;

namespace TPML.Content.Fusion
{
    /// <summary>
    /// TPML 框架级全量背包融合底层补丁矩阵（自 Harmony 迁移至 MonoMod，M2）：<br/>
    /// 1. 统一拦截 HasItem、CountItem、ConsumeItem 等标准查询与消耗方法，调度已注册的 <see cref="IFusionItemSource"/>；<br/>
    /// 2. 统一拦截经典魔杖（tileWand）、万能魔杖（FlexibleTileWand）、油漆涂料（FindPaintOrCoating）等原版硬编码背包遍历的特殊系统，<br/>
    ///    使生命木魔棒、树叶魔棒、骨头魔棒、蜂巢魔棒、碎石放置器、油漆刷等能够直接使用并消耗外部融合容器中的材料。
    /// 作者: SaintCirno9
    /// </summary>
    public static class Patch_UnifiedInventoryFusion
    {
        /// <summary>集中注册全部融合补丁（由 ContentHookDispatcher.ApplyOnDemandPatches 调用）</summary>
        public static void RegisterAll()
        {
            // 1. HasItem 系列
            HookRegistryAdd(MethodLookup.Instance(typeof(Player), nameof(Player.HasItem), typeof(int)),
                (Func<Func<Player, int, bool>, Player, int, bool>)((orig, self, type) =>
                {
                    bool result = orig(self, type);
                    HasItemPostfix(self, type, ref result);
                    return result;
                }));

            HookRegistryAdd(MethodLookup.Instance(typeof(Player), nameof(Player.HasItemInInventoryOrOpenVoidBag), typeof(int)),
                (Func<Func<Player, int, bool>, Player, int, bool>)((orig, self, type) =>
                {
                    bool result = orig(self, type);
                    HasItemInInventoryOrOpenVoidBagPostfix(self, type, ref result);
                    return result;
                }));

            HookRegistryAdd(MethodLookup.Instance(typeof(Player), nameof(Player.HasItemInAnyInventory), typeof(int)),
                (Func<Func<Player, int, bool>, Player, int, bool>)((orig, self, type) =>
                {
                    bool result = orig(self, type);
                    HasItemInAnyInventoryPostfix(self, type, ref result);
                    return result;
                }));

            // 2. CountItem
            HookRegistryAdd(MethodLookup.Instance(typeof(Player), nameof(Player.CountItem), typeof(int), typeof(int)),
                (Func<Func<Player, int, int, int>, Player, int, int, int>)((orig, self, type, stopCountingAt) =>
                {
                    int result = orig(self, type, stopCountingAt);
                    CountItemPostfix(self, type, stopCountingAt, ref result);
                    return result;
                }));

            // 3. ConsumeItem
            HookRegistryAdd(MethodLookup.Instance(typeof(Player), nameof(Player.ConsumeItem), typeof(int), typeof(bool), typeof(bool)),
                (Func<Func<Player, int, bool, bool, bool>, Player, int, bool, bool, bool>)((orig, self, type, reverseOrder, includeVoidBag) =>
                {
                    bool result = orig(self, type, reverseOrder, includeVoidBag);
                    ConsumeItemPostfix(self, type, reverseOrder, includeVoidBag, ref result);
                    return result;
                }));

            // 4. 经典魔杖
            // PlaceThing_Tiles_CheckWandUsability(bool canUse)（实例，返回 bool）
            HookRegistryAdd(GetInstanceMethod(typeof(Player), nameof(Player.PlaceThing_Tiles_CheckWandUsability)),
                (Func<Func<Player, bool, bool>, Player, bool, bool>)((orig, self, canUse) =>
                {
                    bool result = orig(self, canUse);
                    PlaceThing_Tiles_CheckWandUsabilityPostfix(self, ref result);
                    return result;
                }));

            // ItemCheck_CheckCanUse_Inner(Item, bool ignoreCursed)（实例，返回 bool）
            HookRegistryAdd(GetInstanceMethod(typeof(Player), nameof(Player.ItemCheck_CheckCanUse_Inner)),
                (Func<Func<Player, Item, bool, bool>, Player, Item, bool, bool>)((orig, self, sItem, ignoreCursed) =>
                {
                    bool result = orig(self, sItem, ignoreCursed);
                    ItemCheck_CheckCanUse_InnerPostfix(self, sItem, ref result);
                    return result;
                }));

            // ItemCheck 前缀+后缀共享 __state
            HookRegistryAdd(MethodLookup.Instance(typeof(Player), nameof(Player.ItemCheck)),
                (Action<Action<Player>, Player>)((orig, self) =>
                {
                    (bool shouldCheckWand, int wandType, bool hadInInventory) state = default;
                    ItemCheckPrefix(self, out state);
                    orig(self);
                    ItemCheckPostfix(self, state);
                }));

            // 5. 万能魔杖
            HookRegistryAdd(GetInstanceMethod(typeof(FlexibleTileWand), nameof(FlexibleTileWand.TryGetPlacementOption)),
                (Hook_TryGetPlacementOption)TryGetPlacementOptionHook);

            HookRegistryAdd(GetInstanceMethod(typeof(Player), nameof(Player.PlaceThing_Tiles_PlaceIt_ConsumeFlexibleWandMaterial)),
                (Action<Action<Player>, Player>)((orig, self) =>
                {
                    orig(self);
                    PlaceThing_Tiles_PlaceIt_ConsumeFlexibleWandMaterialPostfix(self);
                }));

            // 6. 油漆与涂料
            HookRegistryAdd(GetInstanceMethod(typeof(Player), nameof(Player.FindPaintOrCoating)),
                (Func<Func<Player, Item>, Player, Item>)((orig, self) =>
                {
                    Item result = orig(self);
                    FindPaintOrCoatingPostfix(self, ref result);
                    return result;
                }));

            // ApplyPaint/ApplyCoating(int x, int y, bool paintingAWall, bool applyItemAnimation, Item targetItem)（实例）
            HookRegistryAdd(GetInstanceMethod(typeof(Player), "ApplyPaint"),
                (Action<Action<Player, int, int, bool, bool, Item>, Player, int, int, bool, bool, Item>)((orig, self, x, y, paintingAWall, applyItemAnimation, targetItem) =>
                {
                    orig(self, x, y, paintingAWall, applyItemAnimation, targetItem);
                    ApplyPaintPostfix(self);
                }));

            HookRegistryAdd(GetInstanceMethod(typeof(Player), "ApplyCoating"),
                (Action<Action<Player, int, int, bool, bool, Item>, Player, int, int, bool, bool, Item>)((orig, self, x, y, paintingAWall, applyItemAnimation, targetItem) =>
                {
                    orig(self, x, y, paintingAWall, applyItemAnimation, targetItem);
                    ApplyCoatingPostfix(self);
                }));

            // 7. 原版制作系统
            // Recipe.CollectItemsToCraftWithFrom(Player) 为 private static
            HookRegistryAdd(GetStaticMethod(typeof(Recipe), nameof(Recipe.CollectItemsToCraftWithFrom)),
                (Action<Action<Player>, Player>)((orig, player) =>
                {
                    orig(player);
                    CollectItemsToCraftWithFromPostfix(player);
                }));

            HookRegistryAdd(GetStaticMethod(typeof(CraftingRequests), nameof(CraftingRequests.CanCraftLocally)),
                (Func<Func<Recipe.RequiredItemEntry, List<Chest>, bool>, Recipe.RequiredItemEntry, List<Chest>, bool>)((orig, req, chests) =>
                {
                    bool result = orig(req, chests);
                    CanCraftLocallyPostfix(req, chests, ref result);
                    return result;
                }));

            HookRegistryAdd(GetStaticMethod(typeof(CraftingRequests), nameof(CraftingRequests.Consume)),
                (Func<Func<Recipe.RequiredItemEntry, List<Chest>, List<Item>, bool, int>, Recipe.RequiredItemEntry, List<Chest>, List<Item>, bool, int>)((orig, req, chests, consumedItems, fromChests) =>
                {
                    int result = orig(req, chests, consumedItems, fromChests);
                    ConsumePostfix(req, chests, consumedItems, fromChests, ref result);
                    return result;
                }));

            // CraftingRequests.CraftItem(Recipe, int, bool)（静态）
            HookRegistryAdd(GetStaticMethod(typeof(CraftingRequests), nameof(CraftingRequests.CraftItem)),
                (Action<Action<Recipe, int, bool>, Recipe, int, bool>)((orig, recipe, num, flag) =>
                {
                    orig(recipe, num, flag);
                    CraftItemPostfix();
                }));
        }

        private static void HookRegistryAdd(MethodBase target, Delegate detour)
        {
            if (target == null) throw new MissingMethodException("[Fusion] 目标方法查找失败，请核对游戏版本签名");
            TPML.Content.Engine.HookRegistry.AddContent(target, detour);
        }

        private static MethodInfo GetInstanceMethod(Type type, string name)
        {
            return type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private static MethodInfo GetStaticMethod(Type type, string name)
        {
            return type.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        #region 万能魔杖自定义委托（原方法含 out 参数）

        private delegate bool Orig_TryGetPlacementOption(FlexibleTileWand self, Player player, int randomSeed, int selectCycleOffset, ref FlexibleTileWand.PlacementOption option, ref Item itemToConsume);
        private delegate bool Hook_TryGetPlacementOption(Orig_TryGetPlacementOption orig, FlexibleTileWand self, Player player, int randomSeed, int selectCycleOffset, ref FlexibleTileWand.PlacementOption option, ref Item itemToConsume);

        private static bool TryGetPlacementOptionHook(Orig_TryGetPlacementOption orig, FlexibleTileWand self, Player player, int randomSeed, int selectCycleOffset, ref FlexibleTileWand.PlacementOption option, ref Item itemToConsume)
        {
            bool result = orig(self, player, randomSeed, selectCycleOffset, ref option, ref itemToConsume);
            TryGetPlacementOptionPostfix(self, player, randomSeed, selectCycleOffset, ref option, ref itemToConsume, ref result);
            return result;
        }

        #endregion

        private static bool ShouldFusion(Player player)
        {
            if (player == null || !player.active || player.whoAmI != Main.myPlayer) return false;
            return true;
        }

        #region 1. HasItem 系列查询拦截

        private static void HasItemPostfix(Player __instance, int type, ref bool __result)
        {
            if (__result) return;
            if (!ShouldFusion(__instance)) return;

            if (InventoryFusionManager.HasItem(__instance, type))
            {
                __result = true;
            }
        }

        private static void HasItemInInventoryOrOpenVoidBagPostfix(Player __instance, int type, ref bool __result)
        {
            if (__result) return;
            if (!ShouldFusion(__instance)) return;

            if (InventoryFusionManager.HasItem(__instance, type))
            {
                __result = true;
            }
        }

        private static void HasItemInAnyInventoryPostfix(Player __instance, int type, ref bool __result)
        {
            if (__result) return;
            if (!ShouldFusion(__instance)) return;

            if (InventoryFusionManager.HasItem(__instance, type))
            {
                __result = true;
            }
        }

        #endregion

        #region 2. CountItem 数量统计拦截

        private static void CountItemPostfix(Player __instance, int type, int stopCountingAt, ref int __result)
        {
            if (!ShouldFusion(__instance)) return;
            if (stopCountingAt > 0 && __result >= stopCountingAt) return;

            int externalCount = InventoryFusionManager.CountItem(__instance, type, stopCountingAt > 0 ? stopCountingAt - __result : 0);
            __result += externalCount;
        }

        #endregion

        #region 3. ConsumeItem 自动消耗扣除拦截

        private static void ConsumeItemPostfix(Player __instance, int type, bool reverseOrder, bool includeVoidBag, ref bool __result)
        {
            if (__result) return;
            if (!ShouldFusion(__instance)) return;

            if (InventoryFusionManager.ConsumeItem(__instance, type, reverseOrder))
            {
                __result = true;
            }
        }

        #endregion

        #region 4. 经典魔杖（tileWand）与漆刷等原版硬编码放置/使用/消耗支持

        /// <summary>
        /// 拦截物块放置时的经典魔杖（生命木魔棒、树叶魔棒、骨头魔棒等）可用性判定
        /// </summary>
        private static void PlaceThing_Tiles_CheckWandUsabilityPostfix(Player __instance, ref bool __result)
        {
            if (__result) return;
            if (!ShouldFusion(__instance)) return;

            Item held = __instance.inventory[__instance.selectedItem];
            if (held != null && held.tileWand > 0 && InventoryFusionManager.HasItem(__instance, held.tileWand))
            {
                __result = true;
            }
        }

        /// <summary>
        /// 拦截挥动检测中的经典魔杖与漆刷/涂料刷可用性判定
        /// </summary>
        private static void ItemCheck_CheckCanUse_InnerPostfix(Player __instance, Item sItem, ref bool __result)
        {
            if (__result) return;
            if (!ShouldFusion(__instance) || sItem == null) return;

            // 经典魔杖判定（生命木魔棒等）
            if (sItem.tileWand > 0 && InventoryFusionManager.HasItem(__instance, sItem.tileWand))
            {
                __result = true;
                return;
            }

            // 漆刷 / 滚筒 / 涂料刷判定 (1071=漆刷, 1072=滚筒, 1543=漆铲, 1544=涂料刷)
            if ((sItem.type == 1071 || sItem.type == 1072 || sItem.type == 1543 || sItem.type == 1544) &&
                InventoryFusionManager.HasMatchingItem(__instance, i => i.PaintOrCoating))
            {
                __result = true;
                return;
            }
        }

        /// <summary>
        /// 拦截魔杖挥动放置物块时的消耗逻辑：<br/>
        /// 原版在 ItemCheck 内部硬编码扣除 inventory[0..57]，若背包中无材料而外部融合源中有，在此扣除外部源中的 1 个材料。
        /// </summary>
        private static void ItemCheckPrefix(Player __instance, out (bool shouldCheckWand, int wandType, bool hadInInventory) __state)
        {
            __state = default;
            if (!ShouldFusion(__instance)) return;

            Item item = __instance.inventory[__instance.selectedItem];
            if (item == null || item.tileWand <= 0) return;

            if (!__instance.dontConsumeWand && __instance.itemTimeMax != 0 && __instance.itemTime == __instance.itemTimeMax)
            {
                int tileWand = item.tileWand;
                bool hasInInv = false;
                for (int i = 0; i < 58; i++)
                {
                    if (__instance.inventory[i] != null && __instance.inventory[i].type == tileWand && __instance.inventory[i].stack > 0)
                    {
                        hasInInv = true;
                        break;
                    }
                }
                __state = (true, tileWand, hasInInv);
            }
        }

        private static void ItemCheckPostfix(Player __instance, (bool shouldCheckWand, int wandType, bool hadInInventory) __state)
        {
            if (!__state.shouldCheckWand) return;
            if (!ShouldFusion(__instance)) return;
            if (__instance.dontConsumeWand) return;

            // 原版背包原本无该材料（原版未扣除），由外部融合源扣除
            if (!__state.hadInInventory)
            {
                InventoryFusionManager.ConsumeItem(__instance, __state.wandType);
            }
        }

        #endregion

        #region 5. 万能魔杖（FlexibleTileWand）外部源支持

        /// <summary>
        /// 拦截 1.4.4 万能魔杖（碎石放置器 Rubble Maker、便携式熔炉等）的放置选项与材料匹配
        /// </summary>
        private static void TryGetPlacementOptionPostfix(FlexibleTileWand __instance, Player player, int randomSeed, int selectCycleOffset, ref FlexibleTileWand.PlacementOption option, ref Item itemToConsume, ref bool __result)
        {
            if (__result) return;
            if (!ShouldFusion(player)) return;

            var sources = InventoryFusionManager.GetActiveSources(player);
            for (int s = 0; s < sources.Count; s++)
            {
                var src = sources[s];
                Item[] slots = src.GetSlots(player);
                if (slots == null) continue;

                for (int i = 0; i < slots.Length; i++)
                {
                    Item it = slots[i];
                    if (it != null && !it.IsAir && (__instance.CanConsumeFavorites || !it.favorited) && __instance._options.TryGetValue(it.type, out var bucket))
                    {
                        __instance._random.SetSeed(randomSeed);
                        option = bucket.GetOptionWithCycling(selectCycleOffset);
                        itemToConsume = it;
                        __result = true;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// 万能魔杖消耗材料后同步保存外部源数据
        /// </summary>
        private static void PlaceThing_Tiles_PlaceIt_ConsumeFlexibleWandMaterialPostfix(Player __instance)
        {
            if (!ShouldFusion(__instance)) return;
            InventoryFusionManager.NotifyAllActiveModified(__instance);
        }

        #endregion

        #region 6. 油漆与涂料（FindPaintOrCoating）外部源支持

        /// <summary>
        /// 拦截油漆与涂料查找，使其能识别并使用外部融合源中的油漆
        /// </summary>
        private static void FindPaintOrCoatingPostfix(Player __instance, ref Item __result)
        {
            if (__result != null) return;
            if (!ShouldFusion(__instance)) return;

            Item match = InventoryFusionManager.FindMatchingItem(__instance, i => i.PaintOrCoating, out _);
            if (match != null)
            {
                __result = match;
            }
        }

        private static void ApplyPaintPostfix(Player __instance)
        {
            if (!ShouldFusion(__instance)) return;
            InventoryFusionManager.NotifyAllActiveModified(__instance);
        }

        private static void ApplyCoatingPostfix(Player __instance)
        {
            if (!ShouldFusion(__instance)) return;
            InventoryFusionManager.NotifyAllActiveModified(__instance);
        }

        #endregion

        #region 7. 原版制作系统（Recipe 统计与 CraftingRequests 扣料）外部融合源支持

        /// <summary>
        /// 拦截制作系统材料收集：将所有激活的外部融合源（如饰品袋、大背包等）中的未收藏材料累加进 Recipe._ownedItems，并重新计算配方组
        /// </summary>
        private static void CollectItemsToCraftWithFromPostfix(Player player)
        {
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
        private static void CanCraftLocallyPostfix(Recipe.RequiredItemEntry req, List<Chest> chests, ref bool __result)
        {
            if (__result) return;
            if (!ShouldFusion(Main.LocalPlayer)) return;

            int externalCount = req.IsRecipeGroup
                ? InventoryFusionManager.CountUnfavoritedMatching(Main.LocalPlayer, req.Matches, req.stack)
                : InventoryFusionManager.CountUnfavoritedItem(Main.LocalPlayer, req.itemIdOrRecipeGroup, req.stack);

            if (externalCount <= 0) return;

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

            if (current + externalCount >= req.stack)
            {
                __result = true;
            }
        }

        /// <summary>
        /// 拦截制作消耗扣料：原版背包与箱子扣除后若仍有剩余需求，从外部融合源按优先级扣除未收藏材料
        /// </summary>
        private static void ConsumePostfix(Recipe.RequiredItemEntry req, List<Chest> chests, List<Item> consumedItems, bool fromChests, ref int __result)
        {
            if (__result <= 0) return;
            if (!ShouldFusion(Main.LocalPlayer)) return;

            int needed = __result;
            int taken = req.IsRecipeGroup
                ? InventoryFusionManager.ConsumeUnfavoritedMatching(Main.LocalPlayer, req.Matches, needed)
                : InventoryFusionManager.ConsumeUnfavoritedItem(Main.LocalPlayer, req.itemIdOrRecipeGroup, needed);

            __result -= taken;
        }

        /// <summary>
        /// 拦截制作完成触发：制作完成后统一触发激活数据源持久化通知
        /// </summary>
        private static void CraftItemPostfix()
        {
            if (!ShouldFusion(Main.LocalPlayer)) return;
            InventoryFusionManager.NotifyAllActiveModified(Main.LocalPlayer);
        }

        #endregion
    }
}
