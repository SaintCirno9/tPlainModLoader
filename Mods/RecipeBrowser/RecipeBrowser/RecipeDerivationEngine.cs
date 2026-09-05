using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace RecipeBrowser
{
    /// <summary>
    /// 配方下游正向派生链计算引擎
    /// 自动构建物品/配方组到下游配方的拓扑图，支持全深度广度优先搜索 (BFS)，
    /// 计算从查询材料出发能产出的一级、二级及多级衍生产物配方。
    /// 作者: SaintCirno9
    /// </summary>
    public static class RecipeDerivationEngine
    {
        public class DerivationInfo
        {
            public int RecipeIndex { get; set; }
            public int Depth { get; set; } // 0 = 产物自身, 1 = 直接产物, 2 = 二级衍生...
            public List<int> Path { get; set; } // [root, intermediate..., product]
        }

        private static bool _initialized;
        private static int _lastRecipeCount;

        // 索引结构：
        // 材料物品 ID -> 配方索引列表
        private static readonly Dictionary<int, List<int>> _recipesByIngredient = new Dictionary<int, List<int>>();
        // 配方组 ID -> 配方索引列表
        private static readonly Dictionary<int, List<int>> _recipesByGroup = new Dictionary<int, List<int>>();
        // 物品 ID -> 该物品所属的配方组 ID 列表
        private static readonly Dictionary<int, List<int>> _itemToGroups = new Dictionary<int, List<int>>();
        // 产物物品 ID -> 该配方索引列表
        private static readonly Dictionary<int, List<int>> _recipesByResult = new Dictionary<int, List<int>>();

        public static void EnsureInitialized()
        {
            if (_initialized && _lastRecipeCount == Recipe.numRecipes) return;

            _recipesByIngredient.Clear();
            _recipesByGroup.Clear();
            _itemToGroups.Clear();
            _recipesByResult.Clear();

            // 1. 建立物品 -> 配方组映射
            foreach (var kvp in RecipeGroup.recipeGroups)
            {
                int groupId = kvp.Key;
                RecipeGroup group = kvp.Value;
                if (group?.ValidItems == null) continue;

                foreach (int itemType in group.ValidItems)
                {
                    if (!_itemToGroups.TryGetValue(itemType, out var list))
                    {
                        _itemToGroups[itemType] = list = new List<int>();
                    }
                    if (!list.Contains(groupId)) list.Add(groupId);
                }
            }

            // 2. 遍历所有配方建立索引
            for (int rIndex = 0; rIndex < Recipe.numRecipes; rIndex++)
            {
                Recipe r = Main.recipe[rIndex];
                if (r == null || r.createItem == null || r.createItem.IsAir || r.createItem.type <= 0) continue;

                int productType = r.createItem.type;
                if (!_recipesByResult.TryGetValue(productType, out var resList))
                {
                    _recipesByResult[productType] = resList = new List<int>();
                }
                resList.Add(rIndex);

                // 索引原料
                if (r.requiredItem != null)
                {
                    for (int i = 0; i < r.requiredItem.Length; i++)
                    {
                        Item ing = r.requiredItem[i];
                        if (ing == null || ing.IsAir || ing.type <= 0) continue;

                        if (!_recipesByIngredient.TryGetValue(ing.type, out var ingList))
                        {
                            _recipesByIngredient[ing.type] = ingList = new List<int>();
                        }
                        if (!ingList.Contains(rIndex)) ingList.Add(rIndex);
                    }
                }

                // 索引配方组
                if (r.acceptedGroups != null)
                {
                    foreach (int gId in r.acceptedGroups)
                    {
                        if (!_recipesByGroup.TryGetValue(gId, out var gList))
                        {
                            _recipesByGroup[gId] = gList = new List<int>();
                        }
                        if (!gList.Contains(rIndex)) gList.Add(rIndex);
                    }
                }
            }

            _lastRecipeCount = Recipe.numRecipes;
            _initialized = true;
        }

        /// <summary>
        /// 广度优先搜索 (BFS) 计算从 rootItemType 出发的所有下游衍生配方
        /// </summary>
        public static Dictionary<int, DerivationInfo> ComputeDownstreamRecipes(int rootItemType, int maxDepth = 10)
        {
            EnsureInitialized();

            var results = new Dictionary<int, DerivationInfo>();
            if (rootItemType <= 0) return results;

            // Tier 0: 产物本身就是 rootItemType 的配方（如何制作此物）
            if (_recipesByResult.TryGetValue(rootItemType, out var selfRecipes))
            {
                foreach (int rIndex in selfRecipes)
                {
                    results[rIndex] = new DerivationInfo
                    {
                        RecipeIndex = rIndex,
                        Depth = 0,
                        Path = new List<int> { rootItemType }
                    };
                }
            }

            // BFS 队列与防环集合
            var queue = new Queue<(int itemType, int depth, List<int> path)>();
            var visitedItems = new HashSet<int>();

            visitedItems.Add(rootItemType);
            queue.Enqueue((rootItemType, 1, new List<int> { rootItemType }));

            while (queue.Count > 0)
            {
                var (curItem, curDepth, curPath) = queue.Dequeue();
                if (curDepth > maxDepth) continue;

                // 收集使用 curItem 作为原料的所有候选配方
                var candidateRecipes = new HashSet<int>();

                if (_recipesByIngredient.TryGetValue(curItem, out var directList))
                {
                    foreach (int rIdx in directList) candidateRecipes.Add(rIdx);
                }

                if (_itemToGroups.TryGetValue(curItem, out var groupIds))
                {
                    foreach (int gId in groupIds)
                    {
                        if (_recipesByGroup.TryGetValue(gId, out var gList))
                        {
                            foreach (int rIdx in gList) candidateRecipes.Add(rIdx);
                        }
                    }
                }

                // 遍历候选配方，产出下一步物品
                foreach (int rIndex in candidateRecipes)
                {
                    Recipe r = Main.recipe[rIndex];
                    if (r == null || r.createItem == null || r.createItem.IsAir || r.createItem.type <= 0) continue;

                    int productType = r.createItem.type;

                    // 记录或更新配方的最短衍生深度与链路
                    List<int> nextPath = new List<int>(curPath) { productType };
                    if (!results.TryGetValue(rIndex, out var existing) || curDepth < existing.Depth)
                    {
                        results[rIndex] = new DerivationInfo
                        {
                            RecipeIndex = rIndex,
                            Depth = curDepth,
                            Path = nextPath
                        };
                    }

                    // 若该产物未被作为原料访问过，入队进行下一级推导
                    if (!visitedItems.Contains(productType))
                    {
                        visitedItems.Add(productType);
                        queue.Enqueue((productType, curDepth + 1, nextPath));
                    }
                }
            }

            return results;
        }
    }
}
