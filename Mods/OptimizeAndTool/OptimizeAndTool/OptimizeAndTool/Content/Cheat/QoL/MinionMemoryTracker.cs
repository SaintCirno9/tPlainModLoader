using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TPML.Content.IO;
using TPML.Core.Logging;

namespace OptimizeAndTool.Content.Cheat.QoL
{
    /// <summary>
    /// 单个仆从/哨兵记忆条目
    /// </summary>
    public class MinionEntry
    {
        public int ItemId { get; set; }
        public int BuffType { get; set; }
        public int ProjType { get; set; }
        public int Count { get; set; }
        public bool IsSentry { get; set; }
    }

    /// <summary>
    /// 仆从与哨兵组合记忆追踪器（Sidecar 伴随持久化 & 重生/进世界自适应自动重新召唤）
    /// 作者: SaintCirno9
    /// </summary>
    public static class MinionMemoryTracker
    {
        public const string CustomPropertyKey = "OptimizeAndTool.MinionMemory";

        private static readonly object _lock = new object();
        private static List<MinionEntry> _activeMemory = new List<MinionEntry>();
        private static string _currentLoadedPlayerName = null;
        private static bool _isDirty = false;
        private static int _aliveFrames = 0;
        private static bool _needsWorldJoinResummon = false;

        private static bool _lookupInitialized = false;
        private static readonly Dictionary<int, int> _buffToItem = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> _projToItem = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> _itemToBuff = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> _itemToProj = new Dictionary<int, int>();
        private static readonly Dictionary<int, bool> _itemIsSentry = new Dictionary<int, bool>();

        /// <summary>
        /// 初始化反查映射表（从 ContentSamples 自动构建）
        /// </summary>
        public static void EnsureLookup()
        {
            if (_lookupInitialized && _buffToItem.Count > 10) return;
            if (ContentSamples.ItemsByType == null || ContentSamples.ItemsByType.Count == 0) return;

            lock (_lock)
            {
                if (_lookupInitialized && _buffToItem.Count > 10) return;

                foreach (var kvp in ContentSamples.ItemsByType)
                {
                    Item it = kvp.Value;
                    if (it == null || it.type <= ItemID.None) continue;

                    if (IsMinionSummonItem(it))
                    {
                        if (it.buffType > 0 && !_buffToItem.ContainsKey(it.buffType))
                        {
                            _buffToItem[it.buffType] = it.type;
                        }
                        if (it.shoot > 0 && !_projToItem.ContainsKey(it.shoot))
                        {
                            _projToItem[it.shoot] = it.type;
                        }

                        _itemToBuff[it.type] = it.buffType;
                        _itemToProj[it.type] = it.shoot;
                        _itemIsSentry[it.type] = it.sentry;
                    }
                }

                // 补充多弹幕仆从的配对/次级弹幕反查映射
                _projToItem[ProjectileID.Spazmamini] = ItemID.OpticStaff;
                _projToItem[ProjectileID.Retanimini] = ItemID.OpticStaff;

                _lookupInitialized = true;
            }
        }

        public static bool IsMinionSummonItem(Item item)
        {
            if (item == null || item.type <= ItemID.None) return false;

            if (item.sentry) return true;

            if (item.summon)
            {
                if (item.buffType > 0 && !Main.vanityPet[item.buffType] && !Main.lightPet[item.buffType])
                {
                    return true;
                }
            }

            return false;
        }

