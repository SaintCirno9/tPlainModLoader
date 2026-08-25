using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Tile_Entities;
using Terraria.ID;

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
    /// 玩家伴随存档数据结构
    /// </summary>
    public class PlayerSidecarData
    {
        public string PlayerName { get; set; }
        public List<ModItemSaveEntry> Items { get; set; } = new List<ModItemSaveEntry>();
    }

    /// <summary>
    /// 世界伴随存档数据结构
    /// </summary>
    public class WorldSidecarData
    {
        public string WorldName { get; set; }
        public int WorldID { get; set; }
        public List<ModItemSaveEntry> ChestItems { get; set; } = new List<ModItemSaveEntry>();
        public List<ModItemSaveEntry> TileEntityItems { get; set; } = new List<ModItemSaveEntry>();
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

        public static string GetPlayerSidecarPath(Player player) => SidecarSaveManager.GetPlayerSavePath(player);
        public static string GetWorldSidecarPath() => SidecarSaveManager.GetWorldSavePath();

        #region 玩家全域槽位持久化

        /// <summary>
        /// 原版写盘前：扫描所有模组物品，生成伴随快照并临时置空原版槽位（防止原版写盘报非法 ID）
        /// </summary>
        public static void OnPlayerSavePrefix(Player player)
        {
            if (player == null) return;
            _playerTempSwap.Clear();

            PlayerSidecarData data = new PlayerSidecarData
            {
                PlayerName = player.name
            };

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

            // 保存伴随文件
            try
            {
                string path = GetPlayerSidecarPath(player);
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(path, json);
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
            if (!File.Exists(path)) return;

            try
            {
                string json = File.ReadAllText(path);
                PlayerSidecarData data = JsonConvert.DeserializeObject<PlayerSidecarData>(json);
                if (data?.Items == null) return;

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
                            ModLoader.Log($"[Sidecar] 反序列化物品自定义数据异常: {ex.Message}");
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
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[Sidecar] 加载玩家伴随数据异常: {ex.Message}");
            }
        }

        #endregion

        #region 世界物理宝箱与展示架持久化

        /// <summary>
        /// 世界写盘前：扫描所有箱子与展示架中的模组物品，生成伴随快照并临时置空槽位
        /// </summary>
        public static void OnWorldSavePrefix()
        {
            _worldTempSwap.Clear();

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

            // 2. 扫描 TileEntity (展示框、武器架、展示假人、帽子架、盛餐盘等)
            if (TileEntity.ByID != null)
            {
                foreach (var kvp in TileEntity.ByID)
                {
                    int id = kvp.Key;
                    TileEntity te = kvp.Value;
                    if (te == null) continue;

                    if (te is TEItemFrame frame)
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
            }

            try
            {
                string path = GetWorldSidecarPath();
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(path, json);
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
        /// 世界写盘后：立即在内存中还原箱子与展示架槽位
        /// </summary>
        public static void OnWorldSavePostfix()
        {
            if (_worldTempSwap.Count == 0) return;

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
                    // te_itemframe_{id}, te_weaponrack_{id}, te_foodplatter_{id}
                    if (parts.Length == 3 && int.TryParse(parts[2], out int id) && TileEntity.ByID.TryGetValue(id, out TileEntity te))
                    {
                        if (te is TEItemFrame frame) frame.item = it;
                        else if (te is TEWeaponsRack rack) rack.item = it;
                        else if (te is TEFoodPlatter platter) platter.item = it;
                    }
                    // te_displaydoll_{id}_equip_{i}, te_displaydoll_{id}_dye_{i}, te_displaydoll_{id}_misc_{i}, te_hatrack_{id}_item_{i}
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

        /// <summary>
        /// 世界加载完成后：从伴随文件回填世界所有箱子与展示架中的模组物品
        /// </summary>
        public static void OnWorldLoaded()
        {
            string path = GetWorldSidecarPath();
            if (!File.Exists(path)) return;

            try
            {
                string json = File.ReadAllText(path);
                WorldSidecarData data = JsonConvert.DeserializeObject<WorldSidecarData>(json);
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
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[Sidecar] 加载世界伴随数据异常: {ex.Message}");
            }
        }

        #endregion
    }
}
