using HarmonyLib;
using OptimizeAndTool.Content.BigBag;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;

namespace OptimizeAndTool.Content.QoL.GuaranteedDrop
{
    /// <summary>
    /// 全场景全物品首见保底掉落补丁总集：
    /// 1. 拦截 ItemDropResolver.ResolveRule（怪物/Boss 掉落池）：未获取物品 100% 必定掉落，多选一池首次全量大爆；
    /// 2. 拦截 Player.OpenBossBag（专家/大师 Boss 宝藏袋）：专属战利品首开保底；
    /// 3. 拦截 Player.OpenFishingCrate 及各摸奖袋/锁盒：宝匣与专属战利品首开保底；
    /// 4. 监听 Player.GetItem 与 BigBag.DepositItem：实时收录新发现物品。
    /// 作者: SaintCirno9
    /// </summary>
    public static class Patch_GuaranteedDrop
    {
        #region 1. 怪物掉落规则底层拦截 (ItemDropResolver)

        [HarmonyPatch(typeof(ItemDropResolver), nameof(ItemDropResolver.ResolveRule))]
        internal static class Patch_ItemDropResolver_ResolveRule
        {
            [HarmonyPrefix]
            internal static bool Prefix(ItemDropResolver __instance, IItemDropRule rule, DropAttemptInfo info, ref ItemDropAttemptResult __result)
            {
                if (!GuaranteedDropSystem.EnableGuaranteedDrop.val) return true;
                if (rule == null || info.player == null || info.npc == null) return true;
                if (info.IsInSimulation) return true; // 图鉴概率模拟计算不修改

                // 必须满足前置条件（如肉山后、夜晚、日食、特定事件等），不破坏游戏进程机制
                if (!rule.CanDrop(info))
                {
                    return true;
                }

                // A. 单物品普通掉落规则 (CommonDrop 及其派生类)
                if (rule is CommonDrop commonDrop)
                {
                    int itemId = commonDrop.itemId;
                    if (itemId > 0 && !DiscoveredItemTracker.HasDiscovered(info.player, itemId))
                    {
                        int min = commonDrop.amountDroppedMinimum;
                        int max = commonDrop.amountDroppedMaximum;
                        int stack = min >= max ? min : info.rng.Next(min, max + 1);
                        CommonCode.DropItemFromNPC(info.npc, itemId, stack);
                        DiscoveredItemTracker.RecordDiscovered(itemId);

                        __result = new ItemDropAttemptResult
                        {
                            State = ItemDropAttemptResultState.Success
                        };
                        __instance.ResolveRuleChains(rule, info, __result);
                        return false;
                    }
                }
                // B. 多选一掉落规则 (OneFromOptionsDropRule)
                else if (rule is OneFromOptionsDropRule optionsRule)
                {
                    int[] options = optionsRule.dropIds;
                    if (options != null && options.Length > 0)
                    {
                        List<int> undiscovered = new List<int>();
                        for (int i = 0; i < options.Length; i++)
                        {
                            int id = options[i];
                            if (id > 0 && !DiscoveredItemTracker.HasDiscovered(info.player, id))
                            {
                                undiscovered.Add(id);
                            }
                        }

                        if (undiscovered.Count > 0)
                        {
                            if (GuaranteedDropSystem.EnableMultiOptionBurst.val)
                            {
                                // 全量大爆特爆：掉出该池中所有未获取物品
                                for (int i = 0; i < undiscovered.Count; i++)
                                {
                                    int id = undiscovered[i];
                                    CommonCode.DropItemFromNPC(info.npc, id, 1);
                                    DiscoveredItemTracker.RecordDiscovered(id);
                                }
                            }
                            else
                            {
                                // 单件优先：随机掉落 1 件未获取物品
                                int chosen = undiscovered[info.rng.Next(undiscovered.Count)];
                                CommonCode.DropItemFromNPC(info.npc, chosen, 1);
                                DiscoveredItemTracker.RecordDiscovered(chosen);
                            }

                            __result = new ItemDropAttemptResult
                            {
                                State = ItemDropAttemptResultState.Success
                            };
                            __instance.ResolveRuleChains(rule, info, __result);
                            return false;
                        }
                    }
                }
                // C. 多选一不随幸运缩放规则 (OneFromOptionsNotScaledWithLuckDropRule)
                else if (rule is OneFromOptionsNotScaledWithLuckDropRule optionsNotLuckRule)
                {
                    int[] options = optionsNotLuckRule.dropIds;
                    if (options != null && options.Length > 0)
                    {
                        List<int> undiscovered = new List<int>();
                        for (int i = 0; i < options.Length; i++)
                        {
                            int id = options[i];
                            if (id > 0 && !DiscoveredItemTracker.HasDiscovered(info.player, id))
                            {
                                undiscovered.Add(id);
                            }
                        }

                        if (undiscovered.Count > 0)
                        {
                            if (GuaranteedDropSystem.EnableMultiOptionBurst.val)
                            {
                                for (int i = 0; i < undiscovered.Count; i++)
                                {
                                    int id = undiscovered[i];
                                    CommonCode.DropItemFromNPC(info.npc, id, 1);
                                    DiscoveredItemTracker.RecordDiscovered(id);
                                }
                            }
                            else
                            {
                                int chosen = undiscovered[info.rng.Next(undiscovered.Count)];
                                CommonCode.DropItemFromNPC(info.npc, chosen, 1);
                                DiscoveredItemTracker.RecordDiscovered(chosen);
                            }

                            __result = new ItemDropAttemptResult
                            {
                                State = ItemDropAttemptResultState.Success
                            };
                            __instance.ResolveRuleChains(rule, info, __result);
                            return false;
                        }
                    }
                }
                // D. 不重复多选规则 (FromOptionsWithoutRepeatsDropRule)
                else if (rule is FromOptionsWithoutRepeatsDropRule withoutRepeatsRule)
                {
                    int[] options = withoutRepeatsRule.dropIds;
                    if (options != null && options.Length > 0)
                    {
                        List<int> undiscovered = new List<int>();
                        for (int i = 0; i < options.Length; i++)
                        {
                            int id = options[i];
                            if (id > 0 && !DiscoveredItemTracker.HasDiscovered(info.player, id))
                            {
                                undiscovered.Add(id);
                            }
                        }

                        if (undiscovered.Count > 0)
                        {
                            if (GuaranteedDropSystem.EnableMultiOptionBurst.val)
                            {
                                for (int i = 0; i < undiscovered.Count; i++)
                                {
                                    int id = undiscovered[i];
                                    CommonCode.DropItemFromNPC(info.npc, id, 1);
                                    DiscoveredItemTracker.RecordDiscovered(id);
                                }
                            }
                            else
                            {
                                int chosen = undiscovered[info.rng.Next(undiscovered.Count)];
                                CommonCode.DropItemFromNPC(info.npc, chosen, 1);
                                DiscoveredItemTracker.RecordDiscovered(chosen);
                            }

                            __result = new ItemDropAttemptResult
                            {
                                State = ItemDropAttemptResultState.Success
                            };
                            __instance.ResolveRuleChains(rule, info, __result);
                            return false;
                        }
                    }
                }

                // 其他嵌套规则继续由原版派发（会自动递归调用 ResolveRule）
                return true;
            }
        }