        public static int FindItemIdForProjectile(Projectile p, Player player)
        {
            if (p == null) return 0;
            EnsureLookup();

            // 1. 快速字典反查
            if (_projToItem.TryGetValue(p.type, out int mappedItem) && mappedItem > 0)
            {
                return mappedItem;
            }

            // 2. 根据玩家身上的召唤 Buff 反查
            if (player != null)
            {
                for (int b = 0; b < player.buffType.Length; b++)
                {
                    int buff = player.buffType[b];
                    if (buff <= 0) continue;
                    if (Main.vanityPet[buff] || Main.lightPet[buff]) continue;
                    if (_buffToItem.TryGetValue(buff, out int itemFromBuff) && itemFromBuff > 0)
                    {
                        Item sample = GetItemSample(itemFromBuff);
                        if (sample != null && (sample.shoot == p.type || p.minion || p.sentry))
                        {
                            _projToItem[p.type] = itemFromBuff;
                            return itemFromBuff;
                        }
                    }
                }
            }

            // 3. 全量样本遍历兜底
            if (ContentSamples.ItemsByType != null)
            {
                foreach (var kvp in ContentSamples.ItemsByType)
                {
                    Item it = kvp.Value;
                    if (it != null && it.type > 0 && it.shoot == p.type)
                    {
                        _projToItem[p.type] = it.type;
                        return it.type;
                    }
                }
            }

            return 0;
        }

        public static int FindItemIdForBuff(int buff)
        {
            if (buff <= 0) return 0;
            EnsureLookup();

            if (_buffToItem.TryGetValue(buff, out int mappedItem) && mappedItem > 0)
            {
                return mappedItem;
            }

            if (ContentSamples.ItemsByType != null)
            {
                foreach (var kvp in ContentSamples.ItemsByType)
                {
                    Item it = kvp.Value;
                    if (it != null && it.type > 0 && it.buffType == buff)
                    {
                        _buffToItem[buff] = it.type;
                        return it.type;
                    }
                }
            }

            return 0;
        }

        public static Item GetItemSample(int itemId)
        {
            if (itemId <= 0) return null;
            if (ContentSamples.ItemsByType != null && ContentSamples.ItemsByType.TryGetValue(itemId, out Item sample) && sample != null)
            {
                return sample;
            }
            Item item = new Item();
            item.SetDefaults(itemId);
            return item;
        }

        /// <summary>
        /// 当玩家进入世界时标记等待初始恢复
        /// </summary>
        public static void OnEnterWorld()
        {
            _needsWorldJoinResummon = true;
            _aliveFrames = 0;
            if (Main.LocalPlayer != null)
            {
                LoadForPlayer(Main.LocalPlayer);
            }
        }

        /// <summary>
        /// 从 Sidecar 伴随存档载入玩家专属的仆从记忆
        /// </summary>
        public static void LoadForPlayer(Player player)
        {
            if (player == null || string.IsNullOrEmpty(player.name)) return;

            lock (_lock)
            {
                try
                {
                    string json = ModItemSidecarEngine.LoadPlayerCustomProperty(player, CustomPropertyKey);
                    if (!string.IsNullOrEmpty(json))
                    {
                        var list = JsonConvert.DeserializeObject<List<MinionEntry>>(json);
                        _activeMemory = list ?? new List<MinionEntry>();
                    }
                    else
                    {
                        _activeMemory = new List<MinionEntry>();
                    }
                    _currentLoadedPlayerName = player.name;
                    _isDirty = false;
                }
                catch (Exception ex)
                {
                    LogManager.GetLogger("OptimizeAndTool").Warn($"加载玩家 [{player.name}] 仆从记忆异常", ex);
                    _activeMemory = new List<MinionEntry>();
                    _currentLoadedPlayerName = player.name;
                    _isDirty = false;
                }
            }
        }

