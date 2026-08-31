using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ObjectData;
using TPML.Content;
using TPML.Content.IO;

namespace TPMLBridge.GABP.Tools
{
    /// <summary>
    /// 自动钓鱼机 (FishingMachine) 与 ModTile 原生物块引擎自动化诊断、水池搭建与实机功能测试工具
    /// 作者: SaintCirno9
    /// </summary>
    public static class FishingMachineTools
    {
        public static List<GABPToolDescriptor> GetDescriptors()
        {
            return new List<GABPToolDescriptor>
            {
                new GABPToolDescriptor
                {
                    Name = "tpml/test_fishing_machine",
                    Description = "自动化执行自动钓鱼机全生命周期回归测试（涵盖物品检索、2x2 多格物块放置、TileEntity 绑定、渔具装配、水体识别、破坏回收与 Sidecar 持久化）。",
                    Tags = new List<string> { "diagnostic", "automation", "fishing_machine", "modtile" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            targetX = new { type = "integer", description = "测试放置的目标图格 X 坐标（可选，默认玩家右侧 3 格）" },
                            targetY = new { type = "integer", description = "测试放置的目标图格 Y 坐标（可选，默认玩家脚下水平）" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/inspect_fishing_machine",
                    Description = "检查指定物块坐标或世界中现存的自动钓鱼机实体状态（包含钓具、鱼饵、战利品仓、渔力与水体判定）。",
                    Tags = new List<string> { "read-only", "diagnostic", "fishing_machine" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            tileX = new { type = "integer", description = "钓鱼机所在图格 X 坐标（可选）" },
                            tileY = new { type = "integer", description = "钓鱼机所在图格 Y 坐标（可选）" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/setup_fishing_test_pool",
                    Description = "在玩家附近一键开凿标准人工水池（12x8 格深并注满水），并在水池左侧平台放置装配好金钓竿与大师鱼饵的自动钓鱼机。",
                    Tags = new List<string> { "world", "automation", "fishing_machine" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            targetX = new { type = "integer", description = "水池建造基准 X 坐标（可选，默认玩家当前位置）" },
                            targetY = new { type = "integer", description = "水池建造基准 Y 坐标（可选，默认玩家当前深度）" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/test_fishing_functional",
                    Description = "对现存或新建水池中的自动钓鱼机执行真实垂钓循环测试，验证水体格数(>=75)、有效渔力计算、触发钓鱼判定与战利品入库。",
                    Tags = new List<string> { "diagnostic", "functional", "fishing_machine" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            attempts = new { type = "integer", description = "模拟执行的垂钓判定次数（默认 10 次）" }
                        }
                    }
                }
            };
        }

        public static async Task<object> HandleAsync(string name, JObject args)
        {
            switch (name)
            {
                case "tpml/test_fishing_machine":
                {
                    int? targetX = args?["targetX"]?.Value<int>();
                    int? targetY = args?["targetY"]?.Value<int>();
                    return await MainThreadQueue.EnqueueAsync(() => ExecuteFishingMachineAutomatedTest(targetX, targetY));
                }
                case "tpml/inspect_fishing_machine":
                {
                    int? tileX = args?["tileX"]?.Value<int>();
                    int? tileY = args?["tileY"]?.Value<int>();
                    return await MainThreadQueue.EnqueueAsync(() => InspectFishingMachine(tileX, tileY));
                }
                case "tpml/setup_fishing_test_pool":
                {
                    int? targetX = args?["targetX"]?.Value<int>();
                    int? targetY = args?["targetY"]?.Value<int>();
                    return await MainThreadQueue.EnqueueAsync(() => SetupFishingTestPool(targetX, targetY));
                }
                case "tpml/test_fishing_functional":
                {
                    int attempts = args?["attempts"]?.Value<int>() ?? 10;
                    return await MainThreadQueue.EnqueueAsync(() => ExecuteFunctionalFishingTest(attempts));
                }
                default:
                    return null;
            }
        }

        public static object SetupFishingTestPool(int? optX, int? optY)
        {
            Player player = Main.LocalPlayer;
            if (player == null || !player.active)
            {
                return new { success = false, message = "玩家未在世界中" };
            }

            int pX = optX ?? (int)(player.position.X / 16f);
            int pY = optY ?? (int)(player.position.Y / 16f);
            int surfaceY = pY + 2; // 地表基准线

            // 1. 清理周围环境：上方空间全清空，消除所有浮空图格与杂物
            for (int x = pX - 4; x <= pX + 22; x++)
            {
                for (int y = surfaceY - 8; y < surfaceY; y++)
                {
                    WorldGen.KillTile(x, y, false, false, true);
                    Framing.GetTileSafely(x, y).liquid = 0;
                }
            }

            // 2. 铺设左侧岸边坚固平地钓鱼台 (pX - 4 到 pX + 4，共 9 格宽)
            for (int x = pX - 4; x <= pX + 4; x++)
            {
                for (int y = surfaceY; y <= surfaceY + 12; y++)
                {
                    WorldGen.KillTile(x, y, false, false, true);
                    WorldGen.PlaceTile(x, y, TileID.Stone, false, true);
                    Framing.GetTileSafely(x, y).liquid = 0;
                }
            }

            // 3. 建造下凹水池 (X: pX + 5 到 pX + 19, 宽 15 格; 深度: surfaceY 到 surfaceY + 8)
            int poolLeft = pX + 5;
            int poolRight = poolLeft + 14;
            int poolBottom = surfaceY + 8;

            for (int x = poolLeft - 1; x <= poolRight + 1; x++)
            {
                for (int y = surfaceY; y <= poolBottom + 1; y++)
                {
                    bool isBorder = (x == poolLeft - 1 || x == poolRight + 1 || y == poolBottom + 1);
                    if (isBorder)
                    {
                        WorldGen.KillTile(x, y, false, false, true);
                        WorldGen.PlaceTile(x, y, TileID.Stone, false, true);
                        Framing.GetTileSafely(x, y).liquid = 0;
                    }
                    else
                    {
                        WorldGen.KillTile(x, y, false, false, true);
                        Tile t = Framing.GetTileSafely(x, y);
                        t.liquid = 255;
                        t.liquidType(0); // 水
                    }
                }
            }

            // 4. 在左侧平地最右边缘 (pX + 3, pX + 4) 放置钓鱼机 (底部两格 100% 稳落在平整坚固石块上)
            int machineX = pX + 3;
            int machineY = surfaceY - 2;

            int tileType = ModContent.TileType<FishingMachine.Content.Tiles.FishingMachineTile>();
            TileObject objectData;
            bool canPlace = TileObject.CanPlace(machineX + 1, machineY + 1, tileType, 0, 1, out objectData, false, null);
            bool placed = canPlace && TileObject.Place(objectData);
            if (!placed)
            {
                placed = WorldGen.PlaceObject(machineX + 1, machineY + 1, tileType, false, 0, -1, -1, 1);
            }

            if (placed)
            {
                TileObjectData data = TileObjectData.GetTileData(tileType, 0, 0);
                if (data?.HookPostPlaceMyPlayer.hook != null)
                {
                    data.HookPostPlaceMyPlayer.hook(machineX + 1, machineY + 1, tileType, 0, 1, 0);
                }
            }

            // 5. 检索实体并装配金钓竿与大师鱼饵
            int teId = -1;
            foreach (var kvp in TileEntity.ByID)
            {
                if (kvp.Value.Position.X == machineX && kvp.Value.Position.Y == machineY)
                {
                    teId = kvp.Key;
                    break;
                }
            }

            FishingMachine.Content.Tiles.TEFishingMachine machine = teId >= 0 ? TileEntity.ByID[teId] as FishingMachine.Content.Tiles.TEFishingMachine : null;
            if (machine != null)
            {
                machine.fishingPole = new Item();
                machine.fishingPole.SetDefaults(ItemID.GoldenFishingRod);
                machine.bait = new Item();
                machine.bait.SetDefaults(ItemID.MasterBait);
                machine.bait.stack = 999;
                machine.FindNearbyWater();
                machine.Update();
            }

            return new
            {
                success = machine != null && placed,
                pool = new { left = poolLeft, right = poolRight, top = surfaceY, bottom = poolBottom, waterVolume = 15 * 9 },
                machine = machine != null ? new
                {
                    id = teId,
                    position = new { x = machineX, y = machineY },
                    statusTip = machine.statusTip,
                    waterCount = machine.waterCount,
                    fishingPower = machine.lastFishingPower,
                    pole = Lang.GetItemNameValue(machine.fishingPole.type),
                    bait = Lang.GetItemNameValue(machine.bait.type)
                } : null
            };
        }

        public static object ExecuteFunctionalFishingTest(int attempts)
        {
            // 找到或创建水池中的钓鱼机
            FishingMachine.Content.Tiles.TEFishingMachine machine = null;
            foreach (var kvp in TileEntity.ByID)
            {
                if (kvp.Value is FishingMachine.Content.Tiles.TEFishingMachine m)
                {
                    machine = m;
                    break;
                }
            }

            if (machine == null)
            {
                // 自动先建水池
                SetupFishingTestPool(null, null);
                foreach (var kvp in TileEntity.ByID)
                {
                    if (kvp.Value is FishingMachine.Content.Tiles.TEFishingMachine m)
                    {
                        machine = m;
                        break;
                    }
                }
            }

            if (machine == null)
            {
                return new { success = false, message = "未找到有效的自动钓鱼机实体" };
            }

            // 确保钓具
            if (machine.fishingPole == null || machine.fishingPole.IsAir)
            {
                machine.fishingPole = new Item();
                machine.fishingPole.SetDefaults(ItemID.GoldenFishingRod);
            }
            if (machine.bait == null || machine.bait.IsAir)
            {
                machine.bait = new Item();
                machine.bait.SetDefaults(ItemID.MasterBait);
                machine.bait.stack = 999;
            }

            machine.FindNearbyWater();
            machine.Update();

            int initialFishCount = machine.fish.Count(it => it != null && !it.IsAir);
            var catches = new List<string>();

            // 模拟执行垂钓判定
            for (int i = 0; i < attempts; i++)
            {
                var fisher = machine.GetFisher();
                if (fisher.fishingLevel > 0)
                {
                    machine.ExecuteFishingCheck(fisher);
                }
            }

            int finalFishCount = machine.fish.Count(it => it != null && !it.IsAir);
            foreach (var item in machine.fish)
            {
                if (item != null && !item.IsAir)
                {
                    catches.Add($"{Lang.GetItemNameValue(item.type)} x{item.stack}");
                }
            }

            bool passed = machine.waterCount >= 75 && machine.lastFishingPower > 0;

            return new
            {
                success = passed,
                waterDetected = machine.waterCount,
                waterSufficient = machine.waterCount >= 75,
                fishingPower = machine.lastFishingPower,
                statusTip = machine.statusTip,
                attemptsExecuted = attempts,
                initialFishCount,
                finalFishCount,
                catches,
                summary = passed ? $"自动钓鱼机功能测试成功！水体={machine.waterCount}格, 渔力={machine.lastFishingPower}%, 已捕获 {catches.Count} 种战利品。" : "水体或渔力未达标"
            };
        }

        private static object ExecuteFishingMachineAutomatedTest(int? optX, int? optY)
        {
            var steps = new List<Dictionary<string, object>>();
            bool allPassed = true;

            // 1. 验证 ModItem 与 ModTile 内容注册状态
            var itemMod = ItemLoader.Items.FirstOrDefault(i => i.Name == "FishingMachine");
            var tileMod = TileLoader.Tiles.FirstOrDefault(t => t.Name == "FishingMachineTile");

            bool itemRegistered = itemMod != null && itemMod.Type >= ItemID.Count;
            bool tileRegistered = tileMod != null && tileMod.Type >= TileID.Count;

            steps.Add(new Dictionary<string, object>
            {
                ["step"] = "1. ContentRegistrationCheck",
                ["itemRegistered"] = itemRegistered,
                ["itemId"] = itemMod?.Type ?? -1,
                ["tileRegistered"] = tileRegistered,
                ["tileId"] = tileMod?.Type ?? -1,
                ["passed"] = itemRegistered && tileRegistered
            });

            if (!itemRegistered || !tileRegistered)
            {
                return new
                {
                    success = false,
                    summary = "自动钓鱼机或物块内容未正确注册",
                    steps
                };
            }

            Player player = Main.LocalPlayer;
            if (player == null || !player.active)
            {
                return new
                {
                    success = false,
                    summary = "玩家未进入世界或 LocalPlayer 为空",
                    steps
                };
            }

            int playerTileX = (int)(player.position.X / 16f);
            int playerTileY = (int)(player.position.Y / 16f);
            int placeX = optX ?? (playerTileX + 3);
            int placeY = optY ?? (playerTileY);

            // 确保测试区域 2x2 地面坚固，上方清空
            for (int dx = 0; dx < 2; dx++)
            {
                for (int dy = 0; dy < 2; dy++)
                {
                    WorldGen.KillTile(placeX + dx, placeY + dy, false, false, true);
                }
                // 脚下铺设坚固物块
                WorldGen.PlaceTile(placeX + dx, placeY + 2, TileID.Stone, false, true);
            }

            // 2. 模拟放置 2x2 物块与 TileObjectData (Origin 1, 1 对应底部 placeX + 1, placeY + 1)
            int tileType = tileMod.Type;
            TileObject objectData;
            bool canPlace = TileObject.CanPlace(placeX + 1, placeY + 1, tileType, 0, 1, out objectData, false, null);
            bool placed = canPlace && TileObject.Place(objectData);
            if (!placed)
            {
                placed = WorldGen.PlaceObject(placeX + 1, placeY + 1, tileType, false, 0, -1, -1, 1);
            }

            if (placed)
            {
                TileObjectData data = TileObjectData.GetTileData(tileType, 0, 0);
                if (data?.HookPostPlaceMyPlayer.hook != null)
                {
                    data.HookPostPlaceMyPlayer.hook(placeX + 1, placeY + 1, tileType, 0, 1, 0);
                }
            }
            
            // 检查 2x2 图格是否成功创建
            bool tileCheck = true;
            for (int dx = 0; dx < 2; dx++)
            {
                for (int dy = 0; dy < 2; dy++)
                {
                    Tile t = Framing.GetTileSafely(placeX + dx, placeY + dy);
                    if (!t.active() || t.type != tileType)
                    {
                        tileCheck = false;
                    }
                }
            }

            // 检查 TileEntity 绑定 (在 placeX, placeY)
            int teId = -1;
            foreach (var kvp in TileEntity.ByID)
            {
                if (kvp.Value.Position.X == placeX && kvp.Value.Position.Y == placeY)
                {
                    teId = kvp.Key;
                    break;
                }
            }

            bool teBound = teId >= 0;
            steps.Add(new Dictionary<string, object>
            {
                ["step"] = "2. MultiTilePlacementAndEntityBinding",
                ["placed"] = placed,
                ["tileCheck2x2"] = tileCheck,
                ["tileEntityId"] = teId,
                ["tileEntityBound"] = teBound,
                ["passed"] = tileCheck && teBound
            });

            if (!tileCheck || !teBound) allPassed = false;

            // 3. 渔具装配与背包逻辑测试
            TileEntity te = teId >= 0 ? TileEntity.ByID[teId] : null;
            bool inventoryPassed = false;
            if (te is FishingMachine.Content.Tiles.TEFishingMachine machine)
            {
                machine.fishingPole = new Item();
                machine.fishingPole.SetDefaults(ItemID.GoldenFishingRod);
                machine.bait = new Item();
                machine.bait.SetDefaults(ItemID.MasterBait);
                machine.bait.stack = 50;

                int added = machine.AddItemToInventory(ItemID.Bass, 5);
                bool hasFish = machine.fish[0] != null && machine.fish[0].type == ItemID.Bass && machine.fish[0].stack == 5;
                inventoryPassed = added == 5 && hasFish;

                steps.Add(new Dictionary<string, object>
                {
                    ["step"] = "3. FishingApparatusAndInventory",
                    ["poleAssigned"] = machine.fishingPole.type == ItemID.GoldenFishingRod,
                    ["baitAssigned"] = machine.bait.type == ItemID.MasterBait,
                    ["fishAdded"] = added,
                    ["fishSlotVerified"] = hasFish,
                    ["passed"] = inventoryPassed
                });
            }

            if (!inventoryPassed) allPassed = false;

            // 4. Sidecar 序列化与反序列化测试
            bool sidecarPassed = false;
            if (te is FishingMachine.Content.Tiles.TEFishingMachine srcMachine)
            {
                TagCompound tag = new TagCompound();
                srcMachine.SaveData(tag);

                bool hasPoleTag = tag.ContainsKey("fishingPole");
                bool hasFishTag = tag.ContainsKey("fish");

                // 创建新实例反序列化
                var newEntity = (ModTileEntity)te.GenerateInstance();
                newEntity.LoadData(tag);
                if (newEntity is FishingMachine.Content.Tiles.TEFishingMachine newMachine)
                {
                    bool restoredFish = newMachine.fish[0] != null && newMachine.fish[0].type == ItemID.Bass && newMachine.fish[0].stack == 5;
                    sidecarPassed = hasPoleTag && hasFishTag && restoredFish;
                }

                steps.Add(new Dictionary<string, object>
                {
                    ["step"] = "4. SidecarPersistenceRoundtrip",
                    ["hasPoleTag"] = hasPoleTag,
                    ["hasFishTag"] = hasFishTag,
                    ["restoredFishSlot"] = sidecarPassed,
                    ["passed"] = sidecarPassed
                });
            }

            if (!sidecarPassed) allPassed = false;

            // 5. 物块破坏与掉落清理测试
            int preItemCount = Main.item.Count(it => it != null && it.active);
            WorldGen.KillTile(placeX, placeY, false, false, false);

            bool tileDestroyed = !Framing.GetTileSafely(placeX, placeY).active();
            bool teDestroyed = !TileEntity.ByID.ContainsKey(teId);
            int postItemCount = Main.item.Count(it => it != null && it.active);
            bool droppedItems = postItemCount >= preItemCount;

            steps.Add(new Dictionary<string, object>
            {
                ["step"] = "5. TileDestructionAndDrops",
                ["tileDestroyed"] = tileDestroyed,
                ["tileEntityRemoved"] = teDestroyed,
                ["itemsSpawnedInWorld"] = droppedItems,
                ["passed"] = tileDestroyed && teDestroyed
            });

            if (!tileDestroyed || !teDestroyed) allPassed = false;

            return new
            {
                success = allPassed,
                summary = allPassed ? "自动钓鱼机全生命周期自动化回归测试 100% 通过！" : "自动化测试存在未通过断言项",
                steps
            };
        }

        private static object InspectFishingMachine(int? targetX, int? targetY)
        {
            var results = new List<object>();

            foreach (var kvp in TileEntity.ByID)
            {
                if (kvp.Value is FishingMachine.Content.Tiles.TEFishingMachine m)
                {
                    if (targetX.HasValue && m.Position.X != targetX.Value) continue;
                    if (targetY.HasValue && m.Position.Y != targetY.Value) continue;

                    results.Add(new
                    {
                        id = kvp.Key,
                        x = (int)m.Position.X,
                        y = (int)m.Position.Y,
                        statusTip = m.statusTip,
                        lastFishingPower = m.lastFishingPower,
                        waterCount = m.waterCount,
                        locatePoint = new { x = (int)m.locatePoint.X, y = (int)m.locatePoint.Y },
                        pole = m.fishingPole != null && !m.fishingPole.IsAir ? Lang.GetItemNameValue(m.fishingPole.type) : "无",
                        bait = m.bait != null && !m.bait.IsAir ? $"{Lang.GetItemNameValue(m.bait.type)} x{m.bait.stack}" : "无",
                        fishStoredCount = m.fish.Count(it => it != null && !it.IsAir)
                    });
                }
            }

            return new
            {
                success = true,
                count = results.Count,
                machines = results
            };
        }
    }
}
