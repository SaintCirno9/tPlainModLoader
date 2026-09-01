using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Tile_Entities;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader.IO;
using TPML.Core.IO;

namespace TPML.Content.IO
{
    /// <summary>
    /// 单个模组物品伴随持久化条目
    /// </summary>
    public class ModItemSaveEntry
    {
        public string Location { get; set; }
        public string ModName { get; set; }
        public string ItemName { get; set; }
        public int Stack { get; set; }
        public int Prefix { get; set; }
        public bool Favorited { get; set; }
        public string CustomData { get; set; }
    }

    /// <summary>
    /// 单个容器槽位伴随持久化条目（支持原版物品与模组物品）
    /// </summary>
    public class ContainerSlotEntry
    {
        public int Slot { get; set; }
        public int Type { get; set; }
        public string ModName { get; set; }
        public string ItemName { get; set; }
        public int Stack { get; set; }
        public int Prefix { get; set; }
        public bool Favorited { get; set; }
        public string CustomData { get; set; }
    }

    /// <summary>
    /// 玩家伴随存档数据结构
    /// </summary>
    public class PlayerSidecarData
    {
        public int SchemaVersion { get; set; } = 1;
        public string PlayerName { get; set; }
        public List<ModItemSaveEntry> Items { get; set; } = new List<ModItemSaveEntry>();

        /// <summary>
        /// 玩家绑定的独立命名扩展容器（如 "BigBag", "AccessoryBox" 等）
        /// 键为容器标识，值为槽位物品数据列表
        /// </summary>
        public Dictionary<string, List<ContainerSlotEntry>> Containers { get; set; } = new Dictionary<string, List<ContainerSlotEntry>>();

        /// <summary>
        /// 模组挂载在玩家上的通用自定义键值对数据
        /// </summary>
        public Dictionary<string, string> CustomProperties { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// 单个模组物块伴随持久化条目
    /// </summary>
    public class ModTileSaveEntry
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string ModName { get; set; }
        public string TileName { get; set; }
        public short FrameX { get; set; }
        public short FrameY { get; set; }
        public byte Color { get; set; }
    }

    /// <summary>
    /// 单个模组物块实体 (ModTileEntity) 伴随持久化条目
    /// </summary>
    public class ModTileEntitySaveEntry
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string ModName { get; set; }
        public string EntityName { get; set; }
        public string CustomData { get; set; }
    }

    /// <summary>
    /// 世界伴随存档数据结构
    /// </summary>
    public class WorldSidecarData
    {
        public int SchemaVersion { get; set; } = 1;
        public string WorldName { get; set; }
        public int WorldID { get; set; }
        public List<ModItemSaveEntry> ChestItems { get; set; } = new List<ModItemSaveEntry>();
        public List<ModItemSaveEntry> TileEntityItems { get; set; } = new List<ModItemSaveEntry>();
        public List<ModTileSaveEntry> ModTiles { get; set; } = new List<ModTileSaveEntry>();
        public List<ModTileEntitySaveEntry> ModTileEntities { get; set; } = new List<ModTileEntitySaveEntry>();
    }

    /// <summary>
    /// TPML 全域 Sidecar 模组物品伴随持久化引擎
    /// 在官方原版 .plr / .wld 二进制流之外建立透明伴随网格，
    /// 保证模组实体物品放在背包、四大银行、世界宝箱、展示架等任何地方均可 100% 完整无损存读档！
    /// 作者: SaintCirno9
    /// </summary>
    public static class ModItemSidecarEngine
    {
        // 暂存写盘期间置空的槽位（写盘后立即在内存中还原）
        private static readonly Dictionary<string, Item> _playerTempSwap = new Dictionary<string, Item>();
        private static readonly Dictionary<string, Item> _worldTempSwap = new Dictionary<string, Item>();
        private static readonly object _ioLock = new object();

        /// <summary>
        /// 当角色切换、离开世界或重置容器时触发的通知事件
        /// </summary>
        public static event Action OnResetContainers;

        /// <summary>
        /// 当玩家数据载入或进入世界时触发的扩展容器载入事件
        /// </summary>
        public static event Action<Player> OnLoadContainers;

        /// <summary>
        /// 广播重置所有扩展容器内存状态并清空驻留数据
        /// </summary>
        public static void ResetContainers()
        {
            try
            {
                OnResetContainers?.Invoke();
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[Sidecar] 容器重置事件触发异常: {ex.Message}");
            }
        }

        public static string GetPlayerSidecarPath(Player player) => SidecarSaveManager.GetPlayerSavePath(player);
        public static string GetPlayerSidecarPath(Player player, PlayerFileData fileData) =>
            SidecarSaveManager.GetPlayerSavePath(player, fileData?.Path);
        public static string GetWorldSidecarPath() => SidecarSaveManager.GetWorldSavePath();

        private static PlayerSidecarData TryReadPlayerSidecar(string path, bool backupCorrupt)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            try
            {
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<PlayerSidecarData>(json);
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[Sidecar] 读取玩家伴随存档失败: {ex.Message}");
                if (backupCorrupt)
                {
                    string bak = AtomicFile.BackupCorrupt(path);
                    if (!string.IsNullOrEmpty(bak))
                    {
                        ModLoader.Log($"[Sidecar] 已将损坏的玩家伴随存档备份为: {bak}");
                    }
                }
                return null;
            }
        }