        /// <summary>
        /// 将当前仆从记忆保存至 Sidecar 伴随存档（仅在数据脏或强制保存时写盘）
        /// </summary>
        public static void SaveForPlayer(Player player, bool force = false)
        {
            if (player == null || string.IsNullOrEmpty(player.name)) return;

            lock (_lock)
            {
                // 校验角色归属：如果保存的角色名称与当前加载的角色不匹配，直接跳过保存
                // 防止新建角色 (CreateAndSave) 或重命名角色 (Rename) 时用当前静态记忆覆盖/污染新角色数据
                if (player.name != _currentLoadedPlayerName) return;

                if (!_isDirty && !force) return;

                try
                {
                    if (_activeMemory != null && _activeMemory.Count > 0)
                    {
                        string json = JsonConvert.SerializeObject(_activeMemory);
                        ModItemSidecarEngine.SavePlayerCustomProperty(player, CustomPropertyKey, json);
                    }
                    else
                    {
                        ModItemSidecarEngine.SavePlayerCustomProperty(player, CustomPropertyKey, null);
                    }
                    _isDirty = false;
                }
                catch (Exception ex)
                {
                    LogManager.GetLogger("OptimizeAndTool").Warn($"保存玩家 [{player.name}] 仆从记忆异常", ex);
                }
            }
        }

        /// <summary>
        /// 存活帧更新与状态同步（在 UpdatePostfix 中调用，装备与属性已完全结算）
        /// </summary>
        public static void Update(Player player)
        {
            if (player == null || player != Main.LocalPlayer) return;

            if (player.dead)
            {
                _aliveFrames = 0;
                return;
            }

            _aliveFrames++;

            // 进世界等待 5 帧，确保装备、饰品、模组属性完全结算就绪后再执行恢复召唤
            if (_needsWorldJoinResummon && _aliveFrames >= 5)
            {
                _needsWorldJoinResummon = false;
                if (QoLValSet.autoResummonMinions.val && _activeMemory != null && _activeMemory.Count > 0)
                {
                    if (CountActiveMinions(player) == 0)
                    {
                        ExecuteResummon(player);
                    }
                }
            }

            // 存活超过 60 帧后，每 30 帧进行一次内存快照比对与同步（纯内存操作，零同步磁盘 I/O）
            if (_aliveFrames > 60 && _aliveFrames % 30 == 0)
            {
                SyncActiveMinions(player);
            }
        }

        /// <summary>
        /// 角色复活瞬间触发重新召唤
        /// </summary>
        public static void OnRespawn(Player player)
        {
            if (player == null || player != Main.LocalPlayer) return;

            _aliveFrames = 1;

            if (QoLValSet.autoResummonMinions.val && _activeMemory != null && _activeMemory.Count > 0)
            {
                ExecuteResummon(player);
            }
        }

        public static int CountActiveMinions(Player player)
        {
            int count = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI && (p.minion || p.sentry))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 同步当前存活仆从快照至内存记忆；若主动驱散全部 Buff 则置空记忆
        /// </summary>
        public static void SyncActiveMinions(Player player)
        {
            if (player == null || player.dead) return;
            EnsureLookup();

            // 1. 统计当前生效的召唤 Buff
            int summonBuffCount = 0;
            for (int b = 0; b < player.buffType.Length; b++)
            {
                int buff = player.buffType[b];
                if (buff <= 0) continue;
                if (Main.vanityPet[buff] || Main.lightPet[buff]) continue;
                if (FindItemIdForBuff(buff) > 0)
                {
                    summonBuffCount++;
                }
            }

            // 2. 统计当前活跃的弹幕
            var minionSlotsByItem = new Dictionary<int, float>();
            var sentrySlotsByItem = new Dictionary<int, int>();
            int totalActiveProj = 0;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p == null || !p.active || p.owner != player.whoAmI) continue;

                int itemId = FindItemIdForProjectile(p, player);

                if (p.minion)
                {
                    totalActiveProj++;
                    if (itemId > 0)
                    {
                        float slots = p.minionSlots > 0 ? p.minionSlots : 1f;
                        if (minionSlotsByItem.ContainsKey(itemId))
                            minionSlotsByItem[itemId] += slots;
                        else
                            minionSlotsByItem[itemId] = slots;
                    }
                }
                else if (p.sentry)
                {
                    totalActiveProj++;
                    if (itemId > 0)
                    {
                        if (sentrySlotsByItem.ContainsKey(itemId))
                            sentrySlotsByItem[itemId] += 1;
                        else
                            sentrySlotsByItem[itemId] = 1;
                    }
                }
            }

