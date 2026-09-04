using System;
using System.Collections.Generic;
using Terraria;
using TPML.Core.Pinyin;

namespace MapAtlasTool.Content
{
    /// <summary>
    /// 箱子搜索命中条目
    /// </summary>
    internal struct AtlasChestHit
    {
        public int ChestIndex;
        public int X;
        public int Y;
        public string MatchText;
    }

    /// <summary>
    /// 世界箱子物品索引与搜索
    /// 单机世界加载后 Main.chest 全部物品常驻内存; 多人纯客户端仅打开/同步过的箱子有物品数据,
    /// 因此统计口径为 "已索引 N / 总数 M"。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class ChestItemIndex
    {
        private static readonly object _lock = new object();
        private static Dictionary<int, List<int>> _itemToChests = new Dictionary<int, List<int>>();
        private static int _indexedChests = 0;
        private static int _totalChests = 0;

        /// <summary>
        /// 构建物品类型 → 箱子索引映射（后台线程调用, 传入主线程快照）
        /// </summary>
        public static void Build(Chest[] chestSnapshot)
        {
            Dictionary<int, List<int>> dict = new Dictionary<int, List<int>>();
            int indexed = 0, total = 0;

            if (chestSnapshot != null)
            {
                for (int i = 0; i < chestSnapshot.Length; i++)
                {
                    Chest c = chestSnapshot[i];
                    if (c == null || c.x <= 0 || c.y <= 0) continue;

                    total++;
                    bool hasData = false;
                    for (int j = 0; j < c.item.Length; j++)
                    {
                        Item it = c.item[j];
                        if (it == null || it.IsAir || it.stack <= 0) continue;

                        hasData = true;
                        if (!dict.TryGetValue(it.type, out List<int> list))
                        {
                            list = new List<int>();
                            dict[it.type] = list;
                        }
                        if (!list.Contains(i)) list.Add(i);
                    }
                    if (hasData) indexed++;
                }
            }

            lock (_lock)
            {
                _itemToChests = dict;
                _indexedChests = indexed;
                _totalChests = total;
            }
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _itemToChests = new Dictionary<int, List<int>>();
                _indexedChests = 0;
                _totalChests = 0;
            }
        }

        /// <summary>已索引(有物品数据)箱子数 / 世界有效箱子总数</summary>
        public static (int indexed, int total) GetStats()
        {
            lock (_lock)
            {
                return (_indexedChests, _totalChests);
            }
        }

        /// <summary>
        /// 统一箱子搜索（主线程调用）：箱内物品名(中文/拼音/ID)命中 + 箱子自定义名命中。
        /// 结果对每个箱子做实时内容校验, 防止索引陈旧。
        /// </summary>
        public static List<AtlasChestHit> Query(string query)
        {
            Dictionary<int, string> hits = new Dictionary<int, string>();
            bool numeric = int.TryParse(query, out int queryId);

            // 1. 箱内物品名命中（基于索引去重后的类型集合）
            List<KeyValuePair<int, List<int>>> typeSnapshot;
            lock (_lock)
            {
                typeSnapshot = new List<KeyValuePair<int, List<int>>>(_itemToChests.Count);
                foreach (KeyValuePair<int, List<int>> kv in _itemToChests)
                {
                    typeSnapshot.Add(new KeyValuePair<int, List<int>>(kv.Key, kv.Value));
                }
            }

            foreach (KeyValuePair<int, List<int>> kv in typeSnapshot)
            {
                string itemName = Lang.GetItemNameValue(kv.Key);
                bool hit = !string.IsNullOrEmpty(itemName) && PinyinHelper.Matches(itemName, query);
                if (!hit && numeric && kv.Key == queryId) hit = true;
                if (!hit) continue;

                foreach (int chestIndex in kv.Value)
                {
                    if (hits.ContainsKey(chestIndex)) continue;
                    Chest c = Main.chest[chestIndex];
                    if (c == null) continue;
                    if (ChestContainsItemType(c, kv.Key))
                    {
                        hits[chestIndex] = itemName;
                    }
                }
            }

            // 2. 箱子自定义名命中
            Chest[] chests = Main.chest;
            if (chests != null)
            {
                for (int i = 0; i < chests.Length; i++)
                {
                    Chest c = chests[i];
                    if (c == null || string.IsNullOrEmpty(c.name)) continue;
                    if (hits.ContainsKey(i)) continue;
                    if (PinyinHelper.Matches(c.name, query))
                    {
                        hits[i] = $"箱名\"{c.name}\"";
                    }
                }
            }

            List<AtlasChestHit> result = new List<AtlasChestHit>(hits.Count);
            foreach (KeyValuePair<int, string> kv in hits)
            {
                Chest c = Main.chest[kv.Key];
                result.Add(new AtlasChestHit
                {
                    ChestIndex = kv.Key,
                    X = c?.x ?? 0,
                    Y = c?.y ?? 0,
                    MatchText = kv.Value,
                });
            }
            return result;
        }

        private static bool ChestContainsItemType(Chest c, int type)
        {
            for (int j = 0; j < c.item.Length; j++)
            {
                Item it = c.item[j];
                if (it != null && !it.IsAir && it.stack > 0 && it.type == type) return true;
            }
            return false;
        }
    }
}
