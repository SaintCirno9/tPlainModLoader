using System;
using System.Collections.Generic;
using Terraria;
using TPML.Content;

namespace OptimizeAndTool.Content.Storage.AccessoryBox
{
    /// <summary>
    /// 随身饰品袋全局检索与缓存中心
    /// 作者: SaintCirno9
    /// </summary>
    public static class AccessoryBagCacheManager
    {
        private static readonly Dictionary<Guid, AccessoryBagItem> _bagCache = new Dictionary<Guid, AccessoryBagItem>();
        private static readonly List<AccessoryBagItem> _playerBagsCache = new List<AccessoryBagItem>(16);

        public static void UpdateCache()
        {
            if (Main.netMode == 2 || Main.gameMenu) return;

            Player player = Main.LocalPlayer;
            if (player == null || !player.active) return;

            _bagCache.Clear();
            _playerBagsCache.Clear();

            ScanArray(player.inventory);
            ScanArray(player.bank?.item);
            ScanArray(player.bank2?.item);
            ScanArray(player.bank3?.item);
            ScanArray(player.bank4?.item);
        }

        private static void ScanArray(Item[] items)
        {
            if (items == null) return;
            for (int i = 0; i < items.Length; i++)
            {
                Item it = items[i];
                if (it != null && !it.IsAir)
                {
                    AccessoryBagItem bag = ItemLoader.GetModItem(it) as AccessoryBagItem;
                    if (bag != null && bag.BagID != Guid.Empty && !_bagCache.ContainsKey(bag.BagID))
                    {
                        _bagCache[bag.BagID] = bag;
                        _playerBagsCache.Add(bag);
                    }
                }
            }
        }

        public static AccessoryBagItem FindBagByID(Guid id)
        {
            if (id == Guid.Empty) return null;
            if (_bagCache.TryGetValue(id, out AccessoryBagItem bag)) return bag;

            UpdateCache();
            if (_bagCache.TryGetValue(id, out bag)) return bag;

            return null;
        }

        public static IReadOnlyList<AccessoryBagItem> GetAllBags()
        {
            if (Main.GameUpdateCount % 10 == 0) UpdateCache();
            return _playerBagsCache;
        }

        public static AccessoryBagItem GetFirstCarriedBag()
        {
            UpdateCache();
            return _playerBagsCache.Count > 0 ? _playerBagsCache[0] : null;
        }
    }
}