            // 3. 若场上完全无仆从弹幕且无仆从 Buff -> 玩家主动驱散，标记清空记忆
            if (summonBuffCount == 0 && totalActiveProj == 0)
            {
                if (_activeMemory != null && _activeMemory.Count > 0)
                {
                    lock (_lock)
                    {
                        _activeMemory.Clear();
                        _isDirty = true;
                    }
                }
                return;
            }

            // 4. 根据当前弹幕构建最新记忆列表
            var newMemory = new List<MinionEntry>();

            foreach (var kvp in minionSlotsByItem)
            {
                int itemId = kvp.Key;
                int count = Math.Max(1, (int)Math.Round(kvp.Value));
                int buffType = _itemToBuff.TryGetValue(itemId, out int b) ? b : 0;
                int projType = _itemToProj.TryGetValue(itemId, out int pr) ? pr : 0;

                newMemory.Add(new MinionEntry
                {
                    ItemId = itemId,
                    BuffType = buffType,
                    ProjType = projType,
                    Count = count,
                    IsSentry = false
                });
            }

            foreach (var kvp in sentrySlotsByItem)
            {
                int itemId = kvp.Key;
                int count = kvp.Value;
                int buffType = _itemToBuff.TryGetValue(itemId, out int b) ? b : 0;
                int projType = _itemToProj.TryGetValue(itemId, out int pr) ? pr : 0;

                newMemory.Add(new MinionEntry
                {
                    ItemId = itemId,
                    BuffType = buffType,
                    ProjType = projType,
                    Count = count,
                    IsSentry = true
                });
            }

