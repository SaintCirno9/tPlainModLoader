using Newtonsoft.Json;
using OptimizeAndTool.Content.BigBag;
using OptimizeAndTool.Content.Storage.AccessoryBox;
using System;
using System.Collections.Generic;
using System.IO;
using tContentPatch;
using Terraria;
using Terraria.IO;
using TPML.Content.IO;

namespace OptimizeAndTool.Content.QoL.GuaranteedDrop
{
    /// <summary>
    /// 角色历史已获取/已拾取物品记忆追踪器：
    /// 1. 严格按角色隔离已发现的物品 ID（HashSet<int>）；
    /// 2. 角色生命周期（加载、保存、切换）自动伴随存档持久化；
    /// 3. 老角色或新进入游戏时自动深度扫描背包、装备、四种便携银行、大背包及饰品箱建档；
    /// 4. 实时拾取与开箱入库，毫秒级线程安全查重。
    /// 作者: SaintCirno9
    /// </summary>
    public static class DiscoveredItemTracker
    {
        private static readonly object _lock = new object();
        private static readonly HashSet<int> _discoveredItems = new HashSet<int>();
        private static bool _isDirty = false;

        /// <summary>当前在内存中持有已发现物品记录的角色名称</summary>
        public static string ActivePlayerName { get; private set; }

        /// <summary>获取当前已发现的物品种类总数</summary>
        public static int DiscoveredCount
        {
            get
            {
                lock (_lock)
                {
                    return _discoveredItems.Count;
                }
            }
        }

        /// <summary>
        /// 获取指定角色的已发现物品伴随存档路径
        /// </summary>
        public static string GetSavePath(string playerName)
        {
            string cleanName = SidecarSaveManager.CleanFileName(playerName ?? "unknown");
            return Path.Combine(SidecarSaveManager.SaveDirectory, $"Player_{cleanName}_discovered.json");
        }

        /// <summary>
        /// 检查当前角色历史上是否曾经获取/拾取过该物品
        /// </summary>
        /// <param name="player">目标玩家（若为 null 则默认判定本地玩家）</param>
        /// <param name="itemId">物品 ID</param>
        /// <returns>若已获取过返回 true；未曾获取过返回 false</returns>
        public static bool HasDiscovered(Player player, int itemId)
        {
            if (itemId <= 0) return true;

            lock (_lock)
            {
                return _discoveredItems.Contains(itemId);
            }
        }