        #endregion

        #region 2. Boss 宝藏袋首开保底 (Player.OpenBossBag)

        private static readonly Dictionary<int, int[]> BossBagLootTable = new Dictionary<int, int[]>
        {
            // 史莱姆王 (3318)
            { 3318, new int[] { 2430, 2493, 256, 257, 258, 2610, 998, 3090 } },
            // 克苏鲁之眼 (3319)
            { 3319, new int[] { 2112, 1299, 2108 } },
            // 世界吞噬怪 (3320)
            { 3320, new int[] { 3224, 3091, 2105 } },
            // 克苏鲁之脑 (3321)
            { 3321, new int[] { 3223, 3092, 2106 } },
            // 蜂王 (3322)
            { 3322, new int[] { 3333, 1121, 1122, 1123, 1133, 1134, 2431, 1132, 2435, 1131 } },
            // 骷髅王 (3323)
            { 3323, new int[] { 3245, 865, 3094, 1280, 1279, 1313, 1305 } },
            // 血肉墙 (3324)
            { 3324, new int[] { 3225, 489, 490, 491, 2998, 426, 434, 514, 533, 3018, 2111, 3095 } },
            // 双子魔眼 (3325)
            { 3325, new int[] { 3353, 2107, 3096 } },
            // 毁灭者 (3326)
            { 3326, new int[] { 3354, 2109, 3097 } },
            // 机械骷髅王 (3327)
            { 3327, new int[] { 3355, 2110, 3098 } },
            // 世纪之花 (3328)
            { 3328, new int[] { 3336, 757, 1254, 1255, 1258, 1259, 1260, 1295, 3019, 1141, 1297, 2113, 3099 } },
            // 石巨人 (3329)
            { 3329, new int[] { 3337, 1251, 1256, 1257, 1294, 1296, 2114, 3100, 1293 } },
            // 猪鲨公爵 (3330)
            { 3330, new int[] { 3338, 2611, 2621, 2622, 2623, 2624, 3101, 2588 } },
            // 月球领主 (3332)
            { 3332, new int[] { 3339, 3531, 3546, 3063, 3065, 3106, 3389, 3540, 3541, 3542, 3543, 3544, 3545, 3547 } },
            // 光之女皇 (4957)
            { 4957, new int[] { 4986, 4782, 4783, 4784, 4785, 4786, 4787, 4788, 4789, 4959, 4960 } },
            // 史莱姆皇后 (4958)
            { 4958, new int[] { 4987, 4982, 4983, 4984, 4985, 4981, 4961, 4962 } },
            // 巨鹿 (5070)
            { 5070, new int[] { 5098, 5100, 5099, 5101, 5109, 5108, 5110, 5111 } }
        };

