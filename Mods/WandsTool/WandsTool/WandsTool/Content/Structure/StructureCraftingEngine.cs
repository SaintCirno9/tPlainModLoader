using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using TPML.Content.Fusion;
namespace WandsTool.Content.Structure
{
    /// <summary>
    /// 蓝图智能原材料合成与原子扣除引擎
    /// 缺成品材料时自动在背包中寻找配方与原材料，计算最优级联制造路径，实现免手工搓砖的无缝蓝图建造。
    /// 内置环路死锁保护（Cycle Guard）与严格递归深度控制，杜绝无限递归和内存泄露。
    /// </summary>
    public static class StructureCraftingEngine
    {
        /// <summary>
        /// 蓝图材料扣除与自动制造执行计划
        /// </summary>
        public class CraftingPlan
        {
            public bool IsPossible = false;

            /// <summary>直接消耗的成品物品（ItemID -> 数量）</summary>
            public Dictionary<int, int> DirectConsumes = new Dictionary<int, int>();

            /// <summary>扣除的原材料（ItemID -> 数量）</summary>
            public Dictionary<int, int> IngredientConsumes = new Dictionary<int, int>();

            /// <summary>自动制造出的成品汇总（ItemID -> 制造产出数量）</summary>
            public Dictionary<int, int> CraftedCounts = new Dictionary<int, int>();

            /// <summary>制造产出超出蓝图所需、需返还给玩家背包的余数（ItemID -> 数量）</summary>
            public Dictionary<int, int> RefundItems = new Dictionary<int, int>();

            /// <summary>不可行时的缺失详细说明</summary>
            public List<string> MissingMessages = new List<string>();

            public bool HasCrafting => CraftedCounts.Count > 0;
        }

        // 缓存配方产物反查索引（Result ItemID -> Recipes）
        private static Dictionary<int, List<Recipe>> _recipesByResult = null;
        private static int _lastRecipeCount = -1;

        /// <summary>
        /// 获取或初始化配方产物反查字典
        /// </summary>
        public static Dictionary<int, List<Recipe>> GetRecipesByResult()
        {
            if (_recipesByResult == null || _lastRecipeCount != Recipe.numRecipes)
            {
                _recipesByResult = new Dictionary<int, List<Recipe>>();
                for (int i = 0; i < Recipe.numRecipes; i++)
                {
                    Recipe recipe = Main.recipe[i];
                    if (recipe == null || recipe.createItem == null || recipe.createItem.type <= 0 || recipe.createItem.stack <= 0)
                        continue;

                    int resultId = recipe.createItem.type;
                    if (!_recipesByResult.TryGetValue(resultId, out var list))
                    {
                        list = new List<Recipe>();
                        _recipesByResult[resultId] = list;
                    }
                    list.Add(recipe);
                }
                _lastRecipeCount = Recipe.numRecipes;
            }
            return _recipesByResult;
        }

