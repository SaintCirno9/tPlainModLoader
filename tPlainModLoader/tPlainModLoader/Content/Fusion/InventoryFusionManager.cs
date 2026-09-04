using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using TPML.Core.Diagnostics;

using TPML.Core.Logging;

namespace TPML.Content.Fusion
{
    /// <summary>
    /// 通用外部背包融合管理器与调度中心。<br/>
    /// 统一调度所有已注册的 <see cref="IFusionItemSource"/>，对外提供标准化的背包聚合查询、数量统计、自动扣除与匹配检索。
    /// 作者: SaintCirno9
    /// </summary>
    public static class InventoryFusionManager
    {
        private static readonly ILogger Logger = LogManager.GetLogger("FusionManager");
        private static readonly List<IFusionItemSource> _sources = new List<IFusionItemSource>();
        private static readonly object _lock = new object();

        /// <summary>
        /// 注册一个外部物品源
        /// </summary>
        /// <param name="source">物品源提供者</param>
        public static void RegisterSource(IFusionItemSource source)
        {
            if (source == null) return;
            lock (_lock)
            {
                _sources.RemoveAll(s => s.Id.Equals(source.Id, StringComparison.OrdinalIgnoreCase));
                _sources.Add(source);
                _sources.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                Logger.Info($"[FusionManager] 成功注册融合源 [{source.Id}], 优先级={source.Priority}, 允许制作={source.AllowCrafting}, 当前总源数={_sources.Count}");
            }
        }

        /// <summary>
        /// 注销指定标识符的物品源
        /// </summary>
        /// <param name="id">物品源唯一标识符</param>
        public static void UnregisterSource(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            lock (_lock)
            {
                int removed = _sources.RemoveAll(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
                if (removed > 0)
                {
                    Logger.Info($"[FusionManager] 注销融合源 [{id}], 剩余总源数={_sources.Count}");
                }
            }
        }

        /// <summary>
        /// 清理所有已注册的物品源（在模组重载或卸载时由框架调用）
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                int count = _sources.Count;
                _sources.Clear();
                if (count > 0)
                {
                    Logger.Info($"[FusionManager] 清空所有融合源 ({count} 个)");
                }
            }
        }

        /// <summary>
        /// 获取当前针对该玩家处于激活状态的所有物品源快照
        /// </summary>
        public static List<IFusionItemSource> GetActiveSources(Player player)
        {
            if (player == null || !player.active) return new List<IFusionItemSource>();

            lock (_lock)
            {
                var list = new List<IFusionItemSource>(_sources.Count);
                for (int i = 0; i < _sources.Count; i++)
                {
                    var src = _sources[i];
                    if (src != null && src.IsActive(player))
                    {
                        list.Add(src);
                    }
                }
                return list;
            }
        }

        #region 聚合查询与统计