        [HarmonyPatch(typeof(Player), nameof(Player.OpenBossBag))]
        internal static class Patch_Player_OpenBossBag
        {
            [HarmonyPrefix]
            internal static void Prefix(Player __instance, int type)
            {
                if (!GuaranteedDropSystem.EnableGuaranteedDrop.val || __instance == null || __instance != Main.LocalPlayer) return;

                if (BossBagLootTable.TryGetValue(type, out int[] potentialLoot) && potentialLoot != null)
                {
                    List<int> undiscovered = new List<int>();
                    for (int i = 0; i < potentialLoot.Length; i++)
                    {
                        int itemId = potentialLoot[i];
                        if (itemId > 0 && itemId < ItemID.Count && !DiscoveredItemTracker.HasDiscovered(__instance, itemId))
                        {
                            undiscovered.Add(itemId);
                        }
                    }

                    if (undiscovered.Count > 0)
                    {
                        IEntitySource source = __instance.GetItemSource_OpenItem(type);
                        if (GuaranteedDropSystem.EnableMultiOptionBurst.val)
                        {
                            for (int i = 0; i < undiscovered.Count; i++)
                            {
                                int itemId = undiscovered[i];
                                __instance.QuickSpawnItem(source, itemId, 1);
                                DiscoveredItemTracker.RecordDiscovered(itemId);
                            }
                        }
                        else
                        {
                            int chosen = undiscovered[Main.rand.Next(undiscovered.Count)];
                            __instance.QuickSpawnItem(source, chosen, 1);
                            DiscoveredItemTracker.RecordDiscovered(chosen);
                        }
                    }
                }
            }
        }

        #endregion

        #region 3. 钓鱼宝匣、摸奖包与锁盒保底 (Player.OpenFishingCrate / OpenCanofWorms / OpenLockBox 等)

        private static readonly Dictionary<int, int[]> CrateExclusiveLootTable = new Dictionary<int, int[]>
        {
            // 木匣 / 珍珠木匣 (2334, 3979)
            { 2334, new int[] { 285, 953, 4341, 3068, 3084, 3200, 3201 } },
            { 3979, new int[] { 285, 953, 4341, 3068, 3084, 3200, 3201, 3064, 2424 } },
            // 铁匣 / 秘银匣 (2335, 3980)
            { 2335, new int[] { 158, 159, 285, 953, 4341, 3068, 3084 } },
            { 3980, new int[] { 158, 159, 285, 953, 4341, 3068, 3084, 3064, 2424 } },
            // 金匣 / 钛金匣 (2336, 3981)
            { 2336, new int[] { 997, 3064, 2424 } },
            { 3981, new int[] { 997, 3064, 2424 } },
            // 地牢匣 / 围栏匣 (3203, 3982)
            { 3203, new int[] { 3085 } },
            { 3982, new int[] { 3085 } },
            // 天空匣 / 天蓝匣 (3206, 3985)
            { 3206, new int[] { 158, 65, 159, 2219, 1584 } },
            { 3985, new int[] { 158, 65, 159, 2219, 1584 } },
            // 丛林匣 / 荆棘匣 (3208, 3987)
            { 3208, new int[] { 211, 212, 964, 2292, 2266, 2289, 753 } },
            { 3987, new int[] { 211, 212, 964, 2292, 2266, 2289, 753 } },
            // 腐化匣 / 污损匣 (3204, 3983)
            { 3204, new int[] { 64, 96, 111, 162, 800, 3210 } },
            { 3983, new int[] { 64, 96, 111, 162, 800, 3210 } },
            // 猩红匣 / 血匣 (3207, 3986)
            { 3207, new int[] { 802, 1257, 1290, 800, 3211 } },
            { 3986, new int[] { 802, 1257, 1290, 800, 3211 } },
            // 神圣匣 / 圣灵匣 (3205, 3984)
            { 3205, new int[] { 520, 502, 2426, 3064 } },
            { 3984, new int[] { 520, 502, 2426, 3064 } },
            // 冰冻匣 / 极寒匣 (3209, 3988)
            { 3209, new int[] { 670, 724, 669, 725, 987, 1579, 3200 } },
            { 3988, new int[] { 670, 724, 669, 725, 987, 1579, 3200 } },
            // 绿洲匣 / 海市蜃楼匣 (4442, 4443)
            { 4442, new int[] { 4483, 4055, 4484, 4056, 4485 } },
            { 4443, new int[] { 4483, 4055, 4484, 4056, 4485 } },
            // 海洋匣 / 渊海匣 (4444, 4445)
            { 4444, new int[] { 186, 277, 863, 4404, 4057 } },
            { 4445, new int[] { 186, 277, 863, 4404, 4057 } }
        };