        /// <summary>
        /// 记录一件新发现/新拾取的物品 ID
        /// </summary>
        /// <param name="itemId">物品 ID</param>
        /// <returns>若为首次收录返回 true；若此前已收录返回 false</returns>
        public static bool RecordDiscovered(int itemId)
        {
            if (itemId <= 0) return false;

            lock (_lock)
            {
                if (_discoveredItems.Add(itemId))
                {
                    _isDirty = true;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 批量记录一组新物品 ID
        /// </summary>
        public static void RecordDiscoveredRange(IEnumerable<int> itemIds)
        {
            if (itemIds == null) return;

            lock (_lock)
            {
                foreach (int id in itemIds)
                {
                    if (id > 0 && _discoveredItems.Add(id))
                    {
                        _isDirty = true;
                    }
                }
            }
        }

        /// <summary>
        /// 深度扫描玩家随身的所有物品（背包、装备、染料、四大便携存钱罐、大背包、饰品箱等）并初始化录入
        /// </summary>
        public static void ScanPlayerInventory(Player player)
        {
            if (player == null) return;

            lock (_lock)
            {
                void ScanArray(Item[] arr)
                {
                    if (arr == null) return;
                    for (int i = 0; i < arr.Length; i++)
                    {
                        Item it = arr[i];
                        if (it != null && !it.IsAir && it.type > 0)
                        {
                            if (_discoveredItems.Add(it.type))
                            {
                                _isDirty = true;
                            }
                        }
                    }
                }

                ScanArray(player.inventory);
                ScanArray(player.armor);
                ScanArray(player.dye);
                ScanArray(player.miscEquips);
                ScanArray(player.miscDyes);
                ScanArray(player.bank?.item);
                ScanArray(player.bank2?.item);
                ScanArray(player.bank3?.item);
                ScanArray(player.bank4?.item);

                if (BigBag.BigBag.Slots != null)
                {
                    ScanArray(BigBag.BigBag.Slots);
                }

                try
                {
                    var bags = AccessoryBagCacheManager.GetAllBags();
                    if (bags != null)
                    {
                        for (int b = 0; b < bags.Count; b++)
                        {
                            if (bags[b]?.personalInventory != null)
                            {
                                ScanArray(bags[b].personalInventory);
                            }
                        }
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// 加载指定角色的历史发现记录
        /// </summary>
        public static void LoadForPlayer(Player player)
        {
            if (player == null)
            {
                ResetMemory();
                return;
            }

            lock (_lock)
            {
                ActivePlayerName = player.name;
                _discoveredItems.Clear();
                _isDirty = false;

                string path = GetSavePath(player.name);
                if (File.Exists(path))
                {
                    try
                    {
                        string json = File.ReadAllText(path);
                        List<int> loadedList = JsonConvert.DeserializeObject<List<int>>(json);
                        if (loadedList != null)
                        {
                            for (int i = 0; i < loadedList.Count; i++)
                            {
                                if (loadedList[i] > 0) _discoveredItems.Add(loadedList[i]);
                            }
                        }
                    }
                    catch { }
                }

                // 首次建档或老角色加载：自动补齐扫描当前身上携带的所有物品并强制即时落盘
                ScanPlayerInventory(player);
                SaveNow(player, force: true);
            }
        }

        /// <summary>
        /// 立即将当前角色的已发现记录落盘保存
        /// </summary>
        public static void SaveNow(Player player = null, bool force = false)
        {
            Player p = player ?? Main.LocalPlayer;
            if (p == null || string.IsNullOrEmpty(ActivePlayerName) || p.name != ActivePlayerName) return;

            lock (_lock)
            {
                if (!_isDirty && !force) return;

                try
                {
                    string path = GetSavePath(p.name);
                    string dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    List<int> list = new List<int>(_discoveredItems);
                    string json = JsonConvert.SerializeObject(list);
                    File.WriteAllText(path, json);
                    _isDirty = false;
                }
                catch { }
            }
        }

        /// <summary>
        /// 一键重置当前角色的历史发现记录，并重新仅扫描当前身上携带的物品
        /// </summary>
        public static void ResetDiscovered(Player player)
        {
            if (player == null) return;

            lock (_lock)
            {
                _discoveredItems.Clear();
                ScanPlayerInventory(player);
                SaveNow(player, force: true);
            }
        }

        /// <summary>
        /// 重置内存状态
        /// </summary>
        public static void ResetMemory()
        {
            lock (_lock)
            {
                ActivePlayerName = null;
                _discoveredItems.Clear();
                _isDirty = false;
            }
        }
    }

    /// <summary>
    /// 角色生命周期监听器：角色加载、保存、切换时自动存取历史发现记忆
    /// </summary>
    public class GuaranteedDropPlayer : PatchPlayer
    {
        public override void SavePlayerPrefix(PlayerFileData playerFile, bool skipMapSave)
        {
            if (playerFile?.Player != null)
            {
                if (!string.IsNullOrEmpty(DiscoveredItemTracker.ActivePlayerName) && playerFile.Player.name == DiscoveredItemTracker.ActivePlayerName)
                {
                    DiscoveredItemTracker.SaveNow(playerFile.Player);
                }
            }
        }

        public override void LoadPlayerPostfix(PlayerFileData playerFile)
        {
            if (playerFile?.Player != null)
            {
                DiscoveredItemTracker.LoadForPlayer(playerFile.Player);
            }
        }

        public override void SetAsActivePostfix(PlayerFileData playerFile)
        {
            if (playerFile?.Player != null)
            {
                DiscoveredItemTracker.LoadForPlayer(playerFile.Player);
            }
        }
    }
}
