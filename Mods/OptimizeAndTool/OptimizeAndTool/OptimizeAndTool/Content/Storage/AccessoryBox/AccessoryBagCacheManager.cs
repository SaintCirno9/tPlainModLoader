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
        private static Dictionary<Guid, AccessoryBagItem> _bagCache = new Dictionary<Guid, AccessoryBagItem>();
        private static List<AccessoryBagItem> _playerBagsCache = new List<AccessoryBagItem>();

        public static void UpdateCache()
        {
            if (Main.netMode == 2 || Main.gameMenu) return;

            Player player = Main.LocalPlayer;
            if (player == null || !player.active) return;

            var newBagCache = new Dictionary<Guid, AccessoryBagItem>();
            var newPlayerBags = new List<AccessoryBagItem>(16);

            ScanArray(player.inventory, newBagCache, newPlayerBags);
            ScanArray(player.bank?.item, newBagCache, newPlayerBags);
            ScanArray(player.bank2?.item, newBagCache, newPlayerBags);
            ScanArray(player.bank3?.item, newBagCache, newPlayerBags);
            ScanArray(player.bank4?.item, newBagCache, newPlayerBags);
            ScanArray(BigBag.BigBag.Slots, newBagCache, newPlayerBags);

            _bagCache = newBagCache;
            _playerBagsCache = newPlayerBags;
        }

        private static void ScanArray(Item[] items, Dictionary<Guid, AccessoryBagItem> bagCache, List<AccessoryBagItem> playerBags)
        {
            if (items == null) return;
            for (int i = 0; i < items.Length; i++)
            {
                Item it = items[i];
                if (it != null && !it.IsAir)
                {
                    AccessoryBagItem bag = ItemLoader.GetModItem(it) as AccessoryBagItem;
                    if (bag != null && bag.BagID != Guid.Empty && !bagCache.ContainsKey(bag.BagID))
                    {
                        bagCache[bag.BagID] = bag;
                        playerBags.Add(bag);
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
            if (_playerBagsCache.Count == 0 || Main.GameUpdateCount % 10 == 0) UpdateCache();
            return _playerBagsCache;
        }

        public static AccessoryBagItem GetFirstCarriedBag()
        {
            UpdateCache();
            return _playerBagsCache.Count > 0 ? _playerBagsCache[0] : null;
        }
    }
}
