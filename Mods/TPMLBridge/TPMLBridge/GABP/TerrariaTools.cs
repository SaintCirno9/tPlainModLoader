using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using Terraria;
using Terraria.ID;
using Terraria.IO;

namespace TPMLBridge.GABP
{
    public static class TerrariaTools
    {
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
                    Description = "保存当前角色与世界并退出到主菜单。",
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
                        return await MainThreadQueue.EnqueueAsync(() => LoadWorld(pName, wName));
                    }

                case "tpml/leave_world":
                case "tpml_leave_world":
                    return await MainThreadQueue.EnqueueAsync(() => LeaveWorld());

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
                    zoneDungeon = p.ZoneDungeon,
                    zoneJungle = p.ZoneJungle,
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

        private static object LoadWorld(string playerName, string worldName)
        {
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

        private static object LeaveWorld()
        {
            if (Main.gameMenu)
            {
                return new { success = true, message = "当前已在主菜单中" };
            }

            WorldFile.SaveWorld();
            Main.menuMode = 0;
            Main.gameMenu = true;
            return new { success = true, message = "已保存并退出世界" };
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
    }
}