        /// <summary>
        /// 检查所有外部物品源中是否存在指定类型的物品
        /// </summary>
        public static bool HasItem(Player player, int type)
        {
            using (PerformanceProfiler.Measure("Fusion", "InventoryFusionManager.HasItem"))
            {
                var sources = GetActiveSources(player);
                for (int s = 0; s < sources.Count; s++)
                {
                    var src = sources[s];
                    Item[] slots = src.GetSlots(player);
                    if (slots == null) continue;

                    for (int i = 0; i < slots.Length; i++)
                    {
                        Item it = slots[i];
                        if (it != null && !it.IsAir && it.type == type && it.stack > 0)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// 统计所有外部物品源中指定类型物品的总堆叠数量
        /// </summary>
        public static int CountItem(Player player, int type, int stopCountingAt = 0)
        {
            using (PerformanceProfiler.Measure("Fusion", "InventoryFusionManager.CountItem"))
            {
                int total = 0;
                var sources = GetActiveSources(player);
                for (int s = 0; s < sources.Count; s++)
                {
                    var src = sources[s];
                    Item[] slots = src.GetSlots(player);
                    if (slots == null) continue;

                    for (int i = 0; i < slots.Length; i++)
                    {
                        Item it = slots[i];
                        if (it != null && !it.IsAir && it.type == type && it.stack > 0)
                        {
                            total += it.stack;
                            if (stopCountingAt > 0 && total >= stopCountingAt)
                            {
                                return total;
                            }
                        }
                    }
                }
                return total;
            }
        }

        /// <summary>
        /// 获取所有外部激活物品源中的全部非空物品列表
        /// </summary>
        public static List<Item> GetAllFusionItems(Player player)
        {
            List<Item> result = new List<Item>();
            var sources = GetActiveSources(player);
            for (int s = 0; s < sources.Count; s++)
            {
                var src = sources[s];
                Item[] slots = src.GetSlots(player);
                if (slots == null) continue;

                for (int i = 0; i < slots.Length; i++)
                {
                    Item it = slots[i];
                    if (it != null && !it.IsAir && it.stack > 0)
                    {
                        result.Add(it);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 检查所有外部物品源中是否存在符合条件的物品
        /// </summary>
        public static bool HasMatchingItem(Player player, Func<Item, bool> predicate)
        {
            if (predicate == null) return false;
            var sources = GetActiveSources(player);
            for (int s = 0; s < sources.Count; s++)
            {
                var src = sources[s];
                Item[] slots = src.GetSlots(player);
                if (slots == null) continue;

                for (int i = 0; i < slots.Length; i++)
                {
                    Item it = slots[i];
                    if (it != null && !it.IsAir && it.stack > 0 && predicate(it))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 查找首个符合条件的物品实例及其所属的数据源
        /// </summary>
        public static Item FindMatchingItem(Player player, Func<Item, bool> predicate, out IFusionItemSource matchedSource)
        {
            matchedSource = null;
            if (predicate == null) return null;

            using (PerformanceProfiler.Measure("Fusion", "InventoryFusionManager.FindMatchingItem"))
            {
                var sources = GetActiveSources(player);
                for (int s = 0; s < sources.Count; s++)
                {
                    var src = sources[s];
                    Item[] slots = src.GetSlots(player);
                    if (slots == null) continue;

                    for (int i = 0; i < slots.Length; i++)
                    {
                        Item it = slots[i];
                        if (it != null && !it.IsAir && it.stack > 0 && predicate(it))
                        {
                            matchedSource = src;
                            return it;
                        }
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// 收集所有激活且允许参与制作的外部物品源中的未收藏物品（用于原版制作系统 Recipe._ownedItems 统计）
        /// </summary>
        public static void CollectUnfavoritedItems(Player player, Action<int, int> onAdd)
        {
            if (onAdd == null) return;

            using (PerformanceProfiler.Measure("Fusion", "InventoryFusionManager.CollectUnfavoritedItems"))
            {
                var sources = GetActiveSources(player);

                for (int s = 0; s < sources.Count; s++)
                {
                    var src = sources[s];
                    if (!src.AllowCrafting) continue;
                    Item[] slots = src.GetSlots(player);
                    if (slots == null) continue;

                    for (int i = 0; i < slots.Length; i++)
                    {
                        Item it = slots[i];
                        if (it != null && !it.IsAir && it.stack > 0 && !it.favorited)
                        {
                            onAdd(it.type, it.stack);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 统计所有激活且允许制作的外部物品源中未被收藏（!favorited）的指定类型物品总数
        /// </summary>
        public static int CountUnfavoritedItem(Player player, int type, int stopCountingAt = 0)
        {
            using (PerformanceProfiler.Measure("Fusion", "InventoryFusionManager.CountUnfavoritedItem"))
            {
                int total = 0;
                var sources = GetActiveSources(player);
                for (int s = 0; s < sources.Count; s++)
                {
                    var src = sources[s];
                    if (!src.AllowCrafting) continue;
                    Item[] slots = src.GetSlots(player);
                    if (slots == null) continue;

                    for (int i = 0; i < slots.Length; i++)
                    {
                        Item it = slots[i];
                        if (it != null && !it.IsAir && it.type == type && it.stack > 0 && !it.favorited)
                        {
                            total += it.stack;
                            if (stopCountingAt > 0 && total >= stopCountingAt)
                            {
                                return total;
                            }
                        }
                    }
                }
                return total;
            }
        }

        /// <summary>
        /// 统计所有激活且允许制作的外部物品源中未被收藏（!favorited）且满足匹配条件的物品总数（如配方组匹配）
        /// </summary>
        public static int CountUnfavoritedMatching(Player player, Func<int, bool> typePredicate, int stopCountingAt = 0)
        {
            if (typePredicate == null) return 0;

            using (PerformanceProfiler.Measure("Fusion", "InventoryFusionManager.CountUnfavoritedMatching"))
            {
                int total = 0;
                var sources = GetActiveSources(player);
                for (int s = 0; s < sources.Count; s++)
                {
                    var src = sources[s];
                    if (!src.AllowCrafting) continue;
                    Item[] slots = src.GetSlots(player);
                    if (slots == null) continue;

                    for (int i = 0; i < slots.Length; i++)
                    {
                        Item it = slots[i];
                        if (it != null && !it.IsAir && it.stack > 0 && !it.favorited && typePredicate(it.type))
                        {
                            total += it.stack;
                            if (stopCountingAt > 0 && total >= stopCountingAt)
                            {
                                return total;
                            }
                        }
                    }
                }
                return total;
            }
        }

        #endregion

        #region 聚合消耗

        /// <summary>
        /// 从外部物品源中消耗指定类型的 1 个物品
        /// </summary>
        /// <param name="player">目标玩家实体</param>
        /// <param name="type">物品类型 ID</param>
        /// <param name="reverseOrder">是否逆序遍历消耗</param>
        /// <returns>若成功扣除则返回 true</returns>
        public static bool ConsumeItem(Player player, int type, bool reverseOrder = false)
        {
            using (PerformanceProfiler.Measure("Fusion", "InventoryFusionManager.ConsumeItem"))
            {
                var sources = GetActiveSources(player);
                int srcStart = reverseOrder ? sources.Count - 1 : 0;
                int srcEnd = reverseOrder ? -1 : sources.Count;
                int srcStep = reverseOrder ? -1 : 1;

                for (int s = srcStart; s != srcEnd; s += srcStep)
                {
                    var src = sources[s];
                    Item[] slots = src.GetSlots(player);
                    if (slots == null || slots.Length == 0) continue;

                    int start = reverseOrder ? slots.Length - 1 : 0;
                    int end = reverseOrder ? -1 : slots.Length;
                    int step = reverseOrder ? -1 : 1;

                    for (int i = start; i != end; i += step)
                    {
                        Item it = slots[i];
                        if (it != null && !it.IsAir && it.type == type && it.stack > 0)
                        {
                            it.stack--;
                            if (it.stack <= 0)
                            {
                                it.TurnToAir();
                                slots[i] = new Item();
                            }

                            src.OnModified(player);
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// 从外部物品源中消耗指定数量的未收藏物品（用于制作系统）
        /// </summary>
        /// <param name="player">目标玩家实体</param>
        /// <param name="type">物品类型 ID</param>
        /// <param name="amount">需消耗的数量</param>
        /// <param name="reverseOrder">是否逆序遍历消耗</param>
        /// <returns>实际扣除的物品数量</returns>
        public static int ConsumeUnfavoritedItem(Player player, int type, int amount, bool reverseOrder = false)
        {
            if (amount <= 0) return 0;

            using (PerformanceProfiler.Measure("Fusion", "InventoryFusionManager.ConsumeUnfavoritedItem"))
            {
                int remaining = amount;
                var sources = GetActiveSources(player);
                int srcStart = reverseOrder ? sources.Count - 1 : 0;
                int srcEnd = reverseOrder ? -1 : sources.Count;
                int srcStep = reverseOrder ? -1 : 1;

                for (int s = srcStart; s != srcEnd; s += srcStep)
                {
                    var src = sources[s];
                    if (!src.AllowCrafting) continue;
                    Item[] slots = src.GetSlots(player);
                    if (slots == null || slots.Length == 0) continue;

                    bool modified = false;
                    int start = reverseOrder ? slots.Length - 1 : 0;
                    int end = reverseOrder ? -1 : slots.Length;
                    int step = reverseOrder ? -1 : 1;

                    for (int i = start; i != end; i += step)
                    {
                        Item it = slots[i];
                        if (it != null && !it.IsAir && it.type == type && it.stack > 0 && !it.favorited)
                        {
                            int take = Math.Min(it.stack, remaining);
                            it.stack -= take;
                            remaining -= take;
                            modified = true;

                            if (it.stack <= 0)
                            {
                                it.TurnToAir();
                                slots[i] = new Item();
                            }

                            if (remaining <= 0) break;
                        }
                    }

                    if (modified)
                    {
                        src.OnModified(player);
                    }

                    if (remaining <= 0) break;
                }

                return amount - remaining;
            }
        }

        /// <summary>
        /// 从外部物品源中根据类型匹配器（如配方组）消耗指定数量的未收藏物品（用于制作系统）
        /// </summary>
        public static int ConsumeUnfavoritedMatching(Player player, Func<int, bool> typePredicate, int amount, bool reverseOrder = false)
        {
            if (amount <= 0 || typePredicate == null) return 0;

            using (PerformanceProfiler.Measure("Fusion", "InventoryFusionManager.ConsumeUnfavoritedMatching"))
            {
                int remaining = amount;
                var sources = GetActiveSources(player);
                int srcStart = reverseOrder ? sources.Count - 1 : 0;
                int srcEnd = reverseOrder ? -1 : sources.Count;
                int srcStep = reverseOrder ? -1 : 1;

                for (int s = srcStart; s != srcEnd; s += srcStep)
                {
                    var src = sources[s];
                    if (!src.AllowCrafting) continue;
                    Item[] slots = src.GetSlots(player);
                    if (slots == null || slots.Length == 0) continue;

                    bool modified = false;
                    int start = reverseOrder ? slots.Length - 1 : 0;
                    int end = reverseOrder ? -1 : slots.Length;
                    int step = reverseOrder ? -1 : 1;

                    for (int i = start; i != end; i += step)
                    {
                        Item it = slots[i];
                        if (it != null && !it.IsAir && it.stack > 0 && !it.favorited && typePredicate(it.type))
                        {
                            int take = Math.Min(it.stack, remaining);
                            it.stack -= take;
                            remaining -= take;
                            modified = true;

                            if (it.stack <= 0)
                            {
                                it.TurnToAir();
                                slots[i] = new Item();
                            }

                            if (remaining <= 0) break;
                        }
                    }

                    if (modified)
                    {
                        src.OnModified(player);
                    }

                    if (remaining <= 0) break;
                }

                return amount - remaining;
            }
        }

        /// <summary>
        /// 从外部物品源中消耗符合条件的 1 个物品
        /// </summary>
        public static bool ConsumeMatchingItem(Player player, Func<Item, bool> predicate, bool reverseOrder = false)
        {
            if (predicate == null) return false;

            using (PerformanceProfiler.Measure("Fusion", "InventoryFusionManager.ConsumeMatchingItem"))
            {
                var sources = GetActiveSources(player);
                int srcStart = reverseOrder ? sources.Count - 1 : 0;
                int srcEnd = reverseOrder ? -1 : sources.Count;
                int srcStep = reverseOrder ? -1 : 1;

                for (int s = srcStart; s != srcEnd; s += srcStep)
                {
                    var src = sources[s];
                    Item[] slots = src.GetSlots(player);
                    if (slots == null || slots.Length == 0) continue;

                    int start = reverseOrder ? slots.Length - 1 : 0;
                    int end = reverseOrder ? -1 : slots.Length;
                    int step = reverseOrder ? -1 : 1;

                    for (int i = start; i != end; i += step)
                    {
                        Item it = slots[i];
                        if (it != null && !it.IsAir && it.stack > 0 && predicate(it))
                        {
                            it.stack--;
                            if (it.stack <= 0)
                            {
                                it.TurnToAir();
                                slots[i] = new Item();
                            }

                            src.OnModified(player);
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// 触发所有处于激活状态的数据源的变动保存通知
        /// </summary>
        public static void NotifyAllActiveModified(Player player)
        {
            var sources = GetActiveSources(player);
            for (int i = 0; i < sources.Count; i++)
            {
                sources[i]?.OnModified(player);
            }
        }

        #endregion
    }
}