        /// <summary>
        /// 构建蓝图材料消耗与自动制造计划（纯虚拟模拟，不修改任何真实物品）
        public static CraftingPlan BuildPlan(StructureData data, Player player, bool allowAutoCraft = true, bool requireWorkstation = false, Point? originWorldTile = null, bool overwrite = true)
        {
            CraftingPlan plan = new CraftingPlan();
            if (data == null || player == null || player.inventory == null)
            {
                plan.IsPossible = true;
                return plan;
            }

            try
            {
                Dictionary<int, int> requiredItems = data.GetRequiredItems(originWorldTile, overwrite);
                if (requiredItems.Count == 0)
                {
                    plan.IsPossible = true;
                    return plan;
                }
                // 1. 抓取玩家背包虚拟存量镜像
                Dictionary<int, int> virtualStock = GetPlayerInventorySnapshot(player);

                // 2. 第一阶段：直接扣除已拥有的成品
                Dictionary<int, int> deficits = new Dictionary<int, int>();
                foreach (var kvp in requiredItems)
                {
                    int itemId = kvp.Key;
                    int needed = kvp.Value;

                    int owned = virtualStock.TryGetValue(itemId, out int count) ? count : 0;
                    int directTake = Math.Min(owned, needed);

                    if (directTake > 0)
                    {
                        plan.DirectConsumes[itemId] = directTake;
                        virtualStock[itemId] -= directTake;
                    }

                    int remainingNeeded = needed - directTake;
                    if (remainingNeeded > 0)
                    {
                        deficits[itemId] = remainingNeeded;
                    }
                }

                // 如果全部直接满足，无需合成，直接返回成功计划
                if (deficits.Count == 0)
                {
                    plan.IsPossible = true;
                    return plan;
                }

                // 3. 第二阶段：若不允许自动合成，直接统计缺失并返回失败
                if (!allowAutoCraft)
                {
                    foreach (var kvp in deficits)
                    {
                        string name = Lang.GetItemNameValue(kvp.Key);
                        plan.MissingMessages.Add($"{name} (缺 {kvp.Value})");
                    }
                    plan.IsPossible = false;
                    return plan;
                }

                // 4. 第三阶段：对缺口进行智能配方搜索与原材料扣除
                var recipeMap = GetRecipesByResult();

                foreach (var kvp in deficits)
                {
                    int targetItem = kvp.Key;
                    int targetNeeded = kvp.Value;

                    if (!recipeMap.TryGetValue(targetItem, out var recipes) || recipes.Count == 0)
                    {
                        // 无制作配方
                        string name = Lang.GetItemNameValue(targetItem);
                        plan.MissingMessages.Add($"{name} (缺 {targetNeeded}，且无制作配方)");
                        continue;
                    }

                    // 启动环路检测集（Visiting Set），防止石墙<->石块互逆配方导致无限递归
                    HashSet<int> visitingChain = new HashSet<int>();
                    bool resolved = TryResolveCrafting(targetItem, targetNeeded, recipes, virtualStock, player, requireWorkstation, plan, recipeMap, visitingChain, 0);
                    if (!resolved)
                    {
                        string name = Lang.GetItemNameValue(targetItem);
                        plan.MissingMessages.Add($"{name} (缺 {targetNeeded}，原材料不足)");
                    }
                }

                plan.IsPossible = plan.MissingMessages.Count == 0;
            }
            catch (Exception ex)
            {
                plan.IsPossible = false;
                plan.MissingMessages.Add($"合成模拟异常: {ex.Message}");
            }

            return plan;
        }