            // 5. 对比是否有变更，有变更则更新内存并置脏标记（待存档时安全写盘）
            if (newMemory.Count > 0 && !IsMemoryEqual(_activeMemory, newMemory))
            {
                lock (_lock)
                {
                    _activeMemory = newMemory;
                    _isDirty = true;
                }
            }
        }

        private static bool IsMemoryEqual(List<MinionEntry> listA, List<MinionEntry> listB)
        {
            if (listA == null && listB == null) return true;
            if (listA == null || listB == null) return false;
            if (listA.Count != listB.Count) return false;

            for (int i = 0; i < listA.Count; i++)
            {
                var a = listA[i];
                var b = listB[i];
                if (a.ItemId != b.ItemId || a.Count != b.Count || a.IsSentry != b.IsSentry)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 执行批量重新召唤（纯记忆 + 优先级截断 + 主仆从补齐上限 + 哨兵上限约束）
        /// </summary>
        public static void ExecuteResummon(Player player)
        {
            if (player == null) return;

            List<MinionEntry> snapshot;
            lock (_lock)
            {
                if (_activeMemory == null || _activeMemory.Count == 0) return;
                snapshot = new List<MinionEntry>(_activeMemory);
            }

            EnsureLookup();

            var minionEntries = new List<MinionEntry>();
            var sentryEntries = new List<MinionEntry>();

            foreach (var entry in snapshot)
            {
                if (entry == null || entry.Count <= 0) continue;
                if (entry.IsSentry)
                {
                    sentryEntries.Add(entry);
                }
                else
                {
                    minionEntries.Add(entry);
                }
            }

            bool spawnedAny = false;

            // 1. 仆从恢复与自适应补齐
            if (minionEntries.Count > 0 && player.maxMinions > 0)
            {
                int availableSlots = player.maxMinions;
                int totalRecorded = 0;
                foreach (var entry in minionEntries) totalRecorded += entry.Count;

                if (totalRecorded <= availableSlots)
                {
                    for (int i = 0; i < minionEntries.Count; i++)
                    {
                        var entry = minionEntries[i];
                        int toSpawn = entry.Count;
                        if (i == minionEntries.Count - 1)
                        {
                            // 上限高于记录总数：使用最后一种仆从补满多余栏位
                            toSpawn += (availableSlots - totalRecorded);
                        }
                        if (SpawnMinionEntry(player, entry, toSpawn))
                        {
                            spawnedAny = true;
                        }
                    }
                }
                else
                {
                    // 上限低于记录总数：优先级截断
                    int remaining = availableSlots;
                    foreach (var entry in minionEntries)
                    {
                        if (remaining <= 0) break;
                        int toSpawn = Math.Min(entry.Count, remaining);
                        if (SpawnMinionEntry(player, entry, toSpawn))
                        {
                            spawnedAny = true;
                        }
                        remaining -= toSpawn;
                    }
                }
            }

            // 2. 哨兵恢复
            if (sentryEntries.Count > 0 && player.maxTurrets > 0)
            {
                int remainingTurrets = player.maxTurrets;
                foreach (var entry in sentryEntries)
                {
                    if (remainingTurrets <= 0) break;
                    int toSpawn = Math.Min(entry.Count, remainingTurrets);
                    if (SpawnMinionEntry(player, entry, toSpawn))
                    {
                        spawnedAny = true;
                    }
                    remainingTurrets -= toSpawn;
                }
            }

            if (spawnedAny)
            {
                SoundEngine.PlaySound(SoundID.Item44, player.Center);
            }
        }

        private static bool SpawnMinionEntry(Player player, MinionEntry entry, int count)
        {
            if (player == null || entry == null || count <= 0) return false;

            Item sampleItem = GetItemSample(entry.ItemId);
            int buffType = entry.BuffType > 0 ? entry.BuffType : (sampleItem?.buffType ?? 0);
            int shootProj = entry.ProjType > 0 ? entry.ProjType : (sampleItem?.shoot ?? 0);

            if (shootProj <= 0) return false;

            // 赋予仆从/哨兵 Buff
            if (buffType > 0)
            {
                player.AddBuff(buffType, 36000);
            }

            int damage = sampleItem != null ? player.GetWeaponDamage(sampleItem) : 20;
            float knockBack = sampleItem != null ? player.GetWeaponKnockback(sampleItem, sampleItem.knockBack) : 1f;

            var source = (sampleItem != null) ? player.GetProjectileSource_Item(sampleItem) : player.GetProjectileSource_Misc(0);

            // 魔眼法杖双生子特殊生成：每组同时生成红眼 Retanimini 与绿眼 Spazmamini
            if (entry.ItemId == ItemID.OpticStaff)
            {
                for (int k = 0; k < count; k++)
                {
                    float randX1 = (float)(Main.rand.NextDouble() * 40.0 - 20.0);
                    float randY1 = (float)(Main.rand.NextDouble() * 40.0 - 20.0);
                    Vector2 spawnPos1 = player.Center + new Vector2(randX1, randY1);
                    Projectile.NewProjectile(source, spawnPos1.X, spawnPos1.Y, 0f, -2f, ProjectileID.Retanimini, damage, knockBack, player.whoAmI);

                    float randX2 = (float)(Main.rand.NextDouble() * 40.0 - 20.0);
                    float randY2 = (float)(Main.rand.NextDouble() * 40.0 - 20.0);
                    Vector2 spawnPos2 = player.Center + new Vector2(randX2, randY2);
                    Projectile.NewProjectile(source, spawnPos2.X, spawnPos2.Y, 0f, -2f, ProjectileID.Spazmamini, damage, knockBack, player.whoAmI);
                }
                return true;
            }

            for (int k = 0; k < count; k++)
            {
                float randX = (float)(Main.rand.NextDouble() * 40.0 - 20.0);
                float randY = (float)(Main.rand.NextDouble() * 40.0 - 20.0);
                Vector2 spawnPos = player.Center + new Vector2(randX, randY);
                Projectile.NewProjectile(source, spawnPos.X, spawnPos.Y, 0f, -2f, shootProj, damage, knockBack, player.whoAmI);
            }

            return true;
        }
    }
}
