using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using TPML.Content;
using TPML.Content.IO;

namespace TPMLBridge.GABP.Tools
{
    /// <summary>
    /// TPML 全域 Sidecar 模组物品持久化诊断与自动化测试工具
    /// 作者: SaintCirno9
    /// </summary>
    public static class SidecarTools
    {
        public static List<GABPToolDescriptor> GetDescriptors()
        {
            return new List<GABPToolDescriptor>
            {
                new GABPToolDescriptor
                {
                    Name = "tpml/test_sidecar_status",
                    Description = "获取玩家与世界伴随存档文件状态、磁盘数据与当前内存各容器模组物品分布。",
                    Tags = new List<string> { "diagnostic", "read-only", "sidecar" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/set_bank_item",
                    Description = "向玩家四大银行（1:存钱罐, 2:保险箱, 3:护卫熔炉, 4:虚空仓库）的指定槽位放置物品。",
                    Tags = new List<string> { "write", "bank", "sidecar" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "bank", "slot", "itemId" },
                        properties = new
                        {
                            bank = new { type = "integer", description = "银行编号 (1: 存钱罐, 2: 保险箱, 3: 护卫熔炉, 4: 虚空仓库)" },
                            slot = new { type = "integer", description = "槽位索引 (0~39)" },
                            itemId = new { type = "integer", description = "物品 ID" },
                            stack = new { type = "integer", description = "堆叠数量（默认 1）" },
                            prefix = new { type = "integer", description = "物品前缀 ID（默认 0）" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/get_bank_item",
                    Description = "获取玩家四大银行指定槽位的物品详细数据。",
                    Tags = new List<string> { "read-only", "bank", "sidecar" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "bank", "slot" },
                        properties = new
                        {
                            bank = new { type = "integer", description = "银行编号 (1~4)" },
                            slot = new { type = "integer", description = "槽位索引 (0~39)" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/set_chest_item",
                    Description = "向世界物理宝箱（Main.chest 0~7999）指定槽位放置物品。",
                    Tags = new List<string> { "write", "chest", "sidecar" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "chestIndex", "slot", "itemId" },
                        properties = new
                        {
                            chestIndex = new { type = "integer", description = "世界宝箱索引 (0~7999)" },
                            slot = new { type = "integer", description = "槽位索引 (0~39)" },
                            itemId = new { type = "integer", description = "物品 ID" },
                            stack = new { type = "integer", description = "堆叠数量（默认 1）" },
                            prefix = new { type = "integer", description = "物品前缀 ID（默认 0）" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/get_chest_item",
                    Description = "获取世界物理宝箱指定槽位的物品详细数据。",
                    Tags = new List<string> { "read-only", "chest", "sidecar" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "chestIndex", "slot" },
                        properties = new
                        {
                            chestIndex = new { type = "integer", description = "世界宝箱索引 (0~7999)" },
                            slot = new { type = "integer", description = "槽位索引 (0~39)" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/trigger_sidecar_save_load",
                    Description = "手动触发执行玩家/世界存档落盘保存或从伴随文件回填读档。",
                    Tags = new List<string> { "lifecycle", "sidecar" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "action" },
                        properties = new
                        {
                            action = new { type = "string", description = "'save_player' / 'save_world' / 'load_player' / 'load_world' / 'save_all' / 'load_all'" }
                        }
                    }
                }
            };
        }

        public static async Task<object> HandleAsync(string name, JObject args)
        {
            switch (name)
            {
                case "tpml/test_sidecar_status":
                case "tpml_test_sidecar_status":
                    return await MainThreadQueue.EnqueueAsync(() => GetSidecarStatus());

                case "tpml/set_bank_item":
                case "tpml_set_bank_item":
                    {
                        int bank = args?["bank"]?.Value<int>() ?? 1;
                        int slot = args?["slot"]?.Value<int>() ?? 0;
                        int itemId = args?["itemId"]?.Value<int>() ?? 0;
                        int stack = Math.Max(1, args?["stack"]?.Value<int>() ?? 1);
                        int prefix = args?["prefix"]?.Value<int>() ?? 0;
                        return await MainThreadQueue.EnqueueAsync(() => SetBankItem(bank, slot, itemId, stack, prefix));
                    }

                case "tpml/get_bank_item":
                case "tpml_get_bank_item":
                    {
                        int bank = args?["bank"]?.Value<int>() ?? 1;
                        int slot = args?["slot"]?.Value<int>() ?? 0;
                        return await MainThreadQueue.EnqueueAsync(() => GetBankItem(bank, slot));
                    }

                case "tpml/set_chest_item":
                case "tpml_set_chest_item":
                    {
                        int chestIndex = args?["chestIndex"]?.Value<int>() ?? 0;
                        int slot = args?["slot"]?.Value<int>() ?? 0;
                        int itemId = args?["itemId"]?.Value<int>() ?? 0;
                        int stack = Math.Max(1, args?["stack"]?.Value<int>() ?? 1);
                        int prefix = args?["prefix"]?.Value<int>() ?? 0;
                        return await MainThreadQueue.EnqueueAsync(() => SetChestItem(chestIndex, slot, itemId, stack, prefix));
                    }

                case "tpml/get_chest_item":
                case "tpml_get_chest_item":
                    {
                        int chestIndex = args?["chestIndex"]?.Value<int>() ?? 0;
                        int slot = args?["slot"]?.Value<int>() ?? 0;
                        return await MainThreadQueue.EnqueueAsync(() => GetChestItem(chestIndex, slot));
                    }

                case "tpml/trigger_sidecar_save_load":
                case "tpml_trigger_sidecar_save_load":
                    {
                        string action = args?["action"]?.ToString();
                        return await MainThreadQueue.EnqueueAsync(() => TriggerSidecarSaveLoad(action));
                    }

                default:
                    return null;
            }
        }

        public static object GetSidecarStatus()
        {
            Player player = Main.LocalPlayer;
            string playerPath = player != null ? ModItemSidecarEngine.GetPlayerSidecarPath(player) : null;
            bool playerFileExists = playerPath != null && File.Exists(playerPath);
            PlayerSidecarData playerData = null;
            if (playerFileExists)
            {
                try
                {
                    playerData = JsonConvert.DeserializeObject<PlayerSidecarData>(File.ReadAllText(playerPath));
                }
                catch { }
            }

            string worldPath = ModItemSidecarEngine.GetWorldSidecarPath();
            bool worldFileExists = worldPath != null && File.Exists(worldPath);
            WorldSidecarData worldData = null;
            if (worldFileExists)
            {
                try
                {
                    worldData = JsonConvert.DeserializeObject<WorldSidecarData>(File.ReadAllText(worldPath));
                }
                catch { }
            }

            List<object> ExtractModItems(Item[] items, string prefix)
            {
                var list = new List<object>();
                if (items == null) return list;
                for (int i = 0; i < items.Length; i++)
                {
                    Item it = items[i];
                    if (it != null && !it.IsAir && it.type >= ItemID.Count)
                    {
                        list.Add(new
                        {
                            location = $"{prefix}_{i}",
                            slot = i,
                            id = it.type,
                            name = it.Name,
                            stack = it.stack,
                            prefix = it.prefix,
                            favorited = it.favorited
                        });
                    }
                }
                return list;
            }

            return new
            {
                playerSidecar = new
                {
                    path = playerPath,
                    exists = playerFileExists,
                    entryCount = playerData?.Items?.Count ?? 0,
                    entries = playerData?.Items
                },
                worldSidecar = new
                {
                    path = worldPath,
                    exists = worldFileExists,
                    chestEntryCount = worldData?.ChestItems?.Count ?? 0,
                    tileEntityEntryCount = worldData?.TileEntityItems?.Count ?? 0,
                    chestEntries = worldData?.ChestItems,
                    tileEntityEntries = worldData?.TileEntityItems
                },
                memory = new
                {
                    inventory = ExtractModItems(player?.inventory, "inv"),
                    armor = ExtractModItems(player?.armor, "armor"),
                    dye = ExtractModItems(player?.dye, "dye"),
                    bank1 = ExtractModItems(player?.bank?.item, "bank1"),
                    bank2 = ExtractModItems(player?.bank2?.item, "bank2"),
                    bank3 = ExtractModItems(player?.bank3?.item, "bank3"),
                    bank4 = ExtractModItems(player?.bank4?.item, "bank4")
                }
            };
        }

        private static Chest GetPlayerBank(int bankNumber)
        {
            Player player = Main.LocalPlayer;
            if (player == null) return null;
            switch (bankNumber)
            {
                case 1: return player.bank;
                case 2: return player.bank2;
                case 3: return player.bank3;
                case 4: return player.bank4;
                default: return null;
            }
        }

        public static object SetBankItem(int bankNumber, int slot, int itemId, int stack, int prefix)
        {
            Chest bank = GetPlayerBank(bankNumber);
            if (bank?.item == null || slot < 0 || slot >= bank.item.Length)
            {
                return new { success = false, message = $"银行编号 {bankNumber} 或槽位 {slot} 无效" };
            }

            if (itemId <= 0)
            {
                bank.item[slot] = new Item();
                return new { success = true, bank = bankNumber, slot, itemId = 0, message = "槽位已清空" };
            }

            Item item = new Item();
            item.SetDefaults(itemId);
            item.stack = Math.Max(1, Math.Min(stack, item.maxStack));
            if (prefix > 0) item.Prefix(prefix);

            bank.item[slot] = item;
            return new
            {
                success = true,
                bank = bankNumber,
                slot,
                itemId = item.type,
                name = item.Name,
                stack = item.stack,
                prefix = item.prefix,
                message = $"已向银行 {bankNumber} 槽位 {slot} 放入物品 [{item.Name}] x{item.stack}"
            };
        }

        public static object GetBankItem(int bankNumber, int slot)
        {
            Chest bank = GetPlayerBank(bankNumber);
            if (bank?.item == null || slot < 0 || slot >= bank.item.Length)
            {
                return new { success = false, message = $"银行编号 {bankNumber} 或槽位 {slot} 无效" };
            }

            Item item = bank.item[slot];
            bool isAir = item == null || item.IsAir || item.type <= 0;
            return new
            {
                success = true,
                bank = bankNumber,
                slot,
                isAir,
                itemId = isAir ? 0 : item.type,
                name = isAir ? string.Empty : item.Name,
                stack = isAir ? 0 : item.stack,
                prefix = isAir ? 0 : item.prefix,
                favorited = !isAir && item.favorited
            };
        }

        public static object SetChestItem(int chestIndex, int slot, int itemId, int stack, int prefix)
        {
            if (Main.chest == null) return new { success = false, message = "世界中未初始化宝箱系统" };

            // 若宝箱不存在，自动创建一个测试箱子
            if (chestIndex >= 0 && chestIndex < Main.chest.Length && Main.chest[chestIndex] == null)
            {
                int cx = (int)(Main.LocalPlayer?.Center.X / 16f ?? 100);
                int cy = (int)(Main.LocalPlayer?.Center.Y / 16f ?? 100);
                Chest.CreateWorldChest(chestIndex, cx, cy);
            }

            if (chestIndex < 0 || chestIndex >= Main.chest.Length || Main.chest[chestIndex]?.item == null || slot < 0 || slot >= Main.chest[chestIndex].item.Length)
            {
                return new { success = false, message = $"宝箱索引 {chestIndex} 或槽位 {slot} 无效" };
            }

            Chest chest = Main.chest[chestIndex];
            if (itemId <= 0)
            {
                chest.item[slot] = new Item();
                return new { success = true, chestIndex, slot, itemId = 0, message = "宝箱槽位已清空" };
            }

            Item item = new Item();
            item.SetDefaults(itemId);
            item.stack = Math.Max(1, Math.Min(stack, item.maxStack));
            if (prefix > 0) item.Prefix(prefix);

            chest.item[slot] = item;
            return new
            {
                success = true,
                chestIndex,
                slot,
                itemId = item.type,
                name = item.Name,
                stack = item.stack,
                prefix = item.prefix,
                message = $"已向世界宝箱 {chestIndex} 槽位 {slot} 放入物品 [{item.Name}] x{item.stack}"
            };
        }

        public static object GetChestItem(int chestIndex, int slot)
        {
            if (Main.chest == null || chestIndex < 0 || chestIndex >= Main.chest.Length || Main.chest[chestIndex]?.item == null || slot < 0 || slot >= Main.chest[chestIndex].item.Length)
            {
                return new { success = false, message = $"宝箱索引 {chestIndex} 或槽位 {slot} 无效" };
            }

            Item item = Main.chest[chestIndex].item[slot];
            bool isAir = item == null || item.IsAir || item.type <= 0;
            return new
            {
                success = true,
                chestIndex,
                slot,
                isAir,
                itemId = isAir ? 0 : item.type,
                name = isAir ? string.Empty : item.Name,
                stack = isAir ? 0 : item.stack,
                prefix = isAir ? 0 : item.prefix
            };
        }

        public static object TriggerSidecarSaveLoad(string action)
        {
            switch (action?.ToLowerInvariant())
            {
                case "save_player":
                    if (Main.LocalPlayer != null && Main.ActivePlayerFileData != null)
                    {
                        Player.SavePlayer(Main.ActivePlayerFileData, false);
                    }
                    else if (Main.LocalPlayer != null)
                    {
                        ModItemSidecarEngine.OnPlayerSavePrefix(Main.LocalPlayer);
                        ModItemSidecarEngine.OnPlayerSavePostfix(Main.LocalPlayer);
                    }
                    return new { success = true, action, message = "已执行玩家数据与 Sidecar 保存" };

                case "save_world":
                    if (!Main.gameMenu)
                    {
                        ModItemSidecarEngine.OnWorldSavePrefix();
                        ModItemSidecarEngine.OnWorldSavePostfix();
                    }
                    return new { success = true, action, message = "已执行世界数据与 Sidecar 保存" };

                case "load_player":
                    if (Main.LocalPlayer != null)
                    {
                        ModItemSidecarEngine.OnPlayerLoaded(Main.LocalPlayer);
                    }
                    return new { success = true, action, message = "已从 Sidecar 文件回填玩家数据" };

                case "load_world":
                    ModItemSidecarEngine.OnWorldLoaded();
                    return new { success = true, action, message = "已从 Sidecar 文件回填世界数据" };

                case "save_all":
                    if (Main.LocalPlayer != null)
                    {
                        ModItemSidecarEngine.OnPlayerSavePrefix(Main.LocalPlayer);
                        ModItemSidecarEngine.OnPlayerSavePostfix(Main.LocalPlayer);
                    }
                    ModItemSidecarEngine.OnWorldSavePrefix();
                    ModItemSidecarEngine.OnWorldSavePostfix();
                    return new { success = true, action, message = "已全量保存玩家与世界 Sidecar" };

                case "load_all":
                    if (Main.LocalPlayer != null)
                    {
                        ModItemSidecarEngine.OnPlayerLoaded(Main.LocalPlayer);
                    }
                    ModItemSidecarEngine.OnWorldLoaded();
                    return new { success = true, action, message = "已全量加载玩家与世界 Sidecar" };

                default:
                    return new { success = false, message = $"未知 action: {action}" };
            }
        }
    }
}
