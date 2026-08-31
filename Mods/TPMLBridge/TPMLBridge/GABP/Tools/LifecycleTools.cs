using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Terraria;
using Terraria.IO;
using TPML.Content.IO;
using TPML.Core.Logging;

namespace TPMLBridge.GABP.Tools
{
    /// <summary>
    /// 游戏生命周期、状态查询、存档保护与指令执行工具
    /// 作者: SaintCirno9
    /// </summary>
    public static class LifecycleTools
    {
        private static readonly ILogger Logger = LogManager.GetLogger("LifecycleTools");
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
                    Description = "一键跳过主菜单动画与选人界面，直接加载指定角色与世界并进入单人游戏（默认开启测试存档保护）。",
                    Tags = new List<string> { "lifecycle" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            playerName = new { type = "string", description = "角色名称（留空则选择最近使用或第一个角色）" },
                            worldName = new { type = "string", description = "世界名称（留空则选择最近使用或第一个世界）" },
                            protectSave = new { type = "boolean", description = "是否在自动化测试期间保护世界存档不被写盘修改（默认 true）" }
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
                        properties = new
                        {
                            save = new { type = "boolean", description = "是否保存世界（若未开启存档保护且显式为 true 时才保存，默认 false）" }
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
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/run_command",
                    Description = "向游戏内聊天框执行聊天指令（例如 /time set day, /godmode 等）或发送聊天文本。",
                    Tags = new List<string> { "write", "chat" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "command" },
                        properties = new
                        {
                            command = new { type = "string", description = "要执行的指令字符串或聊天信息" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/reload_mods",
                    Description = "触发 TPML 现有模组加载器执行完整卸载与重新加载。调用后 Bridge 会短暂断开并重启。",
                    Tags = new List<string> { "lifecycle" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                }
            };
        }

        public static async Task<object> HandleAsync(string name, JObject args)
        {
            switch (name)
            {
                case "tpml/get_game_state":
                case "tpml_get_game_state":
                    return await MainThreadQueue.EnqueueAsync(() => GetGameState());

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

                case "tpml/run_command":
                case "tpml_run_command":
                    {
                        string cmd = args?["command"]?.ToString();
                        return await MainThreadQueue.EnqueueAsync(() => RunCommand(cmd));
                    }

                case "tpml/reload_mods":
                case "tpml_reload_mods":
                    return await MainThreadQueue.EnqueueAsync(() => ReloadMods());

                default:
                    return null;
            }
        }

        public static object GetGameState()
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
                    defense = p.statDefense,
                    meleeDamage = p.meleeDamage,
                    rangedDamage = p.rangedDamage,
                    magicDamage = p.magicDamage,
                    minionDamage = p.minionDamage,
                    manaCost = p.manaCost,
                    pickSpeed = p.pickSpeed,
                    maxTurrets = p.maxTurrets,
                    wingsLogic = p.wingsLogic,
                    tileSpeed = p.tileSpeed,
                    wallSpeed = p.wallSpeed,
                    skyStoneEffects = p.skyStoneEffects,
                    manaFlower = p.manaFlower,
                    chiselSpeed = p.chiselSpeed,
                    dd2Accessory = p.dd2Accessory,
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

        public static object ListPlayers()
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

        public static object ListWorlds()
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

        public static object LoadWorld(string playerName, string worldName, bool protectSave = true)
        {
            if (protectSave)
            {
                TPMLBridgeMod.WorldSaveProtectionEnabled = true;
                TPMLBridgeMod.PlayerSaveProtectionEnabled = true;
            }

            Main.LoadPlayers();
            PlayerFileData player = null;
            if (!string.IsNullOrEmpty(playerName))
            {
                player = Main.PlayerList.FirstOrDefault(p => p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
            }
            if (player == null)
            {
                // 优先查找名为 "Test" 的专用测试角色，其次按最后游玩时间排序取最新角色
                player = Main.PlayerList.FirstOrDefault(p => p.Name.Equals("Test", StringComparison.OrdinalIgnoreCase))
                         ?? Main.PlayerList.OrderByDescending(p => p.LastPlayed).FirstOrDefault();
            }

            if (player == null)
            {
                if (protectSave) TPMLBridgeMod.ResetSaveProtection();
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
                // 优先查找名为 "Test" 的专用测试世界，其次按最后游玩时间排序取最新世界
                world = Main.WorldList.FirstOrDefault(w => w.Name.Equals("Test", StringComparison.OrdinalIgnoreCase))
                        ?? Main.WorldList.OrderByDescending(w => w.LastPlayed).FirstOrDefault();
            }

            if (world == null)
            {
                if (protectSave) TPMLBridgeMod.ResetSaveProtection();
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

        public static object LeaveWorld(bool save = false)
        {
            if (Main.gameMenu)
            {
                TPMLBridgeMod.ResetSaveProtection();
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

            // 离开世界时立即重置扩展容器与吸管工具状态
            ModItemSidecarEngine.ResetContainers();

            // 退出自动化测试后复位保护标志，确保玩家后续正常游玩时能够正常保存
            bool wasProtected = TPMLBridgeMod.WorldSaveProtectionEnabled || TPMLBridgeMod.PlayerSaveProtectionEnabled;
            TPMLBridgeMod.ResetSaveProtection();

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

        public static object SetSaveProtection(bool? enabled)
        {
            if (enabled.HasValue)
            {
                TPMLBridgeMod.WorldSaveProtectionEnabled = enabled.Value;
                TPMLBridgeMod.PlayerSaveProtectionEnabled = enabled.Value;
            }

            return new
            {
                success = true,
                worldSaveProtectionEnabled = TPMLBridgeMod.WorldSaveProtectionEnabled,
                playerSaveProtectionEnabled = TPMLBridgeMod.PlayerSaveProtectionEnabled,
                message = TPMLBridgeMod.WorldSaveProtectionEnabled
                    ? "自动化测试存档保护已开启（WorldFile.SaveWorld 与 Player.SavePlayer 均被拦截）"
                    : "自动化测试存档保护已关闭（允许正常写盘保存）"
            };
        }

        public static object RunCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return new { success = false, message = "指令内容为空" };
            }

            if (!Main.gameMenu && Main.LocalPlayer != null)
            {
                Main.NewText($"[TPMLBridge] {command}", 100, 255, 200);
            }

            Logger.Info($"[TPMLBridge Command] {command}");
            tContentPatch.ContentPatch.RunCommand(command);
            return new { success = true, command, message = "指令已执行" };
        }

        public static object ReloadMods()
        {
            tContentPatch.ContentPatch.ReloadMods();
            return new { success = true, message = "已触发模组重载，Bridge 将短暂断开并自动恢复" };
        }
    }
}
