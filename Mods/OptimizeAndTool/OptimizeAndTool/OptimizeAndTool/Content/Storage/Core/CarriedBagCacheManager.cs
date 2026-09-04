using System;
using System.Collections.Generic;
using OptimizeAndTool.Content.Storage.AccessoryBox;
using OptimizeAndTool.Content.Storage.ItemContainer;
using Terraria;
using TPML.Content;

namespace OptimizeAndTool.Content.Storage.Core
{
    /// <summary>
    /// 随身容器全局检索与权威缓存中心：<br/>
    /// 统一覆盖本地玩家主背包、随身银行（猪猪存钱罐/保险箱/护卫熔炉/虚空袋）以及大背包 (<see cref="BigBag.BigBag"/>)；<br/>
    /// 具备单帧更新保护，杜绝重复遍历与 GC 开销，为属性计算、制作融合与窗口管理提供唯一的权威数据源。
    /// 作者: SaintCirno9
    /// </summary>
    public static class CarriedBagCacheManager
    {
        private static readonly List<IBagInventory> _allBags = new List<IBagInventory>(16);
        private static readonly List<AccessoryBagItem> _accessoryBags = new List<AccessoryBagItem>(8);
        private static readonly List<ItemContainerItem> _itemContainers = new List<ItemContainerItem>(16);
        private static readonly Dictionary<Guid, AccessoryBagItem> _accessoryBagById = new Dictionary<Guid, AccessoryBagItem>();
        private static uint _lastScanFrame = uint.MaxValue;

        /// <summary>
        /// 确保当前帧缓存已与玩家随身物品同步
        /// </summary>
        public static void UpdateCache(Player player = null)
        {
            if (Main.netMode == 2 || Main.gameMenu) return;

            player = player ?? Main.LocalPlayer;
            if (player == null || !player.active) return;

            if (_lastScanFrame == Main.GameUpdateCount) return;
            _lastScanFrame = Main.GameUpdateCount;

            _allBags.Clear();
            _accessoryBags.Clear();
            _itemContainers.Clear();
            _accessoryBagById.Clear();

            ScanArray(player.inventory);
            ScanArray(player.bank?.item);
            ScanArray(player.bank2?.item);
            ScanArray(player.bank3?.item);
            ScanArray(player.bank4?.item);
            ScanArray(BigBag.BigBag.Slots);
        }

        private static void ScanArray(Item[] items)
        {
            if (items == null) return;

            for (int i = 0; i < items.Length; i++)
            {
                Item it = items[i];
                if (it != null && !it.IsAir)
                {
                    ModItem modItem = ItemLoader.GetModItem(it);
                    if (modItem is AccessoryBagItem accBag)
                    {
                        if (!_accessoryBags.Contains(accBag))
                        {
                            _accessoryBags.Add(accBag);
                            _allBags.Add(accBag);
                            if (accBag.BagID != Guid.Empty && !_accessoryBagById.ContainsKey(accBag.BagID))
                            {
                                _accessoryBagById[accBag.BagID] = accBag;
                            }
                        }
                    }
                    else if (modItem is ItemContainerItem container)
                    {
                        if (!_itemContainers.Contains(container))
                        {
                            _itemContainers.Add(container);
                            _allBags.Add(container);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取所有随身携带的指定类型容器列表
        /// </summary>
        public static List<T> GetCarriedBags<T>(Player player = null) where T : class, IBagInventory
        {
            UpdateCache(player);

            if (typeof(T) == typeof(AccessoryBagItem))
            {
                return _accessoryBags as List<T>;
            }

            if (typeof(T) == typeof(ItemContainerItem))
            {
                return _itemContainers as List<T>;
            }

            var result = new List<T>(_allBags.Count);
            for (int i = 0; i < _allBags.Count; i++)
            {
                if (_allBags[i] is T match)
                {
                    result.Add(match);
                }
            }
            return result;
        }

        /// <summary>
        /// 获取随身携带的第一个指定类型容器实体
        /// </summary>
        public static T GetFirstCarriedBag<T>(Player player = null) where T : class, IBagInventory
        {
            UpdateCache(player);

            if (typeof(T) == typeof(AccessoryBagItem))
            {
                return (_accessoryBags.Count > 0 ? _accessoryBags[0] : null) as T;
            }

            for (int i = 0; i < _allBags.Count; i++)
            {
                if (_allBags[i] is T match) return match;
            }

            return null;
        }

        /// <summary>
        /// 获取所有随身携带的容器实体列表（包含饰品袋与收纳袋）
        /// </summary>
        public static IReadOnlyList<IBagInventory> GetAllCarriedBags(Player player = null)
        {
            UpdateCache(player);
            return _allBags;
        }

        /// <summary>
        /// 获取所有随身携带的饰品袋列表
        /// </summary>
        public static IReadOnlyList<AccessoryBagItem> GetAllAccessoryBags(Player player = null)
        {
            UpdateCache(player);
            return _accessoryBags;
        }

        /// <summary>
        /// 获取所有随身携带的通用大型收纳容器（垃圾桶、药水袋、旗帜盒）列表
        /// </summary>
        public static IReadOnlyList<ItemContainerItem> GetAllItemContainers(Player player = null)
        {
            UpdateCache(player);
            return _itemContainers;
        }

        /// <summary>
        /// 根据唯一标识符检索饰品袋
        /// </summary>
        public static AccessoryBagItem FindAccessoryBagByID(Guid id)
        {
            if (id == Guid.Empty) return null;

            UpdateCache();
            if (_accessoryBagById.TryGetValue(id, out AccessoryBagItem bag)) return bag;

            return null;
        }
    }
}
