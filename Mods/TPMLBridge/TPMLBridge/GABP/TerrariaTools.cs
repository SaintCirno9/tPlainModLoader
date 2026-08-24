using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;

namespace TPMLBridge.GABP
{
    public static class TerrariaTools
    {
        public static int PendingHoldUseFrames = 0;
        public static bool PendingHoldAlt = false;

        public static List<GABPToolDescriptor> GetDescriptors()
        {
            return new List<GABPToolDescriptor>
            {
                new GABPToolDescriptor
                {
                    Name = "tpml/get_game_state",
                    Description = "获取当前泰拉瑞亚游戏状态（菜单/世界中、玩家信息、世界信息、背包打开状态）。",
                    Tags = new List<string> { "diagnostic", "read-only", "observation" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/test_instavator",
                    Description = "诊断 Instavator 原生内容注册、贴图、配方、向导材料和物品搜索状态。",
                    Tags = new List<string> { "diagnostic", "read-only", "instavator" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/list_players",
                    Description = "获取本地与云端已保存的所有玩家角色列表。",
                    Tags = new List<string> { "read-only" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/list_worlds",
                    Description = "获取本地与云端已保存的所有世界存档列表。",
                    Tags = new List<string> { "read-only" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/load_world",
                    Description = "一键跳过主菜单动画与选人界面，直接加载指定角色与世界并进入单人游戏。",
                    Tags = new List<string> { "lifecycle" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            playerName = new { type = "string", description = "角色名称（留空则选择最近使用或第一个角色）" },
                            worldName = new { type = "string", description = "世界名称（留空则选择最近使用或第一个世界）" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/leave_world",
                    Description = "安全退出当前世界至主菜单（默认启用存档保护，严格禁止写盘保存以保护玩家正在游玩的真实世界）。",
                    Tags = new List<string> { "lifecycle" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/get_inventory",
                    Description = "获取当前玩家背包物品列表（58格物品槽位包含ID、名称、数量、前缀与收藏状态）。",
                    Tags = new List<string> { "read-only" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/toggle_inventory",
                    Description = "打开或关闭玩家物品栏背包界面。",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            open = new { type = "boolean", description = "true为打开，false为关闭；不传则切换开/关状态" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/run_command",
                    Description = "向游戏发送聊天消息或执行命令（以 / 开头的控制台指令）。",
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "command" },
                        properties = new
                        {
                            command = new { type = "string", description = "消息内容或 / 指令" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/teleport",
                    Description = "将当前本地玩家传送到指定的图格坐标 (tileX, tileY) 或世界像素坐标。",
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "tileX", "tileY" },
                        properties = new
                        {
                            tileX = new { type = "number", description = "目标图格 X 坐标 (0 ~ maxTilesX)" },
                            tileY = new { type = "number", description = "目标图格 Y 坐标 (0 ~ maxTilesY)" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/heal_player",
                    Description = "为当前玩家恢复全部生命值与法力值，并清除所有负面 Debuff。",
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/give_item",
                    Description = "将指定物品发放到当前玩家背包。",
                    Tags = new List<string> { "write", "inventory" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "itemId" },
                        properties = new
                        {
                            itemId = new { type = "integer" },
                            stack = new { type = "integer" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/spawn_item_in_world",
                    Description = "在当前玩家位置生成指定物品实体。",
                    Tags = new List<string> { "write", "world" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "itemId" },
                        properties = new
                        {
                            itemId = new { type = "integer" },
                            stack = new { type = "integer" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/select_inventory_slot",
                    Description = "将当前玩家的指定背包槽位设为手持物品。",
                    Tags = new List<string> { "write", "inventory" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "slot" },
                        properties = new
                        {
                            slot = new { type = "integer" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/get_item_info",
                    Description = "读取物品栏指定槽位（0~57）物品的详细属性与 Tooltip 提示行（包含伤害、暴击、击退、攻速、稀有度、价格、前缀等）。",
                    Tags = new List<string> { "read-only", "inventory" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "slot" },
                        properties = new
                        {
                            slot = new { type = "integer", description = "物品栏槽位索引 (0~57，0~9 为快捷栏，10~49 为主背包，50~53 为钱币槽，54~57 为弹药槽)" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/use_held_item",
                    Description = "使用当前玩家手持的物品（支持指定目标图格/世界坐标、朝向、左右键副功能以及持续按住帧数）。",
                    Tags = new List<string> { "write", "player", "action" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            tileX = new { type = "integer", description = "目标图格 X 坐标（可选）" },
                            tileY = new { type = "integer", description = "目标图格 Y 坐标（可选）" },
                            worldX = new { type = "number", description = "目标世界像素 X 坐标（可选）" },
                            worldY = new { type = "number", description = "目标世界像素 Y 坐标（可选）" },
                            direction = new { type = "integer", description = "玩家朝向 (-1 为左, 1 为右，可选)" },
                            altUse = new { type = "boolean", description = "是否以副功能/右键方式使用物品（可选，默认 false）" },
                            holdFrames = new { type = "integer", description = "持续按住使用的帧数，默认 1（针对蓄力或引导类武器）" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/swap_inventory_slots",
                    Description = "交换玩家背包中指定的两个槽位物品（支持将主背包物品安全移入快捷栏）。",
                    Tags = new List<string> { "write", "inventory" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "fromSlot", "toSlot" },
                        properties = new
                        {
                            fromSlot = new { type = "integer", description = "源槽位索引 (0~57)" },
                            toSlot = new { type = "integer", description = "目标槽位索引 (0~57)" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/get_last_build_result",
                    Description = "获取最近一次直通车建造任务的执行结果报告快照（包含开凿类型、范围、耗时、清理方块数、放置绳索/火把/护壁统计）。",
                    Tags = new List<string> { "read-only", "instavator", "diagnostic" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/inspect_shaft",
                    Description = "扫描矿道垂直切片图格质量，量化分析直通率、绳索连续性、火把密度、液体残留与黑曜石护壁完整度。",
                    Tags = new List<string> { "read-only", "world", "diagnostic" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            centerX = new { type = "integer", description = "矿道中心图格 X 坐标（可选，默认玩家当前所在 X）" },
                            startY = new { type = "integer", description = "开凿起始图格 Y 深度（可选，默认玩家脚下）" },
                            targetY = new { type = "integer", description = "开凿目标图格 Y 深度（可选，默认地狱底层 maxTilesY - 40）" },
                            variant = new { type = "string", description = "矿道规格（可选，'Full' / 'Half' / 'DoubleObsidian' / 'Auto'，默认 'Auto'）" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/test_creative_inventory",
                    Description = "诊断创造模式物品浏览器 UI 状态（是否打开、搜索关键词、当前匹配物品数、输入框 Focus 状态与尺寸）。",
                    Tags = new List<string> { "read-only", "ui", "diagnostic" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/toggle_creative_inventory",
                    Description = "打开或关闭创造模式物品浏览器 UI 窗口。",
                    Tags = new List<string> { "write", "ui" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            open = new { type = "boolean", description = "true 为打开，false 为关闭；不传则切换开/关状态" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/set_creative_search",
                    Description = "向创造模式物品浏览器输入搜索关键词，并立即执行过滤返回匹配结果摘要。",
                    Tags = new List<string> { "write", "ui" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "query" },
                        properties = new
                        {
                            query = new { type = "string", description = "要搜索的物品名称（中文/英文）或纯数字 ItemID" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/focus_creative_search",
                    Description = "切换或设置创造模式搜索框的键盘输入焦点 (Focus)。",
                    Tags = new List<string> { "write", "ui" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            focus = new { type = "boolean", description = "是否获得输入焦点（默认 true）" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/simulate_scroll_wheel",
                    Description = "模拟鼠标滚轮滚动（设置 ScrollWheelDelta / ScrollWheelDeltaForUI），测试快捷栏切换与 UI 滚动条。",
                    Tags = new List<string> { "write", "input" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "delta" },
                        properties = new
                        {
                            delta = new { type = "integer", description = "滚轮滚动量（如 120 为向上滚 1 格，-120 为向下滚 1 格）" },
                            forUI = new { type = "boolean", description = "是否同时提供给 UI 状态机（默认 true）" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/set_save_protection",
                    Description = "查看或设置自动化测试世界存档只读保护状态（默认开启，严格禁止保存以保护玩家真实存档）。",
                    Tags = new List<string> { "lifecycle", "safety" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            enabled = new { type = "boolean", description = "true 为开启保护（拦截所有保存），false 为关闭保护" }
                        }
                    }
                }
            };
        }

        public static async Task<object> CallToolAsync(string name, JObject args)
        {
            switch (name)
            {
                case "tpml/get_game_state":
                case "tpml_get_game_state":
                    return await MainThreadQueue.EnqueueAsync(() => GetGameState());

                case "tpml/test_instavator":
                case "tpml_test_instavator":
                    return await MainThreadQueue.EnqueueAsync(() => TestInstavator());

                case "tpml/list_players":
                case "tpml_list_players":
                    return await MainThreadQueue.EnqueueAsync(() => ListPlayers());

                case "tpml/list_worlds":
                case "tpml_list_worlds":
                    return await MainThreadQueue.EnqueueAsync(() => ListWorlds());

                case "tpml/load_world":
                case "tpml_load_world":
                    {
                        string pName = args?["playerName"]?.ToString();
                        string wName = args?["worldName"]?.ToString();
                        bool protectSave = args?["protectSave"]?.Value<bool?>() ?? true;
                        return await MainThreadQueue.EnqueueAsync(() => LoadWorld(pName, wName, protectSave));
                    }

                case "tpml/leave_world":
                case "tpml_leave_world":
                    {
                        bool save = args?["save"]?.Value<bool?>() ?? false;
                        return await MainThreadQueue.EnqueueAsync(() => LeaveWorld(save));
                    }

                case "tpml/set_save_protection":
                case "tpml_set_save_protection":
                    {
                        bool? enabled = args?["enabled"]?.Value<bool?>();
                        return await MainThreadQueue.EnqueueAsync(() => SetSaveProtection(enabled));
                    }

                case "tpml/get_inventory":
                case "tpml_get_inventory":
                    return await MainThreadQueue.EnqueueAsync(() => GetInventory());

                case "tpml/toggle_inventory":
                case "tpml_toggle_inventory":
                    {
                        bool? open = args?["open"]?.Value<bool>();
                        return await MainThreadQueue.EnqueueAsync(() => ToggleInventory(open));
                    }

                case "tpml/run_command":
                case "tpml_run_command":
                    {
                        string cmd = args?["command"]?.ToString();
                        return await MainThreadQueue.EnqueueAsync(() => RunCommand(cmd));
                    }

                case "tpml/teleport":
                case "tpml_teleport":
                    {
                        int tx = args?["tileX"]?.Value<int>() ?? 0;
                        int ty = args?["tileY"]?.Value<int>() ?? 0;
                        return await MainThreadQueue.EnqueueAsync(() => TeleportPlayer(tx, ty));
                    }

                case "tpml/heal_player":
                case "tpml_heal_player":
                    return await MainThreadQueue.EnqueueAsync(() => HealPlayer());

                case "tpml/give_item":
                case "tpml_give_item":
                    {
                        int itemId = args?["itemId"]?.Value<int>() ?? 0;
                        int stack = Math.Max(1, args?["stack"]?.Value<int>() ?? 1);
                        return await MainThreadQueue.EnqueueAsync(() => GiveItem(itemId, stack));
                    }

                case "tpml/spawn_item_in_world":
                case "tpml_spawn_item_in_world":
                    {
                        int itemId = args?["itemId"]?.Value<int>() ?? 0;
                        int stack = Math.Max(1, args?["stack"]?.Value<int>() ?? 1);
                        return await MainThreadQueue.EnqueueAsync(() => SpawnItemInWorld(itemId, stack));
                    }

                case "tpml/select_inventory_slot":
                case "tpml_select_inventory_slot":
                    {
                        int slot = args?["slot"]?.Value<int>() ?? -1;
                        return await MainThreadQueue.EnqueueAsync(() => SelectInventorySlot(slot));
                    }

                case "tpml/get_item_info":
                case "tpml_get_item_info":
                case "tpml/get_slot_info":
                case "tpml_get_slot_info":
                case "tpml/get_item_detail":
                case "tpml_get_item_detail":
                    {
                        int slot = args?["slot"]?.Value<int>() ?? -1;
                        return await MainThreadQueue.EnqueueAsync(() => GetInventorySlotInfo(slot));
                    }

                case "tpml/use_held_item":
                case "tpml_use_held_item":
                case "tpml/use_item":
                case "tpml_use_item":
                    {
                        int? tileX = args?["tileX"]?.Value<int?>();
                        int? tileY = args?["tileY"]?.Value<int?>();
                        float? worldX = args?["worldX"]?.Value<float?>();
                        float? worldY = args?["worldY"]?.Value<float?>();
                        int? direction = args?["direction"]?.Value<int?>();
                        bool? altUse = args?["altUse"]?.Value<bool?>();
                        int? holdFrames = args?["holdFrames"]?.Value<int?>();
                        return await MainThreadQueue.EnqueueAsync(() => UseHeldItem(tileX, tileY, worldX, worldY, direction, altUse, holdFrames));
                    }

                case "tpml/swap_inventory_slots":
                case "tpml_swap_inventory_slots":
                case "tpml/swap_slots":
                    {
                        int fromSlot = args?["fromSlot"]?.Value<int>() ?? -1;
                        int toSlot = args?["toSlot"]?.Value<int>() ?? -1;
                        return await MainThreadQueue.EnqueueAsync(() => SwapInventorySlots(fromSlot, toSlot));
                    }

                case "tpml/get_last_build_result":
                case "tpml_get_last_build_result":
                case "tpml/last_build_result":
                    return await MainThreadQueue.EnqueueAsync(() => GetLastBuildResult());

                case "tpml/inspect_shaft":
                case "tpml_inspect_shaft":
                case "tpml/inspect_mine":
                    {
                        int? centerX = args?["centerX"]?.Value<int?>();
                        int? startY = args?["startY"]?.Value<int?>();
                        int? targetY = args?["targetY"]?.Value<int?>();
                        string variant = args?["variant"]?.ToString();
                        return await MainThreadQueue.EnqueueAsync(() => InspectShaft(centerX, startY, targetY, variant));
                    }

                case "tpml/test_creative_inventory":
                case "tpml_test_creative_inventory":
                    return await MainThreadQueue.EnqueueAsync(() => TestCreativeInventory());

                case "tpml/toggle_creative_inventory":
                case "tpml_toggle_creative_inventory":
                    {
                        bool? open = args?["open"]?.Value<bool?>();
                        return await MainThreadQueue.EnqueueAsync(() => ToggleCreativeInventory(open));
                    }

                case "tpml/set_creative_search":
                case "tpml_set_creative_search":
                    {
                        string query = args?["query"]?.ToString();
                        return await MainThreadQueue.EnqueueAsync(() => SetCreativeSearch(query));
                    }

                case "tpml/focus_creative_search":
                case "tpml_focus_creative_search":
                    {
                        bool focus = args?["focus"]?.Value<bool?>() ?? true;
                        return await MainThreadQueue.EnqueueAsync(() => FocusCreativeSearch(focus));
                    }

                case "tpml/simulate_scroll_wheel":
                case "tpml_simulate_scroll_wheel":
                    {
                        int delta = args?["delta"]?.Value<int>() ?? 0;
                        bool forUI = args?["forUI"]?.Value<bool?>() ?? true;
                        return await MainThreadQueue.EnqueueAsync(() => SimulateScrollWheel(delta, forUI));
                    }

                default:
                    throw new KeyNotFoundException($"未知的工具名称: {name}");
            }
        }

        private static object GetGameState()
        {
            bool inWorld = !Main.gameMenu;
            var state = new Dictionary<string, object>
            {
                ["gameMenu"] = Main.gameMenu,
                ["inWorld"] = inWorld,
                ["menuMode"] = Main.menuMode,
                ["playerInventory"] = Main.playerInventory,
                ["version"] = Main.versionNumber
            };

            if (inWorld && Main.LocalPlayer != null)
            {
                var p = Main.LocalPlayer;
                state["player"] = new
                {
                    name = p.name,
                    life = p.statLife,
                    maxLife = p.statLifeMax2,
                    mana = p.statMana,
                    maxMana = p.statManaMax2,
                    position = new { x = p.position.X, y = p.position.Y },
                    tilePosition = new { x = (int)(p.position.X / 16f), y = (int)(p.position.Y / 16f) },
                    selectedItem = p.selectedItem,
                    hasBufferedSelection = p.selectedItemState.HasBufferedChange,
                    lastNonOverridenSelection = p.selectedItemState.LastNonOverridenSelection,
                    itemAnimation = p.itemAnimation,
                    itemTime = p.itemTime,
                    reuseDelay = p.reuseDelay,
                    channel = p.channel,
                    usingOrReusingItem = p.UsingOrReusingItem,
                    heldItemId = p.HeldItem?.type ?? 0,
                    lastVisualizedItemId = p.HeldItem?.type ?? 0,
                    zoneDungeon = p.ZoneDungeon,
                    zoneUnderworldHeight = p.ZoneUnderworldHeight
                };

                state["world"] = new
                {
                    name = Main.worldName,
                    id = Main.worldID,
                    time = Main.time,
                    dayTime = Main.dayTime,
                    hardMode = Main.hardMode,
                    expertMode = Main.expertMode,
                    masterMode = Main.masterMode,
                    width = Main.maxTilesX,
                    height = Main.maxTilesY
                };
            }

            return state;
        }

        private static object ListPlayers()
        {
            Main.LoadPlayers();
            var players = Main.PlayerList.Select(p => new
            {
                name = p.Name,
                path = p.Path,
                isCloudSave = p.IsCloudSave,
                isFavorite = p.IsFavorite,
                difficulty = p.Player?.difficulty ?? 0,
                playTime = p.GetPlayTime().ToString()
            }).ToList();

            return new { count = players.Count, players };
        }

        private static object ListWorlds()
        {
            Main.LoadWorlds();
            var worlds = Main.WorldList.Select(w => new
            {
                name = w.Name,
                path = w.Path,
                isCloudSave = w.IsCloudSave,
                isFavorite = w.IsFavorite,
                gameMode = w.GameMode,
                width = w.WorldSizeX,
                height = w.WorldSizeY
            }).ToList();

            return new { count = worlds.Count, worlds };
        }

        private static object LoadWorld(string playerName, string worldName, bool protectSave = true)
        {
            if (protectSave)
            {
                TPMLBridgeMod.WorldSaveProtectionEnabled = true;
            }

            Main.LoadPlayers();
            PlayerFileData player = null;
            if (!string.IsNullOrEmpty(playerName))
            {
                player = Main.PlayerList.FirstOrDefault(p => p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
            }
            if (player == null)
            {
                player = Main.PlayerList.OrderByDescending(p => p.LastPlayed).FirstOrDefault();
            }

            if (player == null)
            {
                return new { success = false, message = "未找到可用的玩家角色存档，请先创建一个角色。" };
            }

            Main.SelectPlayer(player);

            Main.LoadWorlds();
            WorldFileData world = null;
            if (!string.IsNullOrEmpty(worldName))
            {
                world = Main.WorldList.FirstOrDefault(w => w.Name.Equals(worldName, StringComparison.OrdinalIgnoreCase));
            }
            if (world == null)
            {
                world = Main.WorldList.OrderByDescending(w => w.LastPlayed).FirstOrDefault();
            }

            if (world == null)
            {
                return new { success = false, message = "未找到可用的世界存档，请先创建一个世界。" };
            }

            Main.ActiveWorldFileData = world;
            Main.menuMode = 10;
            WorldGen.playWorld();

            return new
            {
                success = true,
                message = $"正在进入世界 [{world.Name}]，角色: [{player.Name}]",
                player = player.Name,
                world = world.Name
            };
        }

        private static object LeaveWorld(bool save = false)
        {
            if (Main.gameMenu)
            {
                TPMLBridgeMod.WorldSaveProtectionEnabled = false;
                return new { success = true, inWorld = false, message = "当前已在主菜单中" };
            }

            if (save && !TPMLBridgeMod.WorldSaveProtectionEnabled)
            {
                WorldFile.SaveWorld();
            }

            // 彻底断开并返回主菜单，严禁将自动化测试产生的数据写盘污染玩家游玩存档
            Netplay.Disconnect = true;
            Main.netMode = 0;
            Main.menuMode = 0;
            Main.gameMenu = true;

            // 退出自动化测试后复位保护标志，确保玩家后续正常游玩时能够正常保存
            bool wasProtected = TPMLBridgeMod.WorldSaveProtectionEnabled;
            TPMLBridgeMod.WorldSaveProtectionEnabled = false;

            return new
            {
                success = true,
                inWorld = false,
                saved = false,
                worldSaveProtection = wasProtected,
                message = wasProtected
                    ? "已安全退出世界（自动化测试存档保护模式已生效，未保存世界文件，且已恢复正常游玩模式）"
                    : "已安全退出世界"
            };
        }

        private static object SetSaveProtection(bool? enabled)
        {
            if (enabled.HasValue)
            {
                TPMLBridgeMod.WorldSaveProtectionEnabled = enabled.Value;
            }

            return new
            {
                success = true,
                worldSaveProtectionEnabled = TPMLBridgeMod.WorldSaveProtectionEnabled,
                message = TPMLBridgeMod.WorldSaveProtectionEnabled
                    ? "自动化测试世界存档保护已开启（所有 WorldFile.SaveWorld 写盘调用均被强行拦截）"
                    : "自动化测试世界存档保护已关闭（允许正常写盘保存）"
            };
        }

        private static object GetInventory()
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
            {
                return new { inWorld = false, message = "当前未进入世界" };
            }

            var p = Main.LocalPlayer;
            var items = new List<object>();

            for (int i = 0; i < p.inventory.Length; i++)
            {
                var item = p.inventory[i];
                if (item != null && item.type > ItemID.None && item.stack > 0)
                {
                    items.Add(new
                    {
                        slot = i,
                        id = item.type,
                        name = item.Name,
                        stack = item.stack,
                        maxStack = item.maxStack,
                        prefix = item.prefix,
                        favorited = item.favorited
                    });
                }
            }

            return new
            {
                inWorld = true,
                playerName = p.name,
                totalSlots = p.inventory.Length,
                itemCount = items.Count,
                inventoryOpen = Main.playerInventory,
                items
            };
        }

        private static object ToggleInventory(bool? open)
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
            {
                return new { inWorld = false, message = "当前未进入世界，无法切换背包" };
            }

            if (open.HasValue)
            {
                Main.playerInventory = open.Value;
            }
            else
            {
                Main.playerInventory = !Main.playerInventory;
            }

            return new
            {
                success = true,
                inventoryOpen = Main.playerInventory,
                message = Main.playerInventory ? "背包已打开" : "背包已关闭"
            };
        }

        private static object RunCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return new { success = false, message = "指令内容为空" };
            }

            if (!Main.gameMenu && Main.LocalPlayer != null)
            {
                Main.NewText($"[TPMLBridge] {command}", 100, 255, 200);
            }

            Console.WriteLine($"[TPMLBridge Command] {command}");
            return new { success = true, command, message = "指令已发送" };
        }

        private static object TeleportPlayer(int tileX, int tileY)
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
            {
                return new { success = false, message = "当前未在世界中" };
            }

            var p = Main.LocalPlayer;
            p.Teleport(new Vector2(tileX * 16f, tileY * 16f), 1);
            return new
            {
                success = true,
                message = $"已传送玩家至图格 ({tileX}, {tileY})",
                tileX,
                tileY
            };
        }

        private static object HealPlayer()
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
            {
                return new { success = false, message = "当前未在世界中" };
            }

            var p = Main.LocalPlayer;
            p.statLife = p.statLifeMax2;
            p.statMana = p.statManaMax2;
            p.ClearBuff(BuffID.Bleeding);
            p.ClearBuff(BuffID.Poisoned);
            p.ClearBuff(BuffID.OnFire);

            return new
            {
                success = true,
                life = p.statLife,
                mana = p.statMana,
                message = "玩家已恢复满状态"
            };
        }

        private static object TestInstavator()
        {
            const int firstInstavatorId = 6200;
            const int lastInstavatorId = 6202;
            var items = new List<object>();
            var itemTypes = new HashSet<int>();

            foreach (ModItem modItem in ItemLoader.Items)
            {
                if (modItem == null || modItem.Type < firstInstavatorId || modItem.Type > lastInstavatorId)
                    continue;

                int type = modItem.Type;
                itemTypes.Add(type);
                ItemLoader.EnsureTextureLoaded(type);
                var asset = TextureAssets.Item != null && type < TextureAssets.Item.Length
                    ? TextureAssets.Item[type]
                    : null;
                var texture = asset?.Value;

                items.Add(new
                {
                    id = type,
                    actualName = GetItemDisplayName(type),
                    registered = ItemLoader.GetItem(type) != null,
                    textureValid = asset != null && asset.IsLoaded && texture != null,
                    textureWidth = texture?.Width ?? 0,
                    textureHeight = texture?.Height ?? 0
                });
            }

            var recipes = new List<object>();
            for (int i = 0; i < Recipe.numRecipes && i < Main.recipe.Length; i++)
            {
                Recipe recipe = Main.recipe[i];
                if (recipe?.createItem == null || recipe.createItem.type < firstInstavatorId || recipe.createItem.type > lastInstavatorId)
                    continue;

                var requirements = recipe.requiredItem
                    .Where(item => item != null && !item.IsAir && item.type > 0)
                    .Select(item => new
                    {
                        id = item.type,
                        name = GetItemDisplayName(item.type),
                        stack = item.stack
                    })
                    .ToList();
                string tileName = recipe.requiredTile >= 0
                    ? Lang.GetMapObjectName(recipe.requiredTile)
                    : string.Empty;

                recipes.Add(new
                {
                    recipeIndex = i,
                    outputItemId = recipe.createItem.type,
                    outputItemName = GetItemDisplayName(recipe.createItem.type),
                    requiredTileName = tileName,
                    requirements
                });
            }

            var matchedGuideTypes = new List<int>();
            foreach (Recipe recipe in Main.recipe.Take(Recipe.numRecipes))
            {
                if (recipe?.createItem == null || recipe.createItem.type < firstInstavatorId || recipe.createItem.type > lastInstavatorId)
                    continue;
                if (recipe.requiredItem.Any(item => item != null && item.type == ItemID.FallenStar))
                    matchedGuideTypes.Add(recipe.createItem.type);
            }

            var creativeMatches = ItemLoader.Items
                .Where(item => item != null && GetItemDisplayName(item.Type).Contains("直通车"))
                .Select(item => item.Type)
                .Distinct()
                .ToList();

            return new
            {
                items,
                recipes,
                guideStarSearch = new
                {
                    success = matchedGuideTypes.Count >= 3,
                    matchedInstavatorItemTypes = matchedGuideTypes.Distinct().ToList()
                },
                creativeSearch = new
                {
                    success = creativeMatches.Count >= 3,
                    matchedItemIds = creativeMatches
                }
            };
        }

        private static object GiveItem(int itemId, int stack)
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
                return new { success = false, message = "当前未进入世界" };

            if (itemId <= 0)
                return new { success = false, message = "物品 ID 无效" };

            Item item = new Item();
            if (ItemLoader.GetItem(itemId) != null)
            {
                item.type = itemId;
                ItemLoader.SetDefaults(item);
                item.stack = stack;
            }
            else
            {
                item.SetDefaults(itemId);
                item.stack = stack;
            }

            Item overflow = Main.LocalPlayer.GetItem(item, GetItemSettings.PickupItemFromWorld);
            int slot = FindInventorySlot(itemId);
            return new
            {
                success = slot >= 0 && (overflow == null || overflow.IsAir),
                slot,
                itemId,
                stack,
                message = slot >= 0 ? $"已发放物品 ID={itemId} x{stack}" : "物品未能进入背包",
                overflow = overflow?.stack ?? 0
            };
        }

        private static object SpawnItemInWorld(int itemId, int stack)
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
                return new { success = false, message = "当前未进入世界" };

            if (itemId <= 0)
                return new { success = false, message = "物品 ID 无效" };

            int itemIndex = Item.NewItem(null, Main.LocalPlayer.Center, itemId, stack);
            return new
            {
                success = itemIndex >= 0,
                itemIndex,
                itemId,
                stack,
                message = itemIndex >= 0 ? $"已生成地面物品 ID={itemId} x{stack}" : "地面物品生成失败"
            };
        }

        private static object SelectInventorySlot(int slot)
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
                return new { success = false, message = "当前未进入世界" };

            Player player = Main.LocalPlayer;
            if (slot < 0 || slot >= player.inventory.Length)
                return new { success = false, message = $"背包槽位无效: {slot}" };

            Item selectedItem = player.inventory[slot];
            if (selectedItem == null || selectedItem.IsAir)
                return new { success = false, slot, itemId = 0, message = $"背包槽位为空: {slot}" };

            player.selectedItemState.Select(slot);
            player.selectedItemState.Update();
            Item held = player.HeldItem;
            return new
            {
                success = true,
                slot,
                selectedSlot = player.selectedItem,
                itemId = held?.type ?? 0,
                currentItemId = held?.type ?? 0,
                message = $"已将槽位 {slot} 物品 [{held?.Name}] 置为当前手持物品"
            };
        }

        private static int FindInventorySlot(int itemId)
        {
            if (Main.LocalPlayer?.inventory == null)
                return -1;

            for (int i = 0; i < Main.LocalPlayer.inventory.Length; i++)
            {
                Item item = Main.LocalPlayer.inventory[i];
                if (item != null && item.type == itemId && item.stack > 0)
                    return i;
            }

            return -1;
        }

        private static object GetInventorySlotInfo(int slot)
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
                return new { inWorld = false, message = "当前未进入世界" };

            Player player = Main.LocalPlayer;
            if (slot < 0 || slot >= player.inventory.Length)
                return new { inWorld = true, success = false, message = $"背包槽位无效: {slot} (有效范围 0~{player.inventory.Length - 1})" };

            Item item = player.inventory[slot];
            if (item == null || item.IsAir || item.type <= 0)
            {
                return new
                {
                    inWorld = true,
                    slot,
                    isAir = true,
                    itemId = 0,
                    name = string.Empty,
                    displayName = string.Empty,
                    stack = 0,
                    maxStack = 0,
                    message = $"槽位 {slot} 为空"
                };
            }

            // 计算 Tooltip 提示文本与颜色
            int yoyoLogo = -1;
            int numLines = 1;
            string[] toolTipLine = new string[60];
            Color[] lineColors = new Color[60];
            for (int i = 0; i < toolTipLine.Length; i++)
            {
                toolTipLine[i] = string.Empty;
                lineColors[i] = Color.White;
            }

            int rare = item.rare;
            if (item.expert) rare = -12;
            bool isMaster = item.rare == -13 || item.rare == 13;
            if (isMaster) rare = -13;
            lineColors[0] = Main.MouseText_DrawItemTooltip_GetItemNameColor(rare, item.expert ? (byte)1 : (byte)0);

            float knockBack = item.knockBack;
            float kbMult = 1f;
            if (item.melee && player.kbGlove) kbMult += 1f;
            if (player.kbBuff) kbMult += 0.5f;
            knockBack *= kbMult;

            try
            {
                Main.MouseText_DrawItemTooltip_GetLinesInfo(item, ref yoyoLogo, knockBack, ref numLines, toolTipLine, lineColors);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TPMLBridge] 计算物品 Tooltip 异常: {ex.Message}");
            }

            var tooltipLines = new List<string>();
            var tooltipDetails = new List<object>();
            for (int i = 0; i < numLines && i < toolTipLine.Length; i++)
            {
                if (!string.IsNullOrEmpty(toolTipLine[i]))
                {
                    tooltipLines.Add(toolTipLine[i]);
                    Color c = lineColors[i];
                    tooltipDetails.Add(new
                    {
                        line = i,
                        text = toolTipLine[i],
                        color = $"#{c.R:X2}{c.G:X2}{c.B:X2}"
                    });
                }
            }

            // 基础属性与战斗属性
            string prefixName = string.Empty;
            if (item.prefix > 0 && item.prefix < Lang.prefix.Length && Lang.prefix[item.prefix] != null)
            {
                prefixName = Lang.prefix[item.prefix].Value;
            }

            string damageType = "none";
            if (item.melee) damageType = "melee";
            else if (item.ranged) damageType = "ranged";
            else if (item.magic) damageType = "magic";
            else if (item.summon) damageType = "summon";
            else if (item.sentry) damageType = "sentry";

            int actualDamage = item.damage > 0 ? player.GetWeaponDamage(item) : 0;
            int actualCrit = item.crit + player.GetWeaponCrit(item);
            float actualKnockback = player.GetWeaponKnockback(item, item.knockBack);

            long val = item.value;
            long platinum = val / 1000000;
            long gold = (val % 1000000) / 10000;
            long silver = (val % 10000) / 100;
            long copper = val % 100;

            return new
            {
                inWorld = true,
                slot,
                isAir = false,
                id = item.type,
                name = item.Name,
                displayName = GetItemDisplayName(item.type),
                stack = item.stack,
                maxStack = item.maxStack,
                favorited = item.favorited,
                prefix = item.prefix,
                prefixName,
                damage = actualDamage,
                baseDamage = item.damage,
                damageType,
                crit = actualCrit,
                knockBack = actualKnockback,
                baseKnockBack = item.knockBack,
                useTime = item.useTime,
                useAnimation = item.useAnimation,
                useStyle = item.useStyle,
                autoReuse = item.autoReuse,
                channel = item.channel,
                mana = item.mana,
                shoot = item.shoot,
                shootSpeed = item.shootSpeed,
                ammo = item.ammo,
                useAmmo = item.useAmmo,
                pick = item.pick,
                axe = item.axe * 5,
                hammer = item.hammer,
                fishingPole = item.fishingPole,
                bait = item.bait,
                tileBoost = item.tileBoost,
                createTile = item.createTile,
                createWall = item.createWall,
                defense = item.defense,
                accessory = item.accessory,
                headSlot = item.headSlot,
                bodySlot = item.bodySlot,
                legSlot = item.legSlot,
                vanity = item.vanity,
                lifeRegen = item.lifeRegen,
                healLife = item.healLife,
                healMana = item.healMana,
                buffType = item.buffType,
                buffTime = item.buffTime,
                rare = item.rare,
                expert = item.expert,
                master = isMaster,
                material = item.material,
                consumable = item.consumable,
                value = item.value,
                price = new { platinum, gold, silver, copper },
                tooltipLines,
                tooltipDetails
            };
        }

        private static object UseHeldItem(int? targetTileX, int? targetTileY, float? targetWorldX, float? targetWorldY, int? direction, bool? altUse, int? holdFrames)
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
                return new { success = false, message = "当前未进入世界" };

            Player p = Main.LocalPlayer;
            if (p.dead || p.CCed)
                return new { success = false, message = "玩家当前处于死亡或被控制状态，无法使用物品" };

            Item item = p.HeldItem;
            if (item == null || item.IsAir || item.useStyle == 0)
                return new { success = false, message = "当前手持物品为空或不可使用" };

            // 计算瞄准的世界坐标与屏幕坐标
            Vector2 targetPos = p.Center + new Vector2(p.direction * 32f, 0f);
            if (targetTileX.HasValue && targetTileY.HasValue)
            {
                targetPos = new Vector2(targetTileX.Value * 16f + 8f, targetTileY.Value * 16f + 8f);
            }
            else if (targetWorldX.HasValue && targetWorldY.HasValue)
            {
                targetPos = new Vector2(targetWorldX.Value, targetWorldY.Value);
            }

            Main.mouseX = (int)(targetPos.X - Main.screenPosition.X);
            Main.mouseY = (int)(targetPos.Y - Main.screenPosition.Y);

            if (direction.HasValue && (direction.Value == -1 || direction.Value == 1))
            {
                p.direction = direction.Value;
            }
            else
            {
                p.direction = (targetPos.X >= p.Center.X) ? 1 : -1;
            }

            bool isAlt = altUse.HasValue && altUse.Value;
            if (isAlt)
            {
                p.altFunctionUse = 1;
            }

            p.controlUseItem = true;
            p.releaseUseItem = true;

            // 立即触发 ItemCheck
            p.ItemCheck();

            int frames = holdFrames.HasValue && holdFrames.Value > 1 ? holdFrames.Value : 1;
            if (frames > 1)
            {
                PendingHoldUseFrames = frames - 1;
                PendingHoldAlt = isAlt;
            }

            return new
            {
                success = true,
                itemId = item.type,
                itemName = item.Name,
                slot = p.selectedItem,
                itemAnimation = p.itemAnimation,
                itemTime = p.itemTime,
                channel = p.channel,
                direction = p.direction,
                altUse = isAlt,
                targetPos = new { x = targetPos.X, y = targetPos.Y },
                message = $"已触发使用手持物品 [{item.Name}] (ID: {item.type})"
            };
        }

        private static string GetItemDisplayName(int itemId)
        {
            string name = Lang.GetItemNameValue(itemId);
            if (string.IsNullOrEmpty(name) || name.StartsWith("ModItem_", StringComparison.Ordinal))
                name = ItemLoader.GetDisplayName(itemId);
            return name ?? string.Empty;
        }

        private static object GetLastBuildResult()
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
                return new { inWorld = false, message = "当前未进入世界" };

            object summary = null;
            bool isBuilding = false;
            int pendingCells = 0;
            bool foundType = false;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var t in asm.GetTypes())
                    {
                        if (t.FullName == "Instavator.Content.Logic.InstavatorShaftBuilder" || t.Name == "InstavatorShaftBuilder")
                        {
                            foundType = true;
                            var isRunningProp = t.GetProperty("IsBuildRunning", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                            var pendingProp = t.GetProperty("PendingCellCount", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                            var prop = t.GetProperty("LastBuildSummary", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                            bool curBuilding = (bool)(isRunningProp?.GetValue(null) ?? false);
                            int curPending = (int)(pendingProp?.GetValue(null) ?? 0);
                            var curSummary = prop?.GetValue(null);

                            if (curBuilding) isBuilding = true;
                            if (curPending > pendingCells) pendingCells = curPending;
                            if (curSummary != null) summary = curSummary;
                        }
                    }
                }
                catch { }
            }

            if (!foundType)
            {
                return new { inWorld = true, hasHistory = false, message = "未检测到 Instavator 模组程序集" };
            }

            if (summary == null && !isBuilding)
            {
                return new { inWorld = true, hasHistory = false, isBuilding = false, message = "当前会话尚未执行过直通车建造任务" };
            }

            return new
            {
                inWorld = true,
                hasHistory = summary != null || isBuilding,
                isBuilding,
                pendingCells,
                summary
            };
        }

        private static object InspectShaft(int? centerX, int? startY, int? targetY, string variant)
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
                return new { inWorld = false, message = "当前未进入世界" };

            Player player = Main.LocalPlayer;
            int cX = centerX.HasValue ? centerX.Value : (int)(player.Center.X / 16f);
            int sY = startY.HasValue ? startY.Value : (int)(player.Bottom.Y / 16f);
            int tY = targetY.HasValue ? targetY.Value : (Main.maxTilesY - 40);

            if (cX < 10 || cX >= Main.maxTilesX - 10)
                return new { inWorld = true, success = false, message = $"中心坐标 X={cX} 超出有效世界范围 (10~{Main.maxTilesX - 10})" };

            if (sY < 10) sY = 10;
            if (tY >= Main.maxTilesY - 10) tY = Main.maxTilesY - 10;
            if (sY >= tY)
                return new { inWorld = true, success = false, message = $"起始深度 ({sY}) 必须小于目标深度 ({tY})" };

            int totalDepth = tY - sY + 1;

            // 确定规格与通道 offset 范围
            string varStr = string.IsNullOrWhiteSpace(variant) ? "Auto" : variant.Trim();
            int minOffset = -3;
            int maxOffset = 3;
            if (varStr.Equals("Half", StringComparison.OrdinalIgnoreCase))
            {
                minOffset = -2;
                maxOffset = 2;
                varStr = "Half";
            }
            else if (varStr.Equals("DoubleObsidian", StringComparison.OrdinalIgnoreCase))
            {
                minOffset = -5;
                maxOffset = 5;
                varStr = "DoubleObsidian";
            }
            else if (varStr.Equals("Full", StringComparison.OrdinalIgnoreCase))
            {
                minOffset = -3;
                maxOffset = 3;
                varStr = "Full";
            }
            else
            {
                // Auto 探测: 查看 cX - 5 是否有黑曜石砖
                int checkY = Math.Min(sY + 10, tY);
                if (WorldGen.InWorld(cX - 5, checkY, 0) && Main.tile[cX - 5, checkY]?.active() == true && Main.tile[cX - 5, checkY]?.type == TileID.ObsidianBrick)
                {
                    minOffset = -5;
                    maxOffset = 5;
                    varStr = "DoubleObsidian";
                }
                else if (WorldGen.InWorld(cX - 3, checkY, 0) && Main.tile[cX - 3, checkY]?.active() == true && Main.tile[cX - 3, checkY]?.type == TileID.ObsidianBrick)
                {
                    minOffset = -3;
                    maxOffset = 3;
                    varStr = "Full";
                }
                else
                {
                    minOffset = -2;
                    maxOffset = 2;
                    varStr = "Half";
                }
            }

            int width = maxOffset - minOffset + 1;
            int totalInternalTiles = 0;
            int clearAirTiles = 0;
            int solidObstacleTiles = 0;
            int liquidTiles = 0;
            int ropeTiles = 0;
            int expectedRopes = (varStr == "DoubleObsidian" ? 2 : 1) * totalDepth;
            int torchTiles = 0;
            int brickTiles = 0;
            int wallTiles = 0;
            int totalWallCheckCount = totalDepth * width;

            var obstacleSamples = new List<object>();
            var ropeGaps = new List<int>();

            for (int y = sY; y <= tY; y++)
            {
                // 1. 检查绳索
                if (varStr == "DoubleObsidian")
                {
                    bool hasRopeLeft = WorldGen.InWorld(cX - 2, y, 0) && Main.tile[cX - 2, y]?.active() == true && Main.tile[cX - 2, y]?.type == TileID.Rope;
                    bool hasRopeRight = WorldGen.InWorld(cX + 2, y, 0) && Main.tile[cX + 2, y]?.active() == true && Main.tile[cX + 2, y]?.type == TileID.Rope;
                    if (hasRopeLeft) ropeTiles++;
                    if (hasRopeRight) ropeTiles++;
                    if (!hasRopeLeft || !hasRopeRight)
                    {
                        if (ropeGaps.Count < 20) ropeGaps.Add(y);
                    }
                }
                else
                {
                    bool hasRope = WorldGen.InWorld(cX, y, 0) && Main.tile[cX, y]?.active() == true && Main.tile[cX, y]?.type == TileID.Rope;
                    if (hasRope) ropeTiles++;
                    else
                    {
                        if (ropeGaps.Count < 20) ropeGaps.Add(y);
                    }
                }

                // 2. 检查每一列
                for (int off = minOffset; off <= maxOffset; off++)
                {
                    int x = cX + off;
                    if (!WorldGen.InWorld(x, y, 0)) continue;
                    Tile tile = Main.tile[x, y];
                    if (tile == null) continue;

                    // 检查背景墙
                    if (tile.wall > 0) wallTiles++;

                    bool isBorder = (varStr != "Half") && (off == minOffset || off == maxOffset || (varStr == "DoubleObsidian" && off == 0));
                    if (isBorder)
                    {
                        if (tile.active() && tile.type == TileID.ObsidianBrick)
                        {
                            brickTiles++;
                        }
                    }
                    else
                    {
                        // 内部通道格
                        totalInternalTiles++;
                        if (tile.liquid > 0)
                        {
                            liquidTiles++;
                        }

                        if (!tile.active())
                        {
                            clearAirTiles++;
                        }
                        else
                        {
                            if (tile.type == TileID.Rope)
                            {
                                clearAirTiles++;
                            }
                            else if (tile.type == TileID.Torches)
                            {
                                torchTiles++;
                                clearAirTiles++;
                            }
                            else
                            {
                                // 实心阻挡方块
                                solidObstacleTiles++;
                                if (obstacleSamples.Count < 20)
                                {
                                    obstacleSamples.Add(new { x, y, tileType = tile.type });
                                }
                            }
                        }
                    }
                }
            }

            int expectedBricks = (varStr == "Full" ? 2 : (varStr == "DoubleObsidian" ? 3 : 0)) * totalDepth;
            double passablePercent = totalInternalTiles > 0 ? Math.Round((double)clearAirTiles / totalInternalTiles * 100.0, 2) : 100.0;
            double ropeContinuity = expectedRopes > 0 ? Math.Round((double)ropeTiles / expectedRopes * 100.0, 2) : 100.0;
            double brickIntegrity = expectedBricks > 0 ? Math.Round((double)brickTiles / expectedBricks * 100.0, 2) : 100.0;
            double wallCoverage = totalWallCheckCount > 0 ? Math.Round((double)wallTiles / totalWallCheckCount * 100.0, 2) : 100.0;

            bool passable = solidObstacleTiles == 0 && liquidTiles == 0;
            string evaluation = "PERFECT";
            if (passablePercent < 80.0 || ropeContinuity < 80.0) evaluation = "BROKEN";
            else if (solidObstacleTiles > 0 || liquidTiles > 0) evaluation = "OBSTRUCTED";
            else if (ropeContinuity < 99.0) evaluation = "GOOD";

            string summary = passable
                ? $"矿道已完美贯通！规格 [{varStr}] | 总深度: {totalDepth} 格 (Y: {sY}~{tY}) | 直通率: {passablePercent}% | 绳索连续率: {ropeContinuity}% | 火把: {torchTiles} 根 | 残留液体: 0 格"
                : $"矿道存在局部阻碍。规格 [{varStr}] | 阻挡方块: {solidObstacleTiles} 格 | 残留液体: {liquidTiles} 格 | 绳索断点数: {ropeGaps.Count} | 直通率: {passablePercent}%";

            return new
            {
                inWorld = true,
                success = true,
                centerX = cX,
                startY = sY,
                targetY = tY,
                totalDepth,
                width,
                variant = varStr,
                passable,
                passablePercent,
                solidObstacleTiles,
                obstacleSamples,
                ropeTiles,
                expectedRopes,
                ropeContinuityPercent = ropeContinuity,
                ropeGaps,
                torchTiles,
                liquidResidualTiles = liquidTiles,
                brickTiles,
                expectedBricks,
                brickIntegrityPercent = brickIntegrity,
                wallTiles,
                wallCoveragePercent = wallCoverage,
                evaluation,
                summary
            };
        }

        private static object SwapInventorySlots(int fromSlot, int toSlot)
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
                return new { success = false, message = "当前未进入世界" };

            Player p = Main.LocalPlayer;
            if (fromSlot < 0 || fromSlot >= p.inventory.Length || toSlot < 0 || toSlot >= p.inventory.Length)
                return new { success = false, message = $"槽位索引超出有效范围 (0~{p.inventory.Length - 1})" };

            Utils.Swap(ref p.inventory[fromSlot], ref p.inventory[toSlot]);
            return new
            {
                success = true,
                fromSlot,
                fromItemId = p.inventory[fromSlot]?.type ?? 0,
                fromItemName = p.inventory[fromSlot]?.Name ?? string.Empty,
                toSlot,
                toItemId = p.inventory[toSlot]?.type ?? 0,
                toItemName = p.inventory[toSlot]?.Name ?? string.Empty,
                message = $"已交换槽位 {fromSlot} 与 {toSlot}"
            };
        }

        private static Type GetCreativeInventoryType()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = asm.GetType("OptimizeAndTool.Content.Creative.CreativeInventory");
                    if (type != null) return type;

                    foreach (var t in asm.GetTypes())
                    {
                        if (t.FullName == "OptimizeAndTool.Content.Creative.CreativeInventory" || t.Name == "CreativeInventory")
                        {
                            return t;
                        }
                    }
                }
                catch { }
            }
            return null;
        }

        private static object TestCreativeInventory()
        {
            Type type = GetCreativeInventoryType();
            if (type == null)
            {
                return new { isAvailable = false, message = "未检测到 OptimizeAndTool 模组程序集" };
            }

            var isOpenProp = type.GetProperty("IsOpen", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var isHoveringProp = type.GetProperty("IsHovering", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var uiProp = type.GetProperty("UI", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            bool isOpen = (bool)(isOpenProp?.GetValue(null) ?? false);
            bool isHovering = (bool)(isHoveringProp?.GetValue(null) ?? false);
            string searchText = null;
            int matchedCount = 0;
            bool textBoxFocus = false;
            string textBoxText = null;

            var uiObj = uiProp?.GetValue(null);
            if (uiObj != null)
            {
                var uiType = uiObj.GetType();
                var searchField = uiType.GetField("Search_Text", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                searchText = searchField?.GetValue(uiObj)?.ToString();

                var matchedProp = uiType.GetProperty("MatchedCount", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                matchedCount = (int)(matchedProp?.GetValue(uiObj) ?? 0);

                var searchTextBoxProp = uiType.GetProperty("SearchTextBox", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var tbObj = searchTextBoxProp?.GetValue(uiObj);
                if (tbObj != null)
                {
                    var tbType = tbObj.GetType();
                    var focusProp = tbType.GetProperty("Focus", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var textProp = tbType.GetProperty("Text", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    textBoxFocus = (bool)(focusProp?.GetValue(tbObj) ?? false);
                    textBoxText = textProp?.GetValue(tbObj)?.ToString();
                }
            }

            return new
            {
                isAvailable = true,
                isOpen,
                isHovering,
                searchText,
                textBoxText,
                textBoxFocus,
                writingText = Terraria.GameInput.PlayerInput.WritingText,
                currentInputTaker = Main.CurrentInputTextTakerOverride != null ? Main.CurrentInputTextTakerOverride.GetType().Name : null,
                matchedCount
            };
        }

        private static object ToggleCreativeInventory(bool? open)
        {
            Type type = GetCreativeInventoryType();
            if (type == null)
            {
                return new { success = false, message = "未找到 OptimizeAndTool 模组程序集" };
            }

            var switchMethod = type.GetMethod("SwitchOpenOrClose", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var isOpenProp = type.GetProperty("IsOpen", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            bool curOpen = (bool)(isOpenProp?.GetValue(null) ?? false);

            if (!open.HasValue || open.Value != curOpen)
            {
                switchMethod?.Invoke(null, null);
            }

            bool finalOpen = (bool)(isOpenProp?.GetValue(null) ?? false);
            return new
            {
                success = true,
                isOpen = finalOpen,
                message = finalOpen ? "创造模式物品浏览器已打开" : "创造模式物品浏览器已关闭"
            };
        }

        private static object SetCreativeSearch(string query)
        {
            Type type = GetCreativeInventoryType();
            if (type == null)
            {
                return new { success = false, message = "未找到创造模式物品浏览器" };
            }

            var isOpenProp = type.GetProperty("IsOpen", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            bool curOpen = (bool)(isOpenProp?.GetValue(null) ?? false);
            if (!curOpen)
            {
                var switchMethod = type.GetMethod("SwitchOpenOrClose", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                switchMethod?.Invoke(null, null);
            }

            var uiProp = type.GetProperty("UI", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var uiObj = uiProp?.GetValue(null);
            if (uiObj != null)
            {
                var uiType = uiObj.GetType();
                var applyMethod = uiType.GetMethod("ApplySearchImmediate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                applyMethod?.Invoke(uiObj, new object[] { query });

                var matchedProp = uiType.GetProperty("MatchedCount", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                int matchedCount = (int)(matchedProp?.GetValue(uiObj) ?? 0);

                return new
                {
                    success = true,
                    query,
                    matchedCount,
                    message = $"已搜索 [{query}]，共匹配 {matchedCount} 个物品"
                };
            }

            return new { success = false, message = "创造模式物品浏览器 UI 实例为空" };
        }

        private static object FocusCreativeSearch(bool focus)
        {
            Type type = GetCreativeInventoryType();
            if (type == null)
            {
                return new { success = false, message = "未找到创造模式物品浏览器" };
            }

            var isOpenProp = type.GetProperty("IsOpen", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            bool curOpen = (bool)(isOpenProp?.GetValue(null) ?? false);
            if (!curOpen && focus)
            {
                var switchMethod = type.GetMethod("SwitchOpenOrClose", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                switchMethod?.Invoke(null, null);
            }

            var uiProp = type.GetProperty("UI", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var uiObj = uiProp?.GetValue(null);
            if (uiObj != null)
            {
                var uiType = uiObj.GetType();
                var searchTextBoxProp = uiType.GetProperty("SearchTextBox", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var tbObj = searchTextBoxProp?.GetValue(uiObj);
                if (tbObj != null)
                {
                    var tbType = tbObj.GetType();
                    var focusProp = tbType.GetProperty("Focus", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    focusProp?.SetValue(tbObj, focus);

                    return new
                    {
                        success = true,
                        focus,
                        writingText = Terraria.GameInput.PlayerInput.WritingText,
                        currentInputTaker = Main.CurrentInputTextTakerOverride != null ? Main.CurrentInputTextTakerOverride.GetType().Name : null,
                        message = focus ? "已激活搜索框输入焦点" : "已释放搜索框焦点"
                    };
                }
            }

            return new { success = false, message = "未找到创造模式物品浏览器搜索框" };
        }

        private static object SimulateScrollWheel(int delta, bool forUI)
        {
            int oldSelected = Main.LocalPlayer?.selectedItem ?? -1;

            Terraria.GameInput.PlayerInput.ScrollWheelDelta = delta;
            if (forUI)
            {
                Terraria.GameInput.PlayerInput.ScrollWheelDeltaForUI = delta;
            }

            if (!Main.gameMenu && Main.LocalPlayer != null)
            {
                Main.LocalPlayer.HandleHotbarControls();
            }

            int newSelected = Main.LocalPlayer?.selectedItem ?? -1;

            return new
            {
                success = true,
                delta,
                forUI,
                oldSelectedItem = oldSelected,
                newSelectedItem = newSelected,
                slotChanged = oldSelected != newSelected,
                scrollWheelDelta = Terraria.GameInput.PlayerInput.ScrollWheelDelta,
                scrollWheelDeltaForUI = Terraria.GameInput.PlayerInput.ScrollWheelDeltaForUI,
                message = $"已模拟滚轮滚动 {delta} (快捷栏槽位: {oldSelected} -> {newSelected})"
            };
        }
    }
}
