using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Terraria;
using Terraria.ID;
using Terraria.IO;

namespace Terraria.ModLoader.IO
{
    /// <summary>
    /// 双轨存档隔离机制：将自定义 Mod 物品数据与原版 .plr 存档隔离，
    /// 确保即使存档在原版纯净客户端加载也不会发生崩溃或数据破损。
    /// </summary>
    public static class SidecarSaveManager
    {
        public class SavedItemRecord
        {
            public string Container { get; set; }
            public int SlotIndex { get; set; }
            public string ModName { get; set; }
            public string ItemName { get; set; }
            public int Stack { get; set; }
            public int Prefix { get; set; }
            public int Favorited { get; set; }
        }

        private static readonly Dictionary<int, Item> _tempSavedItems = new Dictionary<int, Item>();

        public static string GetSidecarFilePath(string playerPath)
        {
            if (string.IsNullOrEmpty(playerPath)) return null;
            return Path.ChangeExtension(playerPath, ".tpml_items.json");
        }

        public static void OnSavePlayerPrefix(PlayerFileData playerFile)
        {
            if (playerFile?.Player == null) return;
            Player player = playerFile.Player;
            string sidecarPath = GetSidecarFilePath(playerFile.Path);
            if (string.IsNullOrEmpty(sidecarPath)) return;

            var records = new List<SavedItemRecord>();
            _tempSavedItems.Clear();

            void ScanAndSanitize(Item[] array, string containerName, int offset)
            {
                if (array == null) return;
                for (int i = 0; i < array.Length; i++)
                {
                    Item item = array[i];
                    if (item != null && !item.IsAir && item.type >= ItemID.Count)
                    {
                        var modItem = ItemLoader.GetItem(item.type);
                        if (modItem != null)
                        {
                            records.Add(new SavedItemRecord
                            {
                                Container = containerName,
                                SlotIndex = i,
                                ModName = modItem.Mod?.Name ?? "Unknown",
                                ItemName = modItem.Name,
                                Stack = item.stack,
                                Prefix = item.prefix,
                                Favorited = item.favorited ? 1 : 0
                            });

                            int globalKey = offset + i;
                            _tempSavedItems[globalKey] = item.Clone();

                            // 临时置空，确保原版 .plr 序列化只看到合法 ID
                            array[i] = new Item();
                        }
                    }
                }
            }

            ScanAndSanitize(player.inventory, "inventory", 0);
            ScanAndSanitize(player.armor, "armor", 1000);
            ScanAndSanitize(player.dye, "dye", 2000);
            ScanAndSanitize(player.miscEquips, "miscEquips", 3000);
            ScanAndSanitize(player.miscDyes, "miscDyes", 4000);
            ScanAndSanitize(player.bank?.item, "bank", 5000);
            ScanAndSanitize(player.bank2?.item, "bank2", 6000);
            ScanAndSanitize(player.bank3?.item, "bank3", 7000);
            ScanAndSanitize(player.bank4?.item, "bank4", 8000);

            try
            {
                if (records.Count > 0)
                {
                    string json = JsonConvert.SerializeObject(records, Formatting.Indented);
                    File.WriteAllText(sidecarPath, json);
                    ModLoader.Log($"[SidecarSave] 已导出 {records.Count} 个自定义物品至伴生存档: {Path.GetFileName(sidecarPath)}");
                }
                else if (File.Exists(sidecarPath))
                {
                    File.Delete(sidecarPath);
                }
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[SidecarSave] 写入伴生存档异常: {ex.Message}");
            }
        }

        public static void OnSavePlayerPostfix(PlayerFileData playerFile)
        {
            if (playerFile?.Player == null || _tempSavedItems.Count == 0) return;
            Player player = playerFile.Player;

            void Restore(Item[] array, int offset)
            {
                if (array == null) return;
                for (int i = 0; i < array.Length; i++)
                {
                    int globalKey = offset + i;
                    if (_tempSavedItems.TryGetValue(globalKey, out Item original))
                    {
                        array[i] = original;
                    }
                }
            }

            Restore(player.inventory, 0);
            Restore(player.armor, 1000);
            Restore(player.dye, 2000);
            Restore(player.miscEquips, 3000);
            Restore(player.miscDyes, 4000);
            Restore(player.bank?.item, 5000);
            Restore(player.bank2?.item, 6000);
            Restore(player.bank3?.item, 7000);
            Restore(player.bank4?.item, 8000);

            _tempSavedItems.Clear();
        }

        public static void OnLoadPlayerPostfix(Player player, string playerPath)
        {
            if (player == null || string.IsNullOrEmpty(playerPath)) return;
            string sidecarPath = GetSidecarFilePath(playerPath);
            if (!File.Exists(sidecarPath)) return;

            try
            {
                string json = File.ReadAllText(sidecarPath);
                var records = JsonConvert.DeserializeObject<List<SavedItemRecord>>(json);
                if (records == null || records.Count == 0) return;

                int restoredCount = 0;
                foreach (var rec in records)
                {
                    int itemType = ItemLoader.GetItemType($"{rec.ModName}/{rec.ItemName}");
                    if (itemType == 0) itemType = ItemLoader.GetItemType(rec.ItemName);

                    if (itemType > 0)
                    {
                        Item item = new Item();
                        item.SetDefaults(itemType);
                        item.stack = rec.Stack > 0 ? rec.Stack : 1;
                        item.prefix = (byte)rec.Prefix;
                        item.favorited = rec.Favorited == 1;

                        Item[] targetArray = player.inventory;
                        switch (rec.Container)
                        {
                            case "inventory":
                                targetArray = player.inventory;
                                break;
                            case "armor":
                                targetArray = player.armor;
                                break;
                            case "dye":
                                targetArray = player.dye;
                                break;
                            case "miscEquips":
                                targetArray = player.miscEquips;
                                break;
                            case "miscDyes":
                                targetArray = player.miscDyes;
                                break;
                            case "bank":
                                targetArray = player.bank?.item;
                                break;
                            case "bank2":
                                targetArray = player.bank2?.item;
                                break;
                            case "bank3":
                                targetArray = player.bank3?.item;
                                break;
                            case "bank4":
                                targetArray = player.bank4?.item;
                                break;
                        }

                        if (targetArray != null && rec.SlotIndex >= 0 && rec.SlotIndex < targetArray.Length)
                        {
                            targetArray[rec.SlotIndex] = item;
                            restoredCount++;
                        }
                    }
                }
                ModLoader.Log($"[SidecarSave] 成功从伴生存档还原 {restoredCount} 个自定义物品: {Path.GetFileName(sidecarPath)}");
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[SidecarSave] 读取伴生存档异常: {ex.Message}");
            }
        }
    }
}