        /// <summary>
        /// 尝试为目标缺口寻找可用配方并从虚拟背包中扣除原材料（带严格深度限制与环路死锁保护）
        /// </summary>
        private static bool TryResolveCrafting(
            int targetItem,
            int targetNeeded,
            List<Recipe> recipes,
            Dictionary<int, int> virtualStock,
            Player player,
            bool requireWorkstation,
            CraftingPlan plan,
            Dictionary<int, List<Recipe>> recipeMap,
            HashSet<int> visitingChain,
            int depth)
        {
            // 严格深度保护：最多允许 1 级原料级联（例如 沙子->玻璃->玻璃墙，总层数=2），禁止深层递归
            if (depth > 1) return false;

            if (visitingChain == null) visitingChain = new HashSet<int>();
            // 检测到配方环路（如 A -> B -> A），立即阻断死循环！
            if (visitingChain.Contains(targetItem)) return false;

            visitingChain.Add(targetItem);

            try
            {
                // 筛选满足环境条件的配方，并过滤掉原料包含目标物自身的恶性循环配方
                List<Recipe> candidateRecipes = new List<Recipe>();
                foreach (Recipe r in recipes)
                {
                    if (r == null || r.createItem == null || r.createItem.stack <= 0) continue;
                    if (requireWorkstation && !r.PlayerMeetsEnvironmentConditions(player)) continue;

                    // 检查原料是否包含自身（如某些转化配方）
                    bool containsSelf = false;
                    for (int i = 0; i < Recipe.maxRequirements; i++)
                    {
                        Item req = r.requiredItem[i];
                        if (req != null && req.type == targetItem)
                        {
                            containsSelf = true;
                            break;
                        }
                    }
                    if (containsSelf) continue;

                    candidateRecipes.Add(r);
                }

                if (candidateRecipes.Count == 0) return false;

                // 1. 优先尝试单级直接合成（所有原料在背包内直接齐备）
                foreach (Recipe recipe in candidateRecipes)
                {
                    int craftTimes = (int)Math.Ceiling((double)targetNeeded / recipe.createItem.stack);
                    Dictionary<int, int> chosenIngredients = new Dictionary<int, int>();
                    bool canCraft = true;

                    for (int i = 0; i < Recipe.maxRequirements; i++)
                    {
                        Item req = recipe.requiredItem[i];
                        if (req == null || req.type <= 0 || req.stack <= 0) continue;

                        int totalReqCount = req.stack * craftTimes;
                        RecipeGroup group = FindRecipeGroupForIngredient(recipe, i, req.type);

                        if (group != null)
                        {
                            // 组配方：寻找存量最充足的组内有效物品
                            int bestItemId = 0;
                            int bestStock = 0;
                            foreach (int validId in group.ValidItems)
                            {
                                int currentStock = virtualStock.TryGetValue(validId, out int st) ? st : 0;
                                if (currentStock >= totalReqCount && currentStock > bestStock)
                                {
                                    bestItemId = validId;
                                    bestStock = currentStock;
                                }
                            }

                            if (bestItemId > 0)
                            {
                                chosenIngredients[bestItemId] = totalReqCount;
                            }
                            else
                            {
                                canCraft = false;
                                break;
                            }
                        }
                        else
                        {
                            // 具体特定物品
                            int currentStock = virtualStock.TryGetValue(req.type, out int st) ? st : 0;
                            if (currentStock >= totalReqCount)
                            {
                                chosenIngredients[req.type] = totalReqCount;
                            }
                            else
                            {
                                canCraft = false;
                                break;
                            }
                        }
                    }
                    if (canCraft)
                    {
                        // 满足单级合成！扣除原料并记录
                        foreach (var ing in chosenIngredients)
                        {
                            virtualStock[ing.Key] -= ing.Value;
                            if (plan.IngredientConsumes.ContainsKey(ing.Key)) plan.IngredientConsumes[ing.Key] += ing.Value;
                            else plan.IngredientConsumes[ing.Key] = ing.Value;
                        }

                        int totalProduced = craftTimes * recipe.createItem.stack;
                        if (plan.CraftedCounts.ContainsKey(targetItem)) plan.CraftedCounts[targetItem] += totalProduced;
                        else plan.CraftedCounts[targetItem] = totalProduced;

                        int refund = totalProduced - targetNeeded;
                        if (refund > 0)
                        {
                            if (plan.RefundItems.ContainsKey(targetItem)) plan.RefundItems[targetItem] += refund;
                            else plan.RefundItems[targetItem] = refund;
                        }

                        return true;
                    }
                }

                // 2. 尝试二级级联合成（仅在 depth == 0 时允许进入第 2 级）
                if (depth == 0)
                {
                    foreach (Recipe recipe in candidateRecipes)
                    {
                        int craftTimes = (int)Math.Ceiling((double)targetNeeded / recipe.createItem.stack);
                        Dictionary<int, int> stepChosenIngredients = new Dictionary<int, int>();
                        Dictionary<int, int> subCrafted = new Dictionary<int, int>();
                        bool cascadeSuccess = true;

                        // 备份当前虚拟背包以支持回滚
                        Dictionary<int, int> virtualStockBackup = new Dictionary<int, int>(virtualStock);

                        for (int i = 0; i < Recipe.maxRequirements; i++)
                        {
                            Item req = recipe.requiredItem[i];
                            if (req == null || req.type <= 0 || req.stack <= 0) continue;

                            int totalReqCount = req.stack * craftTimes;
                            RecipeGroup group = FindRecipeGroupForIngredient(recipe, i, req.type);

                            if (group != null)
                            {
                                int bestItemId = 0;
                                int bestStock = 0;
                                foreach (int validId in group.ValidItems)
                                {
                                    int currentStock = virtualStockBackup.TryGetValue(validId, out int gst) ? gst : 0;
                                    if (currentStock >= totalReqCount && currentStock > bestStock)
                                    {
                                        bestItemId = validId;
                                        bestStock = currentStock;
                                    }
                                }

                                if (bestItemId > 0)
                                {
                                    virtualStockBackup[bestItemId] -= totalReqCount;
                                    stepChosenIngredients[bestItemId] = totalReqCount;
                                    continue;
                                }
                            }
                            int directStock = virtualStockBackup.TryGetValue(req.type, out int dst) ? dst : 0;
                            if (directStock >= totalReqCount)
                            {
                                virtualStockBackup[req.type] -= totalReqCount;
                                stepChosenIngredients[req.type] = totalReqCount;
                            }
                            else
                            {
                                int subDeficit = totalReqCount - directStock;
                                if (directStock > 0)
                                {
                                    virtualStockBackup[req.type] -= directStock;
                                    stepChosenIngredients[req.type] = directStock;
                                }

                                if (recipeMap.TryGetValue(req.type, out var subRecipes) && subRecipes.Count > 0)
                                {
                                    CraftingPlan subPlan = new CraftingPlan();
                                    // 传递带当前 targetItem 的 visitingChain 副本，深度 +1
                                    HashSet<int> subVisitingChain = new HashSet<int>(visitingChain);
                                    bool subOk = TryResolveCrafting(req.type, subDeficit, subRecipes, virtualStockBackup, player, requireWorkstation, subPlan, recipeMap, subVisitingChain, depth + 1);
                                    if (subOk)
                                    {
                                        foreach (var ing in subPlan.IngredientConsumes)
                                        {
                                            if (stepChosenIngredients.ContainsKey(ing.Key)) stepChosenIngredients[ing.Key] += ing.Value;
                                            else stepChosenIngredients[ing.Key] = ing.Value;
                                        }
                                        foreach (var c in subPlan.CraftedCounts)
                                        {
                                            if (subCrafted.ContainsKey(c.Key)) subCrafted[c.Key] += c.Value;
                                            else subCrafted[c.Key] = c.Value;
                                        }
                                    }
                                    else
                                    {
                                        cascadeSuccess = false;
                                        break;
                                    }
                                }
                                else
                                {
                                    cascadeSuccess = false;
                                    break;
                                }
                            }
                        }
                        if (cascadeSuccess)
                        {
                            // 二级级联成功，提交虚拟背包状态
                            virtualStock.Clear();
                            foreach (var kv in virtualStockBackup) virtualStock[kv.Key] = kv.Value;

                            foreach (var ing in stepChosenIngredients)
                            {
                                if (plan.IngredientConsumes.ContainsKey(ing.Key)) plan.IngredientConsumes[ing.Key] += ing.Value;
                                else plan.IngredientConsumes[ing.Key] = ing.Value;
                            }
                            foreach (var c in subCrafted)
                            {
                                if (plan.CraftedCounts.ContainsKey(c.Key)) plan.CraftedCounts[c.Key] += c.Value;
                                else plan.CraftedCounts[c.Key] = c.Value;
                            }

                            int totalProduced = craftTimes * recipe.createItem.stack;
                            if (plan.CraftedCounts.ContainsKey(targetItem)) plan.CraftedCounts[targetItem] += totalProduced;
                            else plan.CraftedCounts[targetItem] = totalProduced;

                            int refund = totalProduced - targetNeeded;
                            if (refund > 0)
                            {
                                if (plan.RefundItems.ContainsKey(targetItem)) plan.RefundItems[targetItem] += refund;
                                else plan.RefundItems[targetItem] = refund;
                            }

                            return true;
                        }
                    }
                }
            }
            finally
            {
                visitingChain.Remove(targetItem);
            }

            return false;
        }
        /// <summary>
        /// 识别某个原料是否绑定了 RecipeGroup（支持 QuickLookup 与 AcceptedGroups 兼容判定）
        /// </summary>
        private static RecipeGroup FindRecipeGroupForIngredient(Recipe recipe, int ingredientIndex, int itemType)
        {
            if (recipe == null) return null;

            if (recipe.requiredItemQuickLookup != null && ingredientIndex < recipe.requiredItemQuickLookup.Length)
            {
                var quick = recipe.requiredItemQuickLookup[ingredientIndex];
                if (quick.IsRecipeGroup && quick.RecipeGroup != null)
                {
                    return quick.RecipeGroup;
                }
            }

            if (recipe.acceptedGroups != null)
            {
                foreach (int groupIndex in recipe.acceptedGroups)
                {
                    if (groupIndex < 0) break;
                    if (RecipeGroup.recipeGroups.TryGetValue(groupIndex, out var group))
                    {
                        if (group.ValidItems.Contains(itemType)) return group;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 原子执行材料扣减与余数返还
        /// </summary>
        public static void ExecutePlan(CraftingPlan plan, Player player)
        {
            if (plan == null || !plan.IsPossible || player?.inventory == null) return;

            try
            {
                // 1. 扣除直接成品物品
                foreach (var kvp in plan.DirectConsumes)
                {
                    ConsumeItemFromPlayer(player, kvp.Key, kvp.Value);
                }

                // 2. 扣除自动制造原材料
                foreach (var kvp in plan.IngredientConsumes)
                {
                    ConsumeItemFromPlayer(player, kvp.Key, kvp.Value);
                }

                // 3. 返还多余制造产物（余数）
                foreach (var kvp in plan.RefundItems)
                {
                    int itemId = kvp.Key;
                    int count = kvp.Value;
                    if (count <= 0) continue;

                    Item refundItem = new Item();
                    refundItem.SetDefaults(itemId);
                    refundItem.stack = count;
                    Item leftOver = player.GetItem(refundItem, GetItemSettings.PickupItemFromWorld);
                    if (leftOver != null && !leftOver.IsAir && leftOver.stack > 0)
                    {
                        player.QuickSpawnItem(player.GetItemSource_InventoryOverflow(), leftOver.type, leftOver.stack);
                    }
                }

                // 4. 弹出多余合成产物返还通知
                if (plan.RefundItems.Count > 0)
                {
                    List<string> refundSummary = new List<string>();
                    foreach (var kvp in plan.RefundItems)
                    {
                        if (kvp.Value > 0)
                        {
                            string name = Lang.GetItemNameValue(kvp.Key);
                            refundSummary.Add($"{kvp.Value}×{name}");
                        }
                    }
                    if (refundSummary.Count > 0)
                    {
                        string refundText = string.Join("，", refundSummary);
                        Main.NewText($"[魔杖] 多余合成产物已存入背包: {refundText}", 100, 220, 255);
                    }
                }

                // 5. 弹出自动制造通知
                if (plan.CraftedCounts.Count > 0)
                {
                    List<string> craftedSummary = new List<string>();
                    foreach (var kvp in plan.CraftedCounts)
                    {
                        string name = Lang.GetItemNameValue(kvp.Key);
                        craftedSummary.Add($"{kvp.Value}×{name}");
                    }
                    string summaryText = string.Join("，", craftedSummary);
                    Main.NewText($"[魔杖] 自动消耗原材料制造: {summaryText}", 120, 255, 150);
                }
            }
            catch (Exception ex)
            {
                Main.NewText($"[魔杖] 扣除材料时发生异常: {ex.Message}", 255, 100, 100);
            }
        }

        /// <summary>
        /// 从玩家背包及外部融合容器（如大背包）中安全扣除指定数量的某种物品（支持跨槽位与跨容器堆叠拆分扣除）
        /// </summary>
        private static void ConsumeItemFromPlayer(Player player, int itemId, int count)
        {
            if (player?.inventory == null || itemId <= 0 || count <= 0) return;

            int remaining = count;

            // 1. 优先从原版主背包扣除 (0~57格)
            for (int i = 0; i < 58 && remaining > 0; i++)
            {
                Item item = player.inventory[i];
                if (item != null && item.type == itemId && item.stack > 0)
                {
                    int take = Math.Min(item.stack, remaining);
                    item.stack -= take;
                    remaining -= take;
                    if (item.stack <= 0) item.TurnToAir();
                }
            }

            // 2. 主背包不足时，从框架级外部融合源（大背包等）中安全扣除
            if (remaining > 0)
            {
                var sources = InventoryFusionManager.GetActiveSources(player);
                for (int s = 0; s < sources.Count && remaining > 0; s++)
                {
                    var src = sources[s];
                    Item[] slots = src.GetSlots(player);
                    if (slots == null || slots.Length == 0) continue;

                    bool modified = false;
                    for (int i = 0; i < slots.Length && remaining > 0; i++)
                    {
                        Item item = slots[i];
                        if (item != null && !item.IsAir && item.type == itemId && item.stack > 0)
                        {
                            int take = Math.Min(item.stack, remaining);
                            item.stack -= take;
                            remaining -= take;
                            if (item.stack <= 0)
                            {
                                slots[i] = new Item();
                            }
                            modified = true;
                        }
                    }

                    if (modified)
                    {
                        src.OnModified(player);
                    }
                }
            }
        }

        /// <summary>
        /// 抓取玩家背包（0-57号槽位）及所有激活的外部融合容器（如大背包）物品总数快照
        /// </summary>
        public static Dictionary<int, int> GetPlayerInventorySnapshot(Player player)
        {
            Dictionary<int, int> stock = new Dictionary<int, int>();
            if (player?.inventory == null) return stock;

            // 1. 扫描原版主背包 (0-57格)
            for (int i = 0; i < 58; i++)
            {
                Item item = player.inventory[i];
                if (item != null && item.type > 0 && item.stack > 0)
                {
                    if (stock.ContainsKey(item.type)) stock[item.type] += item.stack;
                    else stock[item.type] = item.stack;
                }
            }

            // 2. 扫描框架级外部融合物品源（如大背包等）
            var sources = InventoryFusionManager.GetActiveSources(player);
            for (int s = 0; s < sources.Count; s++)
            {
                var src = sources[s];
                Item[] slots = src.GetSlots(player);
                if (slots == null) continue;

                for (int i = 0; i < slots.Length; i++)
                {
                    Item item = slots[i];
                    if (item != null && !item.IsAir && item.type > 0 && item.stack > 0)
                    {
                        if (stock.ContainsKey(item.type)) stock[item.type] += item.stack;
                        else stock[item.type] = item.stack;
                    }
                }
            }

            return stock;
        }

        /// <summary>
        /// 计算玩家背包及外部融合容器状态的快速指纹哈希（用于 Tooltip 缓存避免高频重算）
        /// </summary>
        public static int GetInventoryHash(Player player)
        {
            if (player?.inventory == null) return 0;
            unchecked
            {
                int hash = 17;

                // 1. 主背包哈希
                for (int i = 0; i < 58; i++)
                {
                    Item item = player.inventory[i];
                    if (item != null && item.type > 0 && item.stack > 0)
                    {
                        hash = hash * 31 + item.type;
                        hash = hash * 31 + item.stack;
                    }
                }

                // 2. 外部融合源哈希
                var sources = InventoryFusionManager.GetActiveSources(player);
                for (int s = 0; s < sources.Count; s++)
                {
                    var src = sources[s];
                    Item[] slots = src.GetSlots(player);
                    if (slots == null) continue;

                    for (int i = 0; i < slots.Length; i++)
                    {
                        Item item = slots[i];
                        if (item != null && !item.IsAir && item.type > 0 && item.stack > 0)
                        {
                            hash = hash * 31 + item.type;
                            hash = hash * 31 + item.stack;
                        }
                    }
                }

                return hash;
            }
        }
    }
}
