using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace TPML.Content.Fusion
{
    /// <summary>
    /// 通用外部背包融合管理器与调度中心。<br/>
    /// 统一调度所有已注册的 <see cref="IFusionItemSource"/>，对外提供标准化的背包聚合查询、数量统计、自动扣除与匹配检索。
    /// 作者: SaintCirno9
    /// </summary>
    public static class InventoryFusionManager
    {
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
                _sources.RemoveAll(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// 清理所有已注册的物品源（在模组重载或卸载时由框架调用）
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _sources.Clear();
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

        /// <summary>
        /// 统计所有外部物品源中指定类型物品的总堆叠数量
        /// </summary>
        public static int CountItem(Player player, int type, int stopCountingAt = 0)
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
                            slots[i] = new Item();
                        }

                        src.OnModified(player);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 从外部物品源中消耗符合条件的 1 个物品
        /// </summary>
        public static bool ConsumeMatchingItem(Player player, Func<Item, bool> predicate, bool reverseOrder = false)
        {
            if (predicate == null) return false;

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
                            slots[i] = new Item();
                        }

                        src.OnModified(player);
                        return true;
                    }
                }
            }
            return false;
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