        [HarmonyPatch(typeof(Player), nameof(Player.OpenFishingCrate))]
        internal static class Patch_Player_OpenFishingCrate
        {
            [HarmonyPrefix]
            internal static void Prefix(Player __instance, int crateItemID)
            {
                if (!GuaranteedDropSystem.EnableGuaranteedDrop.val || __instance == null || __instance != Main.LocalPlayer) return;

                if (CrateExclusiveLootTable.TryGetValue(crateItemID, out int[] potentialLoot) && potentialLoot != null)
                {
                    List<int> undiscovered = new List<int>();
                    for (int i = 0; i < potentialLoot.Length; i++)
                    {
                        int itemId = potentialLoot[i];
                        if (itemId > 0 && itemId < ItemID.Count && !DiscoveredItemTracker.HasDiscovered(__instance, itemId))
                        {
                            undiscovered.Add(itemId);
                        }
                    }

                    if (undiscovered.Count > 0)
                    {
                        IEntitySource source = __instance.GetItemSource_OpenItem(crateItemID);
                        if (GuaranteedDropSystem.EnableMultiOptionBurst.val)
                        {
                            for (int i = 0; i < undiscovered.Count; i++)
                            {
                                int itemId = undiscovered[i];
                                __instance.QuickSpawnItem(source, itemId, 1);
                                DiscoveredItemTracker.RecordDiscovered(itemId);
                            }
                        }
                        else
                        {
                            int chosen = undiscovered[Main.rand.Next(undiscovered.Count)];
                            __instance.QuickSpawnItem(source, chosen, 1);
                            DiscoveredItemTracker.RecordDiscovered(chosen);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OpenCanofWorms))]
        internal static class Patch_Player_OpenCanofWorms
        {
            [HarmonyPrefix]
            internal static void Prefix(Player __instance, int sourceItemType)
            {
                if (!GuaranteedDropSystem.EnableGuaranteedDrop.val || __instance == null || __instance != Main.LocalPlayer) return;

                // 蠕虫罐头首见保底：金蠕虫 (2895)
                if (!DiscoveredItemTracker.HasDiscovered(__instance, 2895))
                {
                    __instance.QuickSpawnItem(__instance.GetItemSource_OpenItem(sourceItemType), 2895, 1);
                    DiscoveredItemTracker.RecordDiscovered(2895);
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OpenOyster))]
        internal static class Patch_Player_OpenOyster
        {
            [HarmonyPrefix]
            internal static void Prefix(Player __instance, int sourceItemType)
            {
                if (!GuaranteedDropSystem.EnableGuaranteedDrop.val || __instance == null || __instance != Main.LocalPlayer) return;

                // 生蚝首见保底：白珍珠 (4412)、黑珍珠 (4413)、粉珍珠 (4414)
                int[] pearls = { 4412, 4413, 4414 };
                List<int> undiscovered = new List<int>();
                for (int i = 0; i < pearls.Length; i++)
                {
                    int itemId = pearls[i];
                    if (!DiscoveredItemTracker.HasDiscovered(__instance, itemId))
                    {
                        undiscovered.Add(itemId);
                    }
                }

                if (undiscovered.Count > 0)
                {
                    IEntitySource source = __instance.GetItemSource_OpenItem(sourceItemType);
                    if (GuaranteedDropSystem.EnableMultiOptionBurst.val)
                    {
                        for (int i = 0; i < undiscovered.Count; i++)
                        {
                            int itemId = undiscovered[i];
                            __instance.QuickSpawnItem(source, itemId, 1);
                            DiscoveredItemTracker.RecordDiscovered(itemId);
                        }
                    }
                    else
                    {
                        int chosen = undiscovered[Main.rand.Next(undiscovered.Count)];
                        __instance.QuickSpawnItem(source, chosen, 1);
                        DiscoveredItemTracker.RecordDiscovered(chosen);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OpenLockBox))]
        internal static class Patch_Player_OpenLockBox
        {
            [HarmonyPrefix]
            internal static void Prefix(Player __instance, int lockboxItemType)
            {
                if (!GuaranteedDropSystem.EnableGuaranteedDrop.val || __instance == null || __instance != Main.LocalPlayer) return;

                // 金锁盒地牢武器
                int[] dungeonWeapons = { 155, 156, 157, 163, 113, 327, 3317 };
                List<int> undiscovered = new List<int>();
                for (int i = 0; i < dungeonWeapons.Length; i++)
                {
                    int itemId = dungeonWeapons[i];
                    if (!DiscoveredItemTracker.HasDiscovered(__instance, itemId))
                    {
                        undiscovered.Add(itemId);
                    }
                }

                if (undiscovered.Count > 0)
                {
                    IEntitySource source = __instance.GetItemSource_OpenItem(lockboxItemType);
                    if (GuaranteedDropSystem.EnableMultiOptionBurst.val)
                    {
                        for (int i = 0; i < undiscovered.Count; i++)
                        {
                            int itemId = undiscovered[i];
                            __instance.QuickSpawnItem(source, itemId, 1);
                            DiscoveredItemTracker.RecordDiscovered(itemId);
                        }
                    }
                    else
                    {
                        int chosen = undiscovered[Main.rand.Next(undiscovered.Count)];
                        __instance.QuickSpawnItem(source, chosen, 1);
                        DiscoveredItemTracker.RecordDiscovered(chosen);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OpenShadowLockbox))]
        internal static class Patch_Player_OpenShadowLockbox
        {
            [HarmonyPrefix]
            internal static void Prefix(Player __instance, int boxType)
            {
                if (!GuaranteedDropSystem.EnableGuaranteedDrop.val || __instance == null || __instance != Main.LocalPlayer) return;

                // 黑曜石锁盒地狱武器
                int[] hellWeapons = { 274, 220, 112, 218, 3019 };
                List<int> undiscovered = new List<int>();
                for (int i = 0; i < hellWeapons.Length; i++)
                {
                    int itemId = hellWeapons[i];
                    if (!DiscoveredItemTracker.HasDiscovered(__instance, itemId))
                    {
                        undiscovered.Add(itemId);
                    }
                }

                if (undiscovered.Count > 0)
                {
                    IEntitySource source = __instance.GetItemSource_OpenItem(boxType);
                    if (GuaranteedDropSystem.EnableMultiOptionBurst.val)
                    {
                        for (int i = 0; i < undiscovered.Count; i++)
                        {
                            int itemId = undiscovered[i];
                            __instance.QuickSpawnItem(source, itemId, 1);
                            DiscoveredItemTracker.RecordDiscovered(itemId);
                        }
                    }
                    else
                    {
                        int chosen = undiscovered[Main.rand.Next(undiscovered.Count)];
                        __instance.QuickSpawnItem(source, chosen, 1);
                        DiscoveredItemTracker.RecordDiscovered(chosen);
                    }
                }
            }
        }

        #endregion

        #region 4. 实时拾取与背包存入监听 (Player.GetItem / BigBag.DepositItem)

        [HarmonyPatch(typeof(Player), nameof(Player.GetItem))]
        internal static class Patch_Player_GetItem
        {
            [HarmonyPostfix]
            internal static void Postfix(Player __instance, Item newItem, Item __result)
            {
                if (__instance == null || __instance != Main.LocalPlayer || newItem == null || newItem.IsAir || newItem.type <= 0) return;

                // 如果物品成功被玩家接收（全部或部分进入背包）
                if (__result == null || __result.stack < newItem.stack || __result.IsAir)
                {
                    DiscoveredItemTracker.RecordDiscovered(newItem.type);
                }
            }
        }

        [HarmonyPatch(typeof(BigBag.BigBag), nameof(BigBag.BigBag.DepositItem))]
        internal static class Patch_BigBag_DepositItem
        {
            [HarmonyPostfix]
            internal static void Postfix(Item item, bool __result)
            {
                if (__result && item != null && item.type > 0)
                {
                    DiscoveredItemTracker.RecordDiscovered(item.type);
                }
            }
        }

        #endregion
    }
}