        private static WorldSidecarData TryReadWorldSidecar(string path, bool backupCorrupt)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            try
            {
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<WorldSidecarData>(json);
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[Sidecar] 读取世界伴随存档失败: {ex.Message}");
                if (backupCorrupt)
                {
                    string bak = AtomicFile.BackupCorrupt(path);
                    if (!string.IsNullOrEmpty(bak))
                    {
                        ModLoader.Log($"[Sidecar] 已将损坏的世界伴随存档备份为: {bak}");
                    }
                }
                return null;
            }
        }

        private static void WritePlayerSidecar(string path, PlayerSidecarData data)
        {
            if (data == null) return;
            data.SchemaVersion = 1;
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            AtomicFile.WriteAllText(path, json);
        }

        private static void WriteWorldSidecar(string path, WorldSidecarData data)
        {
            if (data == null) return;
            data.SchemaVersion = 1;
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            AtomicFile.WriteAllText(path, json);
        }

        #region 通用槽位与扩展容器序列化引擎

        /// <summary>
        /// 将物品槽位数组序列化为 ContainerSlotEntry 列表（自动区分原版物品与 Mod 物品及 TagCompound CustomData）
        /// </summary>
        public static List<ContainerSlotEntry> SerializeSlots(Item[] items)
        {
            var list = new List<ContainerSlotEntry>();
            if (items == null) return list;

            for (int i = 0; i < items.Length; i++)
            {
                Item it = items[i];
                if (it == null || it.IsAir || it.type <= 0) continue;

                string modName = "Terraria";
                string itemName = null;
                string customData = null;

                if (it.type >= ItemID.Count)
                {
                    ModItem modItem = ItemLoader.GetModItem(it);
                    if (modItem != null)
                    {
                        modName = modItem.Mod?.Name ?? "TPML";
                        itemName = modItem.Name;
                        try
                        {
                            TagCompound tag = new TagCompound();
                            modItem.SaveData(tag);
                            if (tag.Count > 0)
                            {
                                customData = JsonConvert.SerializeObject(tag);
                            }
                        }
                        catch (Exception ex)
                        {
                            ModLoader.Log($"[Sidecar] 容器槽位序列化自定义数据异常 [{modItem.FullName}]: {ex.Message}");
                        }
                    }
                }

                list.Add(new ContainerSlotEntry
                {
                    Slot = i,
                    Type = it.type,
                    ModName = modName,
                    ItemName = itemName,
                    Stack = it.stack,
                    Prefix = it.prefix,
                    Favorited = it.favorited,
                    CustomData = customData
                });
            }

            return list;
        }

        /// <summary>
        /// 从 ContainerSlotEntry 列表反序列化回填至目标物品槽位数组
        /// </summary>
        public static void DeserializeSlots(List<ContainerSlotEntry> entries, Item[] targetArray)
        {
            if (targetArray == null) return;
            for (int i = 0; i < targetArray.Length; i++)
            {
                targetArray[i] = new Item();
            }

            if (entries == null) return;

            foreach (var entry in entries)
            {
                if (entry == null || entry.Slot < 0 || entry.Slot >= targetArray.Length) continue;

                int type = 0;
                if (entry.ModName != null && entry.ModName != "Terraria" && !string.IsNullOrEmpty(entry.ItemName))
                {
                    type = ItemLoader.ItemType(entry.ModName, entry.ItemName);
                    if (type <= 0)
                    {
                        ModLoader.Log($"[Sidecar] 容器槽位回填跳过未加载模组物品: [{entry.ModName}/{entry.ItemName}]");
                        continue;
                    }
                }
                else
                {
                    type = entry.Type;
                    if (type <= 0) continue;
                }

                Item it = new Item();
                it.SetDefaults(type);
                it.stack = Math.Max(1, Math.Min(entry.Stack, it.maxStack));
                if (entry.Prefix > 0) it.Prefix(entry.Prefix);
                it.favorited = entry.Favorited;

                if (!string.IsNullOrEmpty(entry.CustomData))
                {
                    try
                    {
                        ModItem modItem = ItemLoader.GetModItem(it);
                        if (modItem != null)
                        {
                            TagCompound tag = JsonConvert.DeserializeObject<TagCompound>(entry.CustomData);
                            if (tag != null)
                            {
                                modItem.LoadData(tag);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ModLoader.Log($"[Sidecar] 容器槽位反序列化自定义数据异常: {ex.Message}");
                    }
                }

                targetArray[entry.Slot] = it;
            }
        }

        /// <summary>
        /// 从 ContainerSlotEntry 列表新建物品槽位数组并反序列化填充（自动比对存档最大槽位索引，自适应扩展容量，防止丢物）
        /// </summary>
        public static Item[] DeserializeSlots(List<ContainerSlotEntry> entries, int capacity)
        {
            int maxSlot = -1;
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i] != null && entries[i].Slot > maxSlot)
                    {
                        maxSlot = entries[i].Slot;
                    }
                }
            }

            int actualCapacity = Math.Max(capacity, maxSlot + 1);
            Item[] slots = new Item[actualCapacity];
            DeserializeSlots(entries, slots);
            return slots;
        }

        /// <summary>
        /// 安全保存指定玩家的命名扩展容器数据至其伴随存档文件
        /// </summary>
        public static void SavePlayerContainer(Player player, string containerKey, Item[] items)
        {
            if (player == null || string.IsNullOrEmpty(containerKey)) return;

            try
            {
                lock (_ioLock)
                {
                    string path = GetPlayerSidecarPath(player);
                    PlayerSidecarData data = TryReadPlayerSidecar(path, backupCorrupt: true);
                    if (data == null)
                    {
                        data = new PlayerSidecarData { PlayerName = player.name };
                    }

                    if (data.Containers == null)
                    {
                        data.Containers = new Dictionary<string, List<ContainerSlotEntry>>();
                    }

                    data.Containers[containerKey] = SerializeSlots(items);
                    WritePlayerSidecar(path, data);
                }
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[Sidecar] 保存玩家容器 [{containerKey}] 伴随数据异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 从伴随存档文件加载指定玩家的命名扩展容器物品数组（根据存档内最大槽位索引动态自适应扩展容量，绝不丢物）
        /// </summary>
        public static Item[] LoadPlayerContainer(Player player, string containerKey, int capacity)
        {
            int baseCap = Math.Max(0, capacity);
            if (player == null || string.IsNullOrEmpty(containerKey))
            {
                Item[] defSlots = new Item[baseCap];
                for (int i = 0; i < baseCap; i++) defSlots[i] = new Item();
                return defSlots;
            }

            try
            {
                string path = GetPlayerSidecarPath(player);
                PlayerSidecarData data = TryReadPlayerSidecar(path, backupCorrupt: true);
                if (data?.Containers != null && data.Containers.TryGetValue(containerKey, out List<ContainerSlotEntry> entries) && entries != null)
                {
                    int maxSlot = -1;
                    for (int i = 0; i < entries.Count; i++)
                    {
                        if (entries[i] != null && entries[i].Slot > maxSlot)
                        {
                            maxSlot = entries[i].Slot;
                        }
                    }

                    int actualCapacity = Math.Max(baseCap, maxSlot + 1);
                    Item[] slots = new Item[actualCapacity];
                    DeserializeSlots(entries, slots);
                    return slots;
                }
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[Sidecar] 加载玩家容器 [{containerKey}] 伴随数据异常: {ex.Message}");
            }

            Item[] fallbackSlots = new Item[baseCap];
            for (int i = 0; i < baseCap; i++) fallbackSlots[i] = new Item();
            return fallbackSlots;
        }

        /// <summary>
        /// 保存指定玩家的伴随自定义属性键值对（存入 PlayerSidecarData.CustomProperties）
        /// </summary>
        public static void SavePlayerCustomProperty(Player player, string key, string value)
        {
            if (player == null || string.IsNullOrEmpty(key)) return;

            try
            {
                lock (_ioLock)
                {
                    string path = GetPlayerSidecarPath(player);
                    bool fileExists = File.Exists(path);
                    PlayerSidecarData data = TryReadPlayerSidecar(path, backupCorrupt: true);

                    if (value == null)
                    {
                        if (!fileExists || data == null) return;
                        if (data.CustomProperties == null || !data.CustomProperties.ContainsKey(key)) return;
                        data.CustomProperties.Remove(key);
                    }
                    else
                    {
                        if (data == null)
                        {
                            data = new PlayerSidecarData { PlayerName = player.name };
                        }

                        if (data.CustomProperties == null)
                        {
                            data.CustomProperties = new Dictionary<string, string>();
                        }

                        data.CustomProperties[key] = value;
                    }

                    WritePlayerSidecar(path, data);
                }
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[Sidecar] 保存玩家自定义属性 [{key}] 伴随数据异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载指定玩家的伴随自定义属性值（从 PlayerSidecarData.CustomProperties 读取）
        /// </summary>
        public static string LoadPlayerCustomProperty(Player player, string key)
        {
            if (player == null || string.IsNullOrEmpty(key)) return null;

            try
            {
                string path = GetPlayerSidecarPath(player);
                PlayerSidecarData data = TryReadPlayerSidecar(path, backupCorrupt: true);
                if (data?.CustomProperties != null && data.CustomProperties.TryGetValue(key, out string val))
                {
                    return val;
                }
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[Sidecar] 加载玩家自定义属性 [{key}] 伴随数据异常: {ex.Message}");
            }

            return null;
        }

        #endregion

        #region 玩家全域槽位持久化

        /// <summary>
        /// 原版写盘前：扫描所有模组物品，生成伴随快照并临时置空原版槽位（防止原版写盘报非法 ID）
        /// </summary>
        public static void OnPlayerSavePrefix(Player player, PlayerFileData fileData = null)
        {
            if (player == null) return;
            _playerTempSwap.Clear();

            string path = fileData != null ? GetPlayerSidecarPath(player, fileData) : GetPlayerSidecarPath(player);
            PlayerSidecarData data;
            lock (_ioLock)
            {
                data = TryReadPlayerSidecar(path, backupCorrupt: true);
            }

            if (data == null)
            {
                data = new PlayerSidecarData { PlayerName = player.name };
            }
            else
            {
                data.PlayerName = player.name;
                if (data.Items == null) data.Items = new List<ModItemSaveEntry>();
                else data.Items.Clear();
            }

            void CheckAndExtract(Item[] array, string prefix)
            {
                if (array == null) return;
                for (int i = 0; i < array.Length; i++)
                {
                    Item it = array[i];
                    if (it != null && !it.IsAir && it.type >= ItemID.Count)
                    {
                        ModItem modItem = ItemLoader.GetModItem(it);
                        if (modItem != null)
                        {
                            string loc = $"{prefix}_{i}";
                            string customData = null;

                            try
                            {
                                TagCompound tag = new TagCompound();
                                modItem.SaveData(tag);
                                if (tag.Count > 0)
                                {
                                    customData = JsonConvert.SerializeObject(tag);
                                }
                            }
                            catch (Exception ex)
                            {
                                ModLoader.Log($"[Sidecar] 序列化物品自定义数据异常 [{modItem.FullName}]: {ex.Message}");
                            }

                            data.Items.Add(new ModItemSaveEntry
                            {
                                Location = loc,
                                ModName = modItem.Mod?.Name ?? "TPML",
                                ItemName = modItem.Name,
                                Stack = it.stack,
                                Prefix = it.prefix,
                                Favorited = it.favorited,
                                CustomData = customData
                            });

                            _playerTempSwap[loc] = it;
                            array[i] = new Item(); // 临时替换为空气
                        }
                    }
                }
            }

            CheckAndExtract(player.inventory, "inv");
            CheckAndExtract(player.armor, "armor");
            CheckAndExtract(player.dye, "dye");
            CheckAndExtract(player.miscEquips, "miscEquip");
            CheckAndExtract(player.miscDyes, "miscDye");
            if (player.bank != null) CheckAndExtract(player.bank.item, "bank1");
            if (player.bank2 != null) CheckAndExtract(player.bank2.item, "bank2");
            if (player.bank3 != null) CheckAndExtract(player.bank3.item, "bank3");
            if (player.bank4 != null) CheckAndExtract(player.bank4.item, "bank4");

            // 装备配装方案 (Loadouts)
            if (player.Loadouts != null)
            {
                for (int l = 0; l < player.Loadouts.Length; l++)
                {
                    var loadout = player.Loadouts[l];
                    if (loadout != null)
                    {
                        CheckAndExtract(loadout.Armor, $"loadout_{l}_armor");
                        CheckAndExtract(loadout.Dye, $"loadout_{l}_dye");
                    }
                }
            }

            // 垃圾桶槽位
            if (player.trashItem != null && !player.trashItem.IsAir && player.trashItem.type >= ItemID.Count)
            {
                ModItem modItem = ItemLoader.GetModItem(player.trashItem);
                if (modItem != null)
                {
                    string customData = null;
                    try
                    {
                        TagCompound tag = new TagCompound();
                        modItem.SaveData(tag);
                        if (tag.Count > 0)
                        {
                            customData = JsonConvert.SerializeObject(tag);
                        }
                    }
                    catch { }

                    data.Items.Add(new ModItemSaveEntry
                    {
                        Location = "trash",
                        ModName = modItem.Mod?.Name ?? "TPML",
                        ItemName = modItem.Name,
                        Stack = player.trashItem.stack,
                        Prefix = player.trashItem.prefix,
                        Favorited = player.trashItem.favorited,
                        CustomData = customData
                    });
                    _playerTempSwap["trash"] = player.trashItem;
                    player.trashItem = new Item();
                }
            }

            // 保存所有已注册 ModPlayer 的自定义数据
            try
            {
                if (data.CustomProperties == null) data.CustomProperties = new Dictionary<string, string>();
                foreach (var mpTemplate in ModContent.GetContent<ModPlayer>())
                {
                    var mp = player.GetModPlayer(mpTemplate.GetType());
                    if (mp != null)
                    {
                        var tag = new TagCompound();
                        mp.SaveData(tag);
                        if (tag.Count > 0)
                        {
                            string key = $"ModPlayer_{mp.GetType().FullName}";
                            data.CustomProperties[key] = JsonConvert.SerializeObject(tag);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[Sidecar] 收集 ModPlayer 数据异常: {ex.Message}");
            }

            try
            {
                lock (_ioLock)
                {
                    WritePlayerSidecar(path, data);
                }
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[Sidecar] 保存玩家伴随数据异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 原版写盘后：立即将暂存的模组物品在内存中复原回槽位
        /// </summary>
        public static void OnPlayerSavePostfix(Player player)
        {
            if (player == null || _playerTempSwap.Count == 0) return;

            void RestoreSlot(Item[] array, string prefix)
            {
                if (array == null) return;
                for (int i = 0; i < array.Length; i++)
                {
                    string loc = $"{prefix}_{i}";
                    if (_playerTempSwap.TryGetValue(loc, out Item it))
                    {
                        array[i] = it;
                    }
                }
            }

            RestoreSlot(player.inventory, "inv");
            RestoreSlot(player.armor, "armor");
            RestoreSlot(player.dye, "dye");
            RestoreSlot(player.miscEquips, "miscEquip");
            RestoreSlot(player.miscDyes, "miscDye");
            if (player.bank != null) RestoreSlot(player.bank.item, "bank1");
            if (player.bank2 != null) RestoreSlot(player.bank2.item, "bank2");
            if (player.bank3 != null) RestoreSlot(player.bank3.item, "bank3");
            if (player.bank4 != null) RestoreSlot(player.bank4.item, "bank4");

            if (player.Loadouts != null)
            {
                for (int l = 0; l < player.Loadouts.Length; l++)
                {
                    var loadout = player.Loadouts[l];
                    if (loadout != null)
                    {
                        RestoreSlot(loadout.Armor, $"loadout_{l}_armor");
                        RestoreSlot(loadout.Dye, $"loadout_{l}_dye");
                    }
                }
            }

            if (_playerTempSwap.TryGetValue("trash", out Item trash))
            {
                player.trashItem = trash;
            }

            _playerTempSwap.Clear();
        }

        /// <summary>
        /// 读盘加载或玩家进入世界：从伴随文件动态解析并回填模组物品
        /// </summary>
        public static void OnPlayerLoaded(Player player)
        {
            if (player == null) return;
            string path = GetPlayerSidecarPath(player);

            try
            {
                PlayerSidecarData data = TryReadPlayerSidecar(path, backupCorrupt: true);
                if (data?.Items != null)
                {
                foreach (var entry in data.Items)
                {
                    int type = ItemLoader.ItemType(entry.ModName, entry.ItemName);
                    if (type <= 0)
                    {
                        ModLoader.Log($"[Sidecar] 回填玩家物品跳过未加载项: [{entry.ModName}/{entry.ItemName}]");
                        continue;
                    }

                    Item item = new Item();
                    item.SetDefaults(type);
                    item.stack = Math.Max(1, Math.Min(entry.Stack, item.maxStack));
                    if (entry.Prefix > 0) item.Prefix(entry.Prefix);
                    item.favorited = entry.Favorited;

                    if (!string.IsNullOrEmpty(entry.CustomData))
                    {
                        try
                        {
                            ModItem modItem = ItemLoader.GetModItem(item);
                            if (modItem != null)
                            {
                                TagCompound tag = JsonConvert.DeserializeObject<TagCompound>(entry.CustomData);
                                if (tag != null)
                                {
                                    modItem.LoadData(tag);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ModLoader.Log($"[Sidecar] 反序列化物品 [{item?.Name ?? "null"}] (Type={type}) 自定义数据异常: {ex}");
                        }
                    }

                    string loc = entry.Location;
                    if (loc == "trash")
                    {
                        player.trashItem = item;
                        continue;
                    }

                    // 处理 loadout_{l}_armor_{i} 和 loadout_{l}_dye_{i}
                    if (loc.StartsWith("loadout_"))
                    {
                        string[] parts = loc.Split('_');
                        if (parts.Length == 4 && int.TryParse(parts[1], out int l) && int.TryParse(parts[3], out int idx))
                        {
                            if (player.Loadouts != null && l >= 0 && l < player.Loadouts.Length && player.Loadouts[l] != null)
                            {
                                if (parts[2] == "armor" && idx >= 0 && idx < player.Loadouts[l].Armor.Length)
                                {
                                    player.Loadouts[l].Armor[idx] = item;
                                }
                                else if (parts[2] == "dye" && idx >= 0 && idx < player.Loadouts[l].Dye.Length)
                                {
                                    player.Loadouts[l].Dye[idx] = item;
                                }
                            }
                        }
                        continue;
                    }

                    string[] simpleParts = loc.Split('_');
                    if (simpleParts.Length < 2) continue;

                    string kind = simpleParts[0];
                    if (!int.TryParse(simpleParts[1], out int index)) continue;

                    switch (kind)
                    {
                        case "inv":
                            if (index >= 0 && index < player.inventory.Length) player.inventory[index] = item;
                            break;
                        case "armor":
                            if (index >= 0 && index < player.armor.Length) player.armor[index] = item;
                            break;
                        case "dye":
                            if (index >= 0 && index < player.dye.Length) player.dye[index] = item;
                            break;
                        case "miscEquip":
                            if (index >= 0 && index < player.miscEquips.Length) player.miscEquips[index] = item;
                            break;
                        case "miscDye":
                            if (index >= 0 && index < player.miscDyes.Length) player.miscDyes[index] = item;
                            break;
                        case "bank1":
                            if (player.bank != null && index >= 0 && index < player.bank.item.Length) player.bank.item[index] = item;
                            break;
                        case "bank2":
                            if (player.bank2 != null && index >= 0 && index < player.bank2.item.Length) player.bank2.item[index] = item;
                            break;
                        case "bank3":
                            if (player.bank3 != null && index >= 0 && index < player.bank3.item.Length) player.bank3.item[index] = item;
                            break;
                        case "bank4":
                            if (player.bank4 != null && index >= 0 && index < player.bank4.item.Length) player.bank4.item[index] = item;
                            break;
                    }
                }

                    // 自动回填所有已注册 ModPlayer 的自定义数据
                    if (data?.CustomProperties != null)
                    {
                        foreach (var mpTemplate in ModContent.GetContent<ModPlayer>())
                        {
                            var mp = player.GetModPlayer(mpTemplate.GetType());
                            if (mp != null)
                            {
                                string key = $"ModPlayer_{mp.GetType().FullName}";
                                if (data.CustomProperties.TryGetValue(key, out string jsonStr) && !string.IsNullOrEmpty(jsonStr))
                                {
                                    try
                                    {
                                        var tag = JsonConvert.DeserializeObject<TagCompound>(jsonStr);
                                        if (tag != null)
                                        {
                                            mp.LoadData(tag);
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[Sidecar] 加载玩家伴随数据异常: {ex.Message}");
            }
            finally
            {
                // 广播扩展容器加载事件，确保大背包与饰品箱等扩展容器同步为该玩家载入数据
                try
                {
                    OnLoadContainers?.Invoke(player);
                }
                catch (Exception ex)
                {
                    ModLoader.Log($"[Sidecar] 扩展容器加载事件广播异常: {ex.Message}");
                }
            }
        }

        #endregion

        #region 世界物理宝箱与展示架持久化

        private struct TileStateSnapshot
        {
            public ushort Type;
            public short FrameX;
            public short FrameY;
            public byte Color;
        }
        private static readonly Dictionary<Point16, TileStateSnapshot> _worldTileTempSwap = new Dictionary<Point16, TileStateSnapshot>();

        /// <summary>
        /// 世界写盘前：扫描所有箱子与展示架中的模组物品及全图 ModTile / ModTileEntity，生成伴随快照并临时置空槽位与图格
        /// </summary>
        public static void OnWorldSavePrefix()
        {
            _worldTempSwap.Clear();
            _worldTileTempSwap.Clear();

            WorldSidecarData data = new WorldSidecarData
            {
                WorldName = Main.worldName,
                WorldID = Main.worldID
            };

            // 1. 扫描世界宝箱
            if (Main.chest != null)
            {
                for (int c = 0; c < Main.chest.Length; c++)
                {
                    Chest chest = Main.chest[c];
                    if (chest?.item == null) continue;

                    for (int s = 0; s < chest.item.Length; s++)
                    {
                        Item it = chest.item[s];
                        if (it != null && !it.IsAir && it.type >= ItemID.Count)
                        {
                            ModItem modItem = ItemLoader.GetModItem(it);
                            if (modItem != null)
                            {
                                string loc = $"chest_{c}_{s}";
                                string customData = null;
                                try
                                {
                                    TagCompound tag = new TagCompound();
                                    modItem.SaveData(tag);
                                    if (tag.Count > 0)
                                    {
                                        customData = JsonConvert.SerializeObject(tag);
                                    }
                                }
                                catch { }

                                data.ChestItems.Add(new ModItemSaveEntry
                                {
                                    Location = loc,
                                    ModName = modItem.Mod?.Name ?? "TPML",
                                    ItemName = modItem.Name,
                                    Stack = it.stack,
                                    Prefix = it.prefix,
                                    Favorited = it.favorited,
                                    CustomData = customData
                                });

                                _worldTempSwap[loc] = it;
                                chest.item[s] = new Item();
                            }
                        }
                    }
                }
            }

            // 2. 扫描 TileEntity (原版展示柜与模组 ModTileEntity)
            if (TileEntity.ByID != null)
            {
                foreach (var kvp in TileEntity.ByID)
                {
                    try
                    {
                        int id = kvp.Key;
                        TileEntity te = kvp.Value;
                        if (te == null) continue;

                        if (te is ModTileEntity mte)
                        {
                            string customData = null;
                            try
                            {
                                TagCompound tag = new TagCompound();
                                mte.SaveData(tag);
                                if (tag.Count > 0) customData = JsonConvert.SerializeObject(tag);
                            }
                            catch { }

                            data.ModTileEntities.Add(new ModTileEntitySaveEntry
                            {
                                X = mte.Position.X,
                                Y = mte.Position.Y,
                                ModName = mte.Mod?.Name ?? "TPML",
                                EntityName = mte.Name,
                                CustomData = customData
                            });
                        }
                        else if (te is TEItemFrame frame)
                        {
                            ExtractTileEntityItem(frame.item, $"te_itemframe_{id}", it => frame.item = it, data.TileEntityItems);
                        }
                        else if (te is TEWeaponsRack weaponRack)
                        {
                            ExtractTileEntityItem(weaponRack.item, $"te_weaponrack_{id}", it => weaponRack.item = it, data.TileEntityItems);
                        }
                        else if (te is TEFoodPlatter foodPlatter)
                        {
                            ExtractTileEntityItem(foodPlatter.item, $"te_foodplatter_{id}", it => foodPlatter.item = it, data.TileEntityItems);
                        }
                        else if (te is TEDisplayDoll doll)
                        {
                            ExtractTileEntityArray(doll._equip, $"te_displaydoll_{id}_equip", data.TileEntityItems);
                            ExtractTileEntityArray(doll._dyes, $"te_displaydoll_{id}_dye", data.TileEntityItems);
                            ExtractTileEntityArray(doll._misc, $"te_displaydoll_{id}_misc", data.TileEntityItems);
                        }
                        else if (te is TEHatRack hatRack)
                        {
                            ExtractTileEntityArray(hatRack._items, $"te_hatrack_{id}_item", data.TileEntityItems);
                            ExtractTileEntityArray(hatRack._dyes, $"te_hatrack_{id}_dye", data.TileEntityItems);
                        }
                    }
                    catch (Exception ex)
                    {
                        ModLoader.Log($"[SidecarSave] 序列化 TileEntity 异常: {ex.Message}");
                    }
                }
            }

            // 3. 扫描世界中的 ModTile 并临时置空防止 .wld 异常
            if (Main.tile != null)
            {
                for (int x = 0; x < Main.maxTilesX; x++)
                {
                    for (int y = 0; y < Main.maxTilesY; y++)
                    {
                        Tile t = Main.tile[x, y];
                        if (t.active() && t.type >= TileLoader.ModTileOffset)
                        {
                            ModTile modTile = TileLoader.GetTile(t.type);
                            if (modTile != null)
                            {
                                data.ModTiles.Add(new ModTileSaveEntry
                                {
                                    X = x,
                                    Y = y,
                                    ModName = modTile.Mod?.Name ?? "TPML",
                                    TileName = modTile.Name,
                                    FrameX = t.frameX,
                                    FrameY = t.frameY,
                                    Color = t.color()
                                });

                                _worldTileTempSwap[new Point16(x, y)] = new TileStateSnapshot
                                {
                                    Type = t.type,
                                    FrameX = t.frameX,
                                    FrameY = t.frameY,
                                    Color = t.color()
                                };

                                t.active(false);
                            }
                        }
                    }
                }
            }

            try
            {
                lock (_ioLock)
                {
                    WriteWorldSidecar(GetWorldSidecarPath(), data);
                }
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[Sidecar] 保存世界伴随数据异常: {ex.Message}");
            }
        }

        private static void ExtractTileEntityItem(Item it, string loc, Action<Item> setter, List<ModItemSaveEntry> list)
        {
            if (it != null && !it.IsAir && it.type >= ItemID.Count)
            {
                ModItem modItem = ItemLoader.GetModItem(it);
                if (modItem != null)
                {
                    string customData = null;
                    try
                    {
                        TagCompound tag = new TagCompound();
                        modItem.SaveData(tag);
                        if (tag.Count > 0) customData = JsonConvert.SerializeObject(tag);
                    }
                    catch { }

                    list.Add(new ModItemSaveEntry
                    {
                        Location = loc,
                        ModName = modItem.Mod?.Name ?? "TPML",
                        ItemName = modItem.Name,
                        Stack = it.stack,
                        Prefix = it.prefix,
                        Favorited = it.favorited,
                        CustomData = customData
                    });

                    _worldTempSwap[loc] = it;
                    setter(new Item());
                }
            }
        }

        private static void ExtractTileEntityArray(Item[] array, string prefix, List<ModItemSaveEntry> list)
        {
            if (array == null) return;
            for (int i = 0; i < array.Length; i++)
            {
                Item it = array[i];
                if (it != null && !it.IsAir && it.type >= ItemID.Count)
                {
                    ModItem modItem = ItemLoader.GetModItem(it);
                    if (modItem != null)
                    {
                        string loc = $"{prefix}_{i}";
                        string customData = null;
                        try
                        {
                            TagCompound tag = new TagCompound();
                            modItem.SaveData(tag);
                            if (tag.Count > 0) customData = JsonConvert.SerializeObject(tag);
                        }
                        catch { }

                        list.Add(new ModItemSaveEntry
                        {
                            Location = loc,
                            ModName = modItem.Mod?.Name ?? "TPML",
                            ItemName = modItem.Name,
                            Stack = it.stack,
                            Prefix = it.prefix,
                            Favorited = it.favorited,
                            CustomData = customData
                        });

                        _worldTempSwap[loc] = it;
                        array[i] = new Item();
                    }
                }
            }
        }

        /// <summary>
        /// 世界写盘后：立即在内存中还原箱子、展示架与 ModTile 图格
        /// </summary>
        public static void OnWorldSavePostfix()
        {
            // 还原世界宝箱与展示架槽位
            if (_worldTempSwap.Count > 0)
            {
                foreach (var kvp in _worldTempSwap)
                {
                    string loc = kvp.Key;
                    Item it = kvp.Value;

                    if (loc.StartsWith("chest_") && Main.chest != null)
                    {
                        string[] parts = loc.Split('_');
                        if (parts.Length == 3 && int.TryParse(parts[1], out int c) && int.TryParse(parts[2], out int s))
                        {
                            if (c >= 0 && c < Main.chest.Length && Main.chest[c]?.item != null && s >= 0 && s < Main.chest[c].item.Length)
                            {
                                Main.chest[c].item[s] = it;
                            }
                        }
                    }
                    else if (loc.StartsWith("te_") && TileEntity.ByID != null)
                    {
                        string[] parts = loc.Split('_');
                        if (parts.Length == 3 && int.TryParse(parts[2], out int id) && TileEntity.ByID.TryGetValue(id, out TileEntity te))
                        {
                            if (te is TEItemFrame frame) frame.item = it;
                            else if (te is TEWeaponsRack rack) rack.item = it;
                            else if (te is TEFoodPlatter platter) platter.item = it;
                        }
                        else if (parts.Length == 5 && int.TryParse(parts[2], out int teId) && int.TryParse(parts[4], out int idx) && TileEntity.ByID.TryGetValue(teId, out TileEntity arrayTe))
                        {
                            if (arrayTe is TEDisplayDoll doll)
                            {
                                if (parts[3] == "equip" && idx >= 0 && idx < doll._equip.Length) doll._equip[idx] = it;
                                else if (parts[3] == "dye" && idx >= 0 && idx < doll._dyes.Length) doll._dyes[idx] = it;
                                else if (parts[3] == "misc" && idx >= 0 && idx < doll._misc.Length) doll._misc[idx] = it;
                            }
                            else if (arrayTe is TEHatRack hatRack)
                            {
                                if (parts[3] == "item" && idx >= 0 && idx < hatRack._items.Length) hatRack._items[idx] = it;
                                else if (parts[3] == "dye" && idx >= 0 && idx < hatRack._dyes.Length) hatRack._dyes[idx] = it;
                            }
                        }
                    }
                }
                _worldTempSwap.Clear();
            }

            // 还原 ModTile 图格
            if (_worldTileTempSwap.Count > 0)
            {
                foreach (var kvp in _worldTileTempSwap)
                {
                    Point16 pos = kvp.Key;
                    TileStateSnapshot snap = kvp.Value;
                    Tile t = Framing.GetTileSafely(pos.X, pos.Y);
                    t.active(true);
                    t.type = snap.Type;
                    t.frameX = snap.FrameX;
                    t.frameY = snap.FrameY;
                    t.color(snap.Color);
                }
                _worldTileTempSwap.Clear();
            }
        }

        /// <summary>
        /// 世界加载完成后：从伴随文件回填世界所有箱子、展示架与 ModTile / ModTileEntity
        /// </summary>
        public static void OnWorldLoaded()
        {
            string path = GetWorldSidecarPath();
            if (!File.Exists(path)) return;

            try
            {
                WorldSidecarData data = TryReadWorldSidecar(path, backupCorrupt: true);
                if (data == null) return;

                void RestoreEntry(ModItemSaveEntry entry)
                {
                    if (entry == null) return;
                    int type = ItemLoader.ItemType(entry.ModName, entry.ItemName);
                    if (type <= 0)
                    {
                        ModLoader.Log($"[Sidecar] 回填世界物品跳过未加载项: [{entry.ModName}/{entry.ItemName}]");
                        return;
                    }

                    Item item = new Item();
                    item.SetDefaults(type);
                    item.stack = Math.Max(1, Math.Min(entry.Stack, item.maxStack));
                    if (entry.Prefix > 0) item.Prefix(entry.Prefix);
                    item.favorited = entry.Favorited;

                    if (!string.IsNullOrEmpty(entry.CustomData))
                    {
                        try
                        {
                            ModItem modItem = ItemLoader.GetModItem(item);
                            if (modItem != null)
                            {
                                TagCompound tag = JsonConvert.DeserializeObject<TagCompound>(entry.CustomData);
                                if (tag != null) modItem.LoadData(tag);
                            }
                        }
                        catch { }
                    }

                    string loc = entry.Location;
                    if (loc.StartsWith("chest_") && Main.chest != null)
                    {
                        string[] parts = loc.Split('_');
                        if (parts.Length == 3 && int.TryParse(parts[1], out int c) && int.TryParse(parts[2], out int s))
                        {
                            if (c >= 0 && c < Main.chest.Length && Main.chest[c]?.item != null && s >= 0 && s < Main.chest[c].item.Length)
                            {
                                Main.chest[c].item[s] = item;
                            }
                        }
                    }
                    else if (loc.StartsWith("te_") && TileEntity.ByID != null)
                    {
                        string[] parts = loc.Split('_');
                        if (parts.Length == 3 && int.TryParse(parts[2], out int id) && TileEntity.ByID.TryGetValue(id, out TileEntity te))
                        {
                            if (te is TEItemFrame frame) frame.item = item;
                            else if (te is TEWeaponsRack rack) rack.item = item;
                            else if (te is TEFoodPlatter platter) platter.item = item;
                        }
                        else if (parts.Length == 5 && int.TryParse(parts[2], out int teId) && int.TryParse(parts[4], out int idx) && TileEntity.ByID.TryGetValue(teId, out TileEntity arrayTe))
                        {
                            if (arrayTe is TEDisplayDoll doll)
                            {
                                if (parts[3] == "equip" && idx >= 0 && idx < doll._equip.Length) doll._equip[idx] = item;
                                else if (parts[3] == "dye" && idx >= 0 && idx < doll._dyes.Length) doll._dyes[idx] = item;
                                else if (parts[3] == "misc" && idx >= 0 && idx < doll._misc.Length) doll._misc[idx] = item;
                            }
                            else if (arrayTe is TEHatRack hatRack)
                            {
                                if (parts[3] == "item" && idx >= 0 && idx < hatRack._items.Length) hatRack._items[idx] = item;
                                else if (parts[3] == "dye" && idx >= 0 && idx < hatRack._dyes.Length) hatRack._dyes[idx] = item;
                            }
                        }
                    }
                }

                if (data.ChestItems != null)
                {
                    foreach (var entry in data.ChestItems) RestoreEntry(entry);
                }

                if (data.TileEntityItems != null)
                {
                    foreach (var entry in data.TileEntityItems) RestoreEntry(entry);
                }

                // 回填 ModTile
                if (data.ModTiles != null)
                {
                    foreach (var tileEntry in data.ModTiles)
                    {
                        int type = TileLoader.TileType(tileEntry.ModName, tileEntry.TileName);
                        if (type > 0)
                        {
                            Tile t = Framing.GetTileSafely(tileEntry.X, tileEntry.Y);
                            t.active(true);
                            t.type = (ushort)type;
                            t.frameX = tileEntry.FrameX;
                            t.frameY = tileEntry.FrameY;
                            t.color(tileEntry.Color);
                        }
                    }
                }

                // 回填 ModTileEntity
                if (data.ModTileEntities != null)
                {
                    foreach (var entEntry in data.ModTileEntities)
                    {
                        int entType = TileEntityLoader.TileEntityType($"{entEntry.ModName}/{entEntry.EntityName}");
                        ModTileEntity template = TileEntityLoader.GetEntity(entType);
                        if (template != null)
                        {
                            int id = template.Place(entEntry.X, entEntry.Y);
                            if (TileEntity.ByID.TryGetValue(id, out TileEntity placedTe) && placedTe is ModTileEntity placedMte)
                            {
                                if (!string.IsNullOrEmpty(entEntry.CustomData))
                                {
                                    try
                                    {
                                        TagCompound tag = JsonConvert.DeserializeObject<TagCompound>(entEntry.CustomData);
                                        if (tag != null) placedMte.LoadData(tag);
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[Sidecar] 加载世界伴随数据异常: {ex.Message}");
            }
        }

        #endregion
    }
}
