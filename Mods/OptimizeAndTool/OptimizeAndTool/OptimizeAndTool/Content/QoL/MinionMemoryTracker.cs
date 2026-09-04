using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TPML.Content.IO;
using TPML.Core.Logging;

namespace OptimizeAndTool.Content.QoL
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
        public int OriginalDamage { get; set; }
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
        private static bool _needsResummon = false;

        private static bool _lookupInitialized = false;
        private static readonly Dictionary<int, int> _buffToItem = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> _projToItem = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> _itemToBuff = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> _itemToProj = new Dictionary<int, int>();
        private static readonly Dictionary<int, bool> _itemIsSentry = new Dictionary<int, bool>();

        static MinionMemoryTracker()
        {
            ModItemSidecarEngine.OnCollectPlayerSidecarData += (player, data) =>
            {
                if (player == null || data == null) return;
                lock (_lock)
                {
                    if (player.name == _currentLoadedPlayerName)
                    {
                        if (data.CustomProperties == null) data.CustomProperties = new Dictionary<string, string>();
                        if (_activeMemory != null && _activeMemory.Count > 0)
                        {
                            data.CustomProperties[CustomPropertyKey] = JsonConvert.SerializeObject(_activeMemory);
                        }
                        else
                        {
                            data.CustomProperties.Remove(CustomPropertyKey);
                        }
                        _isDirty = false;
                    }
                }
            };
        }

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
            _needsResummon = true;
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

            // 进世界或复活统一等待 60 帧（1秒），确保装备、随身饰品袋、药水与模组被动属性完全充分结算完毕
            if (_needsResummon && _aliveFrames >= 60)
            {
                _needsResummon = false;
                if (QoLValSet.autoResummonMinions.val && _activeMemory != null && _activeMemory.Count > 0)
                {
                    // 检查仆从槽位是否未满上限：若未达上限，先清理残余旧仆从再按记忆精准召满
                    float activeSlots = CountActiveMinionSlots(player);
                    int activeSentries = CountActiveSentries(player);
                    if (activeSlots < player.maxMinions || activeSentries < player.maxTurrets)
                    {
                        CleanActiveMinionsAndBuffs(player);
                        ExecuteResummon(player);
                    }
                }
            }

            // 存活超过 120 帧（2秒）后，且无待重召任务时，每 30 帧进行一次内存快照比对与同步（纯内存操作，零同步磁盘 I/O）
            if (_aliveFrames >= 120 && !_needsResummon && _aliveFrames % 30 == 0)
            {
                SyncActiveMinions(player);
            }
        }

        /// <summary>
        /// 角色复活瞬间标记待重新召唤（统一延迟 60 帧以待装备完全结算）
        /// </summary>
        public static void OnRespawn(Player player)
        {
            if (player == null || player != Main.LocalPlayer) return;

            _needsResummon = true;
            _aliveFrames = 0;
        }

        /// <summary>
        /// 统计玩家当前活跃仆从所实际占用的总栏位数
        /// </summary>
        public static float CountActiveMinionSlots(Player player)
        {
            if (player == null) return 0f;
            float totalSlots = 0f;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI && p.minion)
                {
                    bool isZeroSlotMinion = p.type == 625 || p.type == 628 ||
                                            p.type == ProjectileID.AbigailMinion ||
                                            p.type == ProjectileID.StormTigerTier1 ||
                                            p.type == ProjectileID.StormTigerTier2 ||
                                            p.type == ProjectileID.StormTigerTier3;

                    float slots = isZeroSlotMinion ? 0f : (p.minionSlots > 0 ? p.minionSlots : 1f);
                    totalSlots += slots;
                }
            }
            return totalSlots;
        }

        /// <summary>
        /// 统计玩家当前活跃哨兵数量
        /// </summary>
        public static int CountActiveSentries(Player player)
        {
            if (player == null) return 0;
            int count = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI && p.sentry)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 清理玩家场上已有的仆从与哨兵弹幕及其对应 Buff，确保干净重建
        /// </summary>
        public static void CleanActiveMinionsAndBuffs(Player player)
        {
            if (player == null) return;
            EnsureLookup();

            // 1. 清除仆从与哨兵弹幕
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p != null && p.active && p.owner == player.whoAmI && (p.minion || p.sentry))
                {
                    p.Kill();
                }
            }

            // 2. 清除仆从 Buff（避免残留旧 Buff 导致计数混乱）
            for (int b = 0; b < player.buffType.Length; b++)
            {
                int buff = player.buffType[b];
                if (buff <= 0) continue;
                if (Main.vanityPet[buff] || Main.lightPet[buff]) continue;
                if (FindItemIdForBuff(buff) > 0)
                {
                    player.DelBuff(b);
                    b--;
                }
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
            var originalDamageByItem = new Dictionary<int, int>();
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
                        // 原版特殊附属/实体弹幕不计入栏位数（由其计数弹幕提供栏位统计）：
                        // - 星尘之龙的头 (625) 与尾 (628) 占 0 栏位（身节 626/627 各占 0.5 栏位）
                        // - 阿比盖尔本体 (963) 占 0 栏位（由计数弹幕 970 各占 1 栏位）
                        // - 沙漠之虎本体 (833, 834, 835) 占 0 栏位（由计数弹幕 831 各占 1 栏位）
                        bool isZeroSlotMinion = p.type == 625 || p.type == 628 ||
                                                p.type == ProjectileID.AbigailMinion ||
                                                p.type == ProjectileID.StormTigerTier1 ||
                                                p.type == ProjectileID.StormTigerTier2 ||
                                                p.type == ProjectileID.StormTigerTier3;

                        float slots = isZeroSlotMinion ? 0f : (p.minionSlots > 0 ? p.minionSlots : 1f);
                        if (slots > 0f)
                        {
                            if (minionSlotsByItem.ContainsKey(itemId))
                                minionSlotsByItem[itemId] += slots;
                            else
                                minionSlotsByItem[itemId] = slots;
                        }

                        int pDmg = p.originalDamage > 0 ? p.originalDamage : p.damage;
                        if (pDmg > 0)
                        {
                            if (!originalDamageByItem.TryGetValue(itemId, out int oldDmg) || pDmg > oldDmg)
                                originalDamageByItem[itemId] = pDmg;
                        }
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

                        int pDmg = p.originalDamage > 0 ? p.originalDamage : p.damage;
                        if (pDmg > 0)
                        {
                            if (!originalDamageByItem.TryGetValue(itemId, out int oldDmg) || pDmg > oldDmg)
                                originalDamageByItem[itemId] = pDmg;
                        }
                    }
                }
            }

            // 3. 若场上完全无仆从弹幕且无仆从 Buff -> 玩家主动驱散，标记清空记忆
            // 保护机制：若处于待重召缓冲期或存活不足 120 帧，严禁误清空伴随记忆
            if (summonBuffCount == 0 && totalActiveProj == 0)
            {
                if (_needsResummon || _aliveFrames < 120) return;

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
                int origDmg = ResolveOriginalDamageForRecord(player, itemId, originalDamageByItem);

                newMemory.Add(new MinionEntry
                {
                    ItemId = itemId,
                    BuffType = buffType,
                    ProjType = projType,
                    Count = count,
                    IsSentry = false,
                    OriginalDamage = origDmg
                });
            }

            foreach (var kvp in sentrySlotsByItem)
            {
                int itemId = kvp.Key;
                int count = kvp.Value;
                int buffType = _itemToBuff.TryGetValue(itemId, out int b) ? b : 0;
                int projType = _itemToProj.TryGetValue(itemId, out int pr) ? pr : 0;
                int origDmg = ResolveOriginalDamageForRecord(player, itemId, originalDamageByItem);

                newMemory.Add(new MinionEntry
                {
                    ItemId = itemId,
                    BuffType = buffType,
                    ProjType = projType,
                    Count = count,
                    IsSentry = true,
                    OriginalDamage = origDmg
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

        private static int ResolveOriginalDamageForRecord(Player player, int itemId, Dictionary<int, int> originalDamageByItem)
        {
            Item invItem = FindPlayerItem(player, itemId);
            if (invItem != null && invItem.damage > 0)
            {
                return invItem.damage;
            }

            if (originalDamageByItem != null && originalDamageByItem.TryGetValue(itemId, out int dmg) && dmg > 0)
            {
                return dmg;
            }

            Item sample = GetItemSample(itemId);
            return sample != null && sample.damage > 0 ? sample.damage : 20;
        }

        public static Item FindPlayerItem(Player player, int itemId)
        {
            if (player == null || player.inventory == null || itemId <= 0) return null;
            for (int i = 0; i < player.inventory.Length; i++)
            {
                Item it = player.inventory[i];
                if (it != null && it.type == itemId)
                {
                    return it;
                }
            }
            return null;
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
                if (a.ItemId != b.ItemId || a.Count != b.Count || a.IsSentry != b.IsSentry || a.OriginalDamage != b.OriginalDamage)
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

        private static void ResolveWeaponDamageAndKnockback(Player player, Item sampleItem, MinionEntry entry, out int originalDamage, out float knockBack)
        {
            originalDamage = 20;
            knockBack = 1f;

            // 1. 优先从玩家背包查找携带的真实武器（继承无情/神话等前缀的基础伤害）
            Item invItem = FindPlayerItem(player, entry.ItemId);
            if (invItem != null && invItem.damage > 0)
            {
                originalDamage = invItem.damage;
                knockBack = player.GetWeaponKnockback(invItem, invItem.knockBack);
                return;
            }

            // 2. 其次使用伴随存档/内存记录的 OriginalDamage
            if (entry.OriginalDamage > 0)
            {
                originalDamage = entry.OriginalDamage;
                if (sampleItem != null)
                {
                    knockBack = player.GetWeaponKnockback(sampleItem, sampleItem.knockBack);
                }
                return;
            }

            // 3. 兜底使用图鉴白板样本
            if (sampleItem != null && sampleItem.damage > 0)
            {
                originalDamage = sampleItem.damage;
                knockBack = player.GetWeaponKnockback(sampleItem, sampleItem.knockBack);
            }
        }

        private static Vector2 FindSentryLandingSpot(Player player, float horizontalOffset)
        {
            float spawnX = player.Center.X + horizontalOffset;
            int tileX = (int)(spawnX / 16f);
            int playerBottomTileY = (int)(player.Bottom.Y / 16f);

            tileX = Math.Max(10, Math.Min(Main.maxTilesX - 10, tileX));
            int startY = Math.Max(10, Math.Min(Main.maxTilesY - 20, playerBottomTileY - 1));

            int targetY = -1;
            for (int y = startY; y < startY + 30 && y < Main.maxTilesY - 10; y++)
            {
                if (Main.tile[tileX, y] != null && WorldGen.SolidTile2(tileX, y))
                {
                    targetY = y;
                    break;
                }
            }

            if (targetY != -1)
            {
                return new Vector2(spawnX, targetY * 16f - 24f);
            }

            return new Vector2(spawnX, player.Bottom.Y - 16f);
        }

        private static void SpawnStardustDragon(Player player, Terraria.DataStructures.IEntitySource source, int originalDamage, float knockBack, int totalSlots)
        {
            Vector2 spawnPos = player.Center;
            int headIdx = Projectile.NewProjectile(source, spawnPos.X, spawnPos.Y, 0f, 0f, 625, originalDamage, knockBack, player.whoAmI);
            int body1Idx = Projectile.NewProjectile(source, spawnPos.X, spawnPos.Y, 0f, 0f, 626, originalDamage, knockBack, player.whoAmI, Main.projectile[headIdx].key);
            int body2Idx = Projectile.NewProjectile(source, spawnPos.X, spawnPos.Y, 0f, 0f, 627, originalDamage, knockBack, player.whoAmI, Main.projectile[body1Idx].key);
            int tailIdx = Projectile.NewProjectile(source, spawnPos.X, spawnPos.Y, 0f, 0f, 628, originalDamage, knockBack, player.whoAmI, Main.projectile[body2Idx].key);

            if (body1Idx >= 0 && body1Idx < Main.maxProjectiles) Main.projectile[body1Idx].localAI[1] = body2Idx;
            if (body2Idx >= 0 && body2Idx < Main.maxProjectiles) Main.projectile[body2Idx].localAI[1] = tailIdx;

            if (headIdx >= 0 && headIdx < Main.maxProjectiles) Main.projectile[headIdx].originalDamage = originalDamage;
            if (body1Idx >= 0 && body1Idx < Main.maxProjectiles) Main.projectile[body1Idx].originalDamage = originalDamage;
            if (body2Idx >= 0 && body2Idx < Main.maxProjectiles) Main.projectile[body2Idx].originalDamage = originalDamage;
            if (tailIdx >= 0 && tailIdx < Main.maxProjectiles) Main.projectile[tailIdx].originalDamage = originalDamage;

            int extraSlots = totalSlots - 1;
            for (int k = 0; k < extraSlots; k++)
            {
                if (tailIdx < 0 || tailIdx >= Main.maxProjectiles) break;
                float prevKey = Main.projectile[tailIdx].ai[0];
                int newBody1 = Projectile.NewProjectile(source, spawnPos.X, spawnPos.Y, 0f, 0f, 626, originalDamage, knockBack, player.whoAmI, prevKey);
                int newBody2 = Projectile.NewProjectile(source, spawnPos.X, spawnPos.Y, 0f, 0f, 627, originalDamage, knockBack, player.whoAmI, Main.projectile[newBody1].key);

                if (newBody1 >= 0 && newBody1 < Main.maxProjectiles && newBody2 >= 0 && newBody2 < Main.maxProjectiles)
                {
                    Main.projectile[newBody1].localAI[1] = newBody2;
                    Main.projectile[newBody1].netUpdate = true;
                    Main.projectile[newBody1].ai[1] = 1f;
                    Main.projectile[newBody1].originalDamage = originalDamage;

                    Main.projectile[newBody2].localAI[1] = tailIdx;
                    Main.projectile[newBody2].netUpdate = true;
                    Main.projectile[newBody2].ai[1] = 1f;
                    Main.projectile[newBody2].originalDamage = originalDamage;

                    Main.projectile[tailIdx].ai[0] = Main.projectile[newBody2].key;
                    Main.projectile[tailIdx].netUpdate = true;
                    Main.projectile[tailIdx].ai[1] = 1f;
                    Main.projectile[tailIdx].originalDamage = originalDamage;
                }
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

            ResolveWeaponDamageAndKnockback(player, sampleItem, entry, out int originalDamage, out float knockBack);
            var source = (sampleItem != null) ? player.GetProjectileSource_Item(sampleItem) : player.GetProjectileSource_Misc(0);

            // 1. 哨兵（Sentry）生成分支
            if (entry.IsSentry)
            {
                for (int k = 0; k < count; k++)
                {
                    float offset = (k - (count - 1) * 0.5f) * 40f;
                    Vector2 landingPos = FindSentryLandingSpot(player, offset);
                    int pIndex = Projectile.NewProjectile(source, landingPos.X, landingPos.Y, 0f, 0f, shootProj, originalDamage, knockBack, player.whoAmI);
                    if (pIndex >= 0 && pIndex < Main.maxProjectiles)
                    {
                        Main.projectile[pIndex].originalDamage = originalDamage;
                    }
                }
                player.UpdateMaxTurrets();
                return true;
            }

            // 2. 特殊复合仆从：星尘之龙法杖（Stardust Dragon Staff）
            if (entry.ItemId == ItemID.StardustDragonStaff)
            {
                SpawnStardustDragon(player, source, originalDamage, knockBack, count);
                return true;
            }

            // 3. 特殊复合仆从：魔眼法杖双生子（Optic Staff）
            if (entry.ItemId == ItemID.OpticStaff)
            {
                for (int k = 0; k < count; k++)
                {
                    float randX1 = (float)(Main.rand.NextDouble() * 40.0 - 20.0);
                    float randY1 = (float)(Main.rand.NextDouble() * 40.0 - 20.0);
                    Vector2 spawnPos1 = player.Center + new Vector2(randX1, randY1);
                    int p1 = Projectile.NewProjectile(source, spawnPos1.X, spawnPos1.Y, 0f, -2f, ProjectileID.Retanimini, originalDamage, knockBack, player.whoAmI);
                    if (p1 >= 0 && p1 < Main.maxProjectiles)
                    {
                        Main.projectile[p1].originalDamage = originalDamage;
                    }

                    float randX2 = (float)(Main.rand.NextDouble() * 40.0 - 20.0);
                    float randY2 = (float)(Main.rand.NextDouble() * 40.0 - 20.0);
                    Vector2 spawnPos2 = player.Center + new Vector2(randX2, randY2);
                    int p2 = Projectile.NewProjectile(source, spawnPos2.X, spawnPos2.Y, 0f, -2f, ProjectileID.Spazmamini, originalDamage, knockBack, player.whoAmI);
                    if (p2 >= 0 && p2 < Main.maxProjectiles)
                    {
                        Main.projectile[p2].originalDamage = originalDamage;
                    }
                }
                return true;
            }

            // 4. 特殊仆从：矮人法杖（Pygmy Staff）
            if (entry.ItemId == ItemID.PygmyStaff)
            {
                for (int k = 0; k < count; k++)
                {
                    int pygmyType = Main.rand.Next(191, 195);
                    float randX = (float)(Main.rand.NextDouble() * 40.0 - 20.0);
                    float randY = (float)(Main.rand.NextDouble() * 40.0 - 20.0);
                    Vector2 spawnPos = player.Center + new Vector2(randX, randY);
                    int p = Projectile.NewProjectile(source, spawnPos.X, spawnPos.Y, 0f, -2f, pygmyType, originalDamage, knockBack, player.whoAmI);
                    if (p >= 0 && p < Main.maxProjectiles)
                    {
                        Main.projectile[p].originalDamage = originalDamage;
                        Main.projectile[p].localAI[0] = 30f;
                    }
                }
                return true;
            }

            // 5. 单体成长型仆从：阿比盖尔的花（Abigail's Flower）与沙漠之虎（Desert Tiger / StormTigerStaff）
            if (entry.ItemId == ItemID.AbigailsFlower || entry.ItemId == ItemID.StormTigerStaff)
            {
                for (int k = 0; k < count; k++)
                {
                    float randX = (float)(Main.rand.NextDouble() * 20.0 - 10.0);
                    float randY = (float)(Main.rand.NextDouble() * 20.0 - 10.0);
                    Vector2 spawnPos = player.Center + new Vector2(randX, randY);
                    int p = Projectile.NewProjectile(source, spawnPos.X, spawnPos.Y, 0f, -2f, shootProj, originalDamage, knockBack, player.whoAmI);
                    if (p >= 0 && p < Main.maxProjectiles)
                    {
                        Main.projectile[p].originalDamage = originalDamage;
                        // 标记 localAI[0] = 1f 抑制单帧内多个计数弹幕连续触发原版升级音效 (SoundID.AbigailUpgrade) 导致的爆音
                        Main.projectile[p].localAI[0] = 1f;
                    }
                }
                return true;
            }

            // 6. 通用常规仆从生成循环
            for (int k = 0; k < count; k++)
            {
                float randX = (float)(Main.rand.NextDouble() * 40.0 - 20.0);
                float randY = (float)(Main.rand.NextDouble() * 40.0 - 20.0);
                Vector2 spawnPos = player.Center + new Vector2(randX, randY);
                int pIndex = Projectile.NewProjectile(source, spawnPos.X, spawnPos.Y, 0f, -2f, shootProj, originalDamage, knockBack, player.whoAmI);
                if (pIndex >= 0 && pIndex < Main.maxProjectiles)
                {
                    Main.projectile[pIndex].originalDamage = originalDamage;
                }
            }

            return true;
        }
    }
}
