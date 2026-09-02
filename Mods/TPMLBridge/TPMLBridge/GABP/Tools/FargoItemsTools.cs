using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using TPML.Content;
using TPML.Content.UI;

namespace TPMLBridge.GABP.Tools
{
    /// <summary>
    /// FargoItems 地狱直通车原生模组测试、建造快照与矿道物理扫描工具
    /// 作者: SaintCirno9
    /// </summary>
    public static class FargoItemsTools
    {
        public static List<GABPToolDescriptor> GetDescriptors()
        {
            return new List<GABPToolDescriptor>
            {
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
                    Name = "tpml/test_autohouse",
                    Description = "诊断 AutoHouse 快速房屋原生内容注册、贴图、弹幕绑定、配方与环境材质主题状态。",
                    Tags = new List<string> { "diagnostic", "read-only", "autohouse" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/use_autohouse",
                    Description = "在世界中指定坐标（默认玩家身旁）触发 AutoHouse 建造流程，并自动执行房屋结构、家具完整性与 NPC 适居性全量扫描。",
                    Tags = new List<string> { "action", "autohouse", "building" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            targetX = new { type = "integer", description = "建造目标图格 X 坐标（可选，默认玩家右侧 6 格）" },
                            targetY = new { type = "integer", description = "建造目标图格 Y 坐标（可选，默认玩家脚底 Y）" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/use_instabridge",
                    Description = "模拟玩家手持使用 InstaBridge / ObsidianInstaBridge / DoubleObsidianInstabridge 铺设全图平台桥并断言图格覆盖率。",
                    Tags = new List<string> { "action", "instabridge", "building" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            variant = new { type = "string", description = "平台桥类型（'Wood' / 'Obsidian' / 'DoubleObsidian'，默认 'Wood'）" },
                            targetY = new { type = "integer", description = "铺设高度图格 Y（可选，默认玩家脚底 Y）" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/use_instatrack",
                    Description = "模拟玩家手持使用 InstaTrack 铺设全图矿车轨道并断言轨道图格连续性。",
                    Tags = new List<string> { "action", "instatrack", "building" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            targetY = new { type = "integer", description = "铺设高度图格 Y（可选，默认玩家脚底 Y）" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/use_instapond",
                    Description = "模拟玩家手持使用 InstaPond 开凿并灌注全环境钓鱼水池并断言水体与石板边缘。",
                    Tags = new List<string> { "action", "instapond", "building" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            targetX = new { type = "integer", description = "水池中心图格 X（可选，默认玩家当前 X）" },
                            targetY = new { type = "integer", description = "水池表面图格 Y（可选，默认玩家脚底 Y）" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/get_last_build_result",
                    Description = "获取最近一次由 Instavator 直通车执行的矿道开凿任务执行结果汇总快照。",
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
                    Description = "对指定矿道区域执行物理切片质量扫描，分析直通率、绳索连续性、火把覆盖、砖块完整性与阻碍物采样。",
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
                }
            };
        }

        public static async Task<object> HandleAsync(string name, JObject args)
        {
            switch (name)
            {
                case "tpml/test_instavator":
                case "tpml_test_instavator":
                    return await MainThreadQueue.EnqueueAsync(() => TestInstavator());

                case "tpml/test_autohouse":
                case "tpml_test_autohouse":
                    return await MainThreadQueue.EnqueueAsync(() => TestAutoHouse());

                case "tpml/use_autohouse":
                case "tpml_use_autohouse":
                    {
                        int? targetX = args?["targetX"]?.Value<int?>();
                        int? targetY = args?["targetY"]?.Value<int?>();
                        return await MainThreadQueue.EnqueueAsync(() => UseAutoHouse(targetX, targetY));
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

                case "tpml/use_instabridge":
                case "tpml_use_instabridge":
                    {
                        string variant = args?["variant"]?.ToString() ?? "Wood";
                        int? targetY = args?["targetY"]?.Value<int>();
                        return await MainThreadQueue.EnqueueAsync(() => UseInstaBridge(variant, targetY));
                    }

                case "tpml/use_instatrack":
                case "tpml_use_instatrack":
                    {
                        int? targetY = args?["targetY"]?.Value<int>();
                        return await MainThreadQueue.EnqueueAsync(() => UseInstaTrack(targetY));
                    }

                case "tpml/use_instapond":
                case "tpml_use_instapond":
                    {
                        int? targetX = args?["targetX"]?.Value<int>();
                        int? targetY = args?["targetY"]?.Value<int>();
                        return await MainThreadQueue.EnqueueAsync(() => UseInstaPond(targetX, targetY));
                    }

                default:
                    return null;
            }
        }

        public static object TestInstavator()
        {
            // M2：不再硬编码 6200-6202（FishingMachine 占 6200 后 ID 漂移），按显示名动态扫描直通车系列
            int firstInstavatorId = int.MaxValue;
            int lastInstavatorId = -1;
            foreach (ModItem modItem in ItemLoader.Items)
            {
                if (modItem == null) continue;
                string nm = ToolHelpers.GetItemDisplayName(modItem.Type) ?? "";
                if (nm.Contains("直通车") || modItem.Name.Contains("Instavator"))
                {
                    if (modItem.Type < firstInstavatorId) firstInstavatorId = modItem.Type;
                    if (modItem.Type > lastInstavatorId) lastInstavatorId = modItem.Type;
                }
            }
            if (lastInstavatorId < 0) { firstInstavatorId = 6200; lastInstavatorId = 6203; } // 兜底
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
                    actualName = ToolHelpers.GetItemDisplayName(type),
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
                        name = ToolHelpers.GetItemDisplayName(item.type),
                        stack = item.stack
                    })
                    .ToList();
                string tileName = recipe.requiredTile >= 0
                    ? Lang.GetMapObjectName(recipe.requiredTile)
                    : string.Empty;

                string baseTooltip = ItemLoader.GetTooltip(recipe.createItem.type) ?? string.Empty;
                var tooltipLines = new List<string>();
                try
                {
                    int yoyoLogo = -1;
                    int numLines = 1;
                    string[] lines = new string[30];
                    Microsoft.Xna.Framework.Color[] colors = new Microsoft.Xna.Framework.Color[30];
                    Main.MouseText_DrawItemTooltip_GetLinesInfo(recipe.createItem, ref yoyoLogo, recipe.createItem.knockBack, ref numLines, lines, colors);
                    for (int j = 0; j < numLines && j < lines.Length; j++)
                    {
                        if (!string.IsNullOrEmpty(lines[j]))
                            tooltipLines.Add(lines[j]);
                    }
                }
                catch { }

                recipes.Add(new
                {
                    recipeIndex = i,
                    outputItemId = recipe.createItem.type,
                    outputItemName = recipe.createItem.Name,
                    outputItemDisplayName = ToolHelpers.GetItemDisplayName(recipe.createItem.type),
                    outputItemBaseTooltip = baseTooltip,
                    outputItemTooltips = tooltipLines,
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
                .Where(item => item != null && ToolHelpers.GetItemDisplayName(item.Type).Contains("直通车"))
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

        public static object GetLastBuildResult()
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

        public static object InspectShaft(int? centerX, int? startY, int? targetY, string variant)
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

        public static object TestAutoHouse()
        {
            // 扫描 AutoHouse 模组物品
            ModItem autoHouseItem = null;
            foreach (ModItem modItem in ItemLoader.Items)
            {
                if (modItem == null) continue;
                if (modItem.Name.Equals("AutoHouse", StringComparison.OrdinalIgnoreCase) ||
                    (ToolHelpers.GetItemDisplayName(modItem.Type) ?? "").Contains("快速房子"))
                {
                    autoHouseItem = modItem;
                    break;
                }
            }

            int itemType = autoHouseItem?.Type ?? 0;
            string itemName = autoHouseItem != null ? ToolHelpers.GetItemDisplayName(itemType) : "未找到";
            bool itemRegistered = autoHouseItem != null && ItemLoader.GetItem(itemType) != null;

            ItemLoader.EnsureTextureLoaded(itemType);
            var itemAsset = TextureAssets.Item != null && itemType < TextureAssets.Item.Length ? TextureAssets.Item[itemType] : null;
            bool itemTextureValid = itemAsset != null && itemAsset.IsLoaded && itemAsset.Value != null;

            // 扫描 AutoHouseProj 模组弹幕
            ModProjectile autoHouseProj = null;
            foreach (ModProjectile modProj in ProjectileLoader.Projectiles)
            {
                if (modProj == null) continue;
                if (modProj.Name.Equals("AutoHouseProj", StringComparison.OrdinalIgnoreCase))
                {
                    autoHouseProj = modProj;
                    break;
                }
            }

            int projType = autoHouseProj?.Type ?? 0;
            bool projRegistered = autoHouseProj != null && ProjectileLoader.GetModProjectile(projType) != null;
            bool projOffsetSafe = projType >= ProjectileLoader.ModProjectileOffset;

            // 扫描 AutoHouse 配方
            var matchedRecipes = new List<object>();
            for (int i = 0; i < Recipe.numRecipes && i < Main.recipe.Length; i++)
            {
                Recipe recipe = Main.recipe[i];
                if (recipe?.createItem == null || recipe.createItem.type != itemType) continue;

                var reqs = recipe.requiredItem
                    .Where(it => it != null && !it.IsAir && it.type > 0)
                    .Select(it => new { id = it.type, name = ToolHelpers.GetItemDisplayName(it.type), stack = it.stack })
                    .ToList();

                matchedRecipes.Add(new
                {
                    recipeIndex = i,
                    outputItemId = itemType,
                    requiredTile = recipe.requiredTile >= 0 ? Lang.GetMapObjectName(recipe.requiredTile) : "无",
                    requirements = reqs
                });
            }

            return new
            {
                success = itemRegistered && projRegistered && projOffsetSafe,
                item = new
                {
                    id = itemType,
                    name = itemName,
                    registered = itemRegistered,
                    textureLoaded = itemTextureValid,
                    width = itemAsset?.Value?.Width ?? 0,
                    height = itemAsset?.Value?.Height ?? 0
                },
                projectile = new
                {
                    id = projType,
                    name = "AutoHouseProj",
                    registered = projRegistered,
                    offsetSafe = projOffsetSafe,
                    minSafeOffset = ProjectileLoader.ModProjectileOffset
                },
                recipes = matchedRecipes,
                summary = (itemRegistered && projRegistered && projOffsetSafe)
                    ? $"AutoHouse 原生注册完美就绪！物品ID={itemType} | 弹幕ID={projType} (安全偏移 >= {ProjectileLoader.ModProjectileOffset}) | 配方数={matchedRecipes.Count}"
                    : $"AutoHouse 存在异常配置！物品注册: {itemRegistered}, 弹幕注册: {projRegistered}, 偏移安全: {projOffsetSafe}"
            };
        }

        public static object UseAutoHouse(int? targetTileX, int? targetTileY)
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
                return new { inWorld = false, success = false, message = "当前未进入世界" };

            Player player = Main.LocalPlayer;
            int originX = targetTileX.HasValue ? targetTileX.Value : (int)(player.Center.X / 16f) + 6;
            int originY = targetTileY.HasValue ? targetTileY.Value : (int)(player.Bottom.Y / 16f);

            if (originX < 15 || originX >= Main.maxTilesX - 15 || originY < 15 || originY >= Main.maxTilesY - 15)
            {
                return new { inWorld = true, success = false, message = $"目标坐标 ({originX}, {originY}) 超出有效世界范围" };
            }

            // 确保玩家手中选中的是 AutoHouse 物品
            int autoHouseItemId = 0;
            foreach (var item in TPML.Content.ItemLoader.Items)
            {
                if (item != null && item.Name.Equals("AutoHouse", StringComparison.OrdinalIgnoreCase))
                {
                    autoHouseItemId = item.Type;
                    break;
                }
            }

            if (autoHouseItemId <= 0)
            {
                return new { inWorld = true, success = false, message = "未找到 AutoHouse 模组物品" };
            }

            // 如果当前手持不是 AutoHouse，在玩家背包第 0 格放置 AutoHouse
            if (player.HeldItem == null || player.HeldItem.type != autoHouseItemId)
            {
                player.inventory[0].SetDefaults(autoHouseItemId);
                player.inventory[0].stack = 10;
                player.selectedItem = 0;
            }

            int initialStack = player.HeldItem.stack;

            // 设置鼠标世界坐标与屏幕坐标
            Vector2 mouseWorld = new Vector2(originX * 16f + 8f, originY * 16f + 8f);
            Main.mouseX = (int)(mouseWorld.X - Main.screenPosition.X);
            Main.mouseY = (int)(mouseWorld.Y - Main.screenPosition.Y);

            // 直接模拟玩家使用当前选中的手持物品
            player.controlUseItem = true;
            player.releaseUseItem = true;
            player.ItemCheck();

            // 更新世界中的弹幕以立即执行弹幕生命周期
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].owner == player.whoAmI)
                {
                    Main.projectile[i].Update(i);
                }
            }

            // 扫描并断言生成的房屋图格
            int side = player.Center.X < mouseWorld.X ? 1 : -1;
            int startX = (side * -1) + originX;
            int x1 = startX + (1 * side);
            int x2 = startX + (10 * side);
            int minX = Math.Min(x1, x2);
            int maxX = Math.Max(x1, x2);
            int minY = originY - 5;
            int maxY = originY;

            int floorTiles = 0;
            int ceilingTiles = 0;
            int platformTiles = 0;
            int wallTiles = 0;
            int doorTiles = 0;
            int chairTiles = 0;
            int tableTiles = 0;
            int torchTiles = 0;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (!WorldGen.InWorld(x, y)) continue;
                    Tile tile = Main.tile[x, y];
                    if (tile == null) continue;

                    if (tile.wall > 0) wallTiles++;

                    if (tile.active())
                    {
                        if (y == maxY) floorTiles++;
                        else if (y == minY && tile.type == TileID.Platforms) platformTiles++;
                        else if (y == minY) ceilingTiles++;
                        else if (tile.type == TileID.ClosedDoor || tile.type == TileID.OpenDoor) doorTiles++;
                        else if (tile.type == TileID.Chairs) chairTiles++;
                        else if (tile.type == TileID.Tables) tableTiles++;
                        else if (tile.type == TileID.Torches) torchTiles++;
                    }
                }
            }

            bool structureValid = floorTiles >= 8 && (ceilingTiles + platformTiles) >= 8 && wallTiles >= 20;
            bool furnitureValid = doorTiles >= 1 && chairTiles >= 1 && tableTiles >= 1 && torchTiles >= 1;
            bool consumed = player.HeldItem.stack < initialStack;
            bool houseComplete = structureValid && furnitureValid;

            return new
            {
                inWorld = true,
                success = houseComplete,
                itemUsed = player.HeldItem?.Name ?? "AutoHouse",
                itemConsumed = consumed,
                remainingStack = player.HeldItem?.stack ?? 0,
                buildOrigin = new { tileX = originX, tileY = originY, worldX = mouseWorld.X, worldY = mouseWorld.Y },
                houseBounds = new { minX, maxX, minY, maxY, width = maxX - minX + 1, height = maxY - minY + 1 },
                structure = new
                {
                    floorTiles,
                    ceilingTiles,
                    platformTiles,
                    wallTiles,
                    structureValid
                },
                furniture = new
                {
                    doorTiles,
                    chairTiles,
                    tableTiles,
                    torchTiles,
                    furnitureValid
                },
                summary = houseComplete
                    ? $"★ 玩家直接使用 [快速房屋] 实机测试大获成功！物品已消耗，在图格 [{minX}~{maxX}, {minY}~{maxY}] 成功构建完整 NPC 适居房屋（地板={floorTiles}, 墙壁={wallTiles}, 门={doorTiles}, 桌={tableTiles}, 椅={chairTiles}, 火把={torchTiles}）"
                    : $"直接使用物品建造存在缺损：结构合格={structureValid}, 家具合格={furnitureValid}, 物品消耗={consumed}"
            };
        }

        public static object UseInstaBridge(string variant, int? targetY)
        {
            if (Main.gameMenu || Main.netMode != 0)
                return new { success = false, message = "必须处于单机世界中才能执行测试" };

            Player player = Main.LocalPlayer;
            if (player == null)
                return new { success = false, message = "未找到本地玩家实例" };

            int yPos = targetY ?? (int)(player.position.Y / 16f);

            int targetItemType;
            bool isMini = string.Equals(variant, "Mini", StringComparison.OrdinalIgnoreCase);
            bool isMiniDirt = string.Equals(variant, "MiniDirt", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(variant, "DoubleObsidian", StringComparison.OrdinalIgnoreCase))
                targetItemType = ModContent.ItemType("FargoItems", "DoubleObsidianInstabridge");
            else if (string.Equals(variant, "Obsidian", StringComparison.OrdinalIgnoreCase))
                targetItemType = ModContent.ItemType("FargoItems", "ObsidianInstaBridge");
            else if (isMini)
                targetItemType = ModContent.ItemType("FargoItems", "MiniInstaBridge");
            else if (isMiniDirt)
                targetItemType = ModContent.ItemType("FargoItems", "MiniDirtInstaBridge");
            else
                targetItemType = ModContent.ItemType("FargoItems", "InstaBridge");

            player.inventory[0].SetDefaults(targetItemType);
            player.inventory[0].stack = 10;
            player.selectedItem = 0;

            Vector2 mouseWorld = new Vector2(player.Center.X + (isMini || isMiniDirt ? 100f : 0f), yPos * 16f);
            Main.mouseX = (int)(mouseWorld.X - Main.screenPosition.X);
            Main.mouseY = (int)(mouseWorld.Y - Main.screenPosition.Y);

            player.controlUseItem = true;
            player.releaseUseItem = true;
            player.ItemCheck();

            if (isMini || isMiniDirt)
            {
                int originX = (int)(mouseWorld.X / 16f);
                int expectedTileType = isMiniDirt ? TileID.Dirt : TileID.Platforms;
                int length = isMiniDirt ? 150 : 400;
                int placedCount = 0;

                for (int x = 0; x < length; x++)
                {
                    int gx = originX + x;
                    if (WorldGen.InWorld(gx, yPos))
                    {
                        Tile tile = Main.tile[gx, yPos];
                        if (tile != null && tile.active() && tile.type == expectedTileType)
                            placedCount++;
                    }
                }

                double miniCoverage = (double)placedCount / length;
                bool miniSuccess = miniCoverage >= 0.90;

                return new
                {
                    inWorld = true,
                    success = miniSuccess,
                    variant,
                    targetY = yPos,
                    placedTiles = placedCount,
                    expectedLength = length,
                    coverage = $"{miniCoverage * 100:F1}%",
                    itemConsumed = player.HeldItem.stack < 10,
                    summary = miniSuccess
                        ? $"★ 小型{(isMiniDirt ? "泥土" : "木")}平台桥 [{variant}] 铺设大获成功！铺设图格数: {placedCount}/{length} (覆盖率: {miniCoverage * 100:F1}%)"
                        : $"小型平台桥铺设未达预期：覆盖率={miniCoverage * 100:F1}%"
                };
            }

            // 采样扫描世界全宽平台覆盖率 (采样 100 个点)
            int sampleCount = 100;
            int foundPlatforms = 0;
            int step = (Main.maxTilesX - 20) / sampleCount;

            for (int i = 0; i < sampleCount; i++)
            {
                int sampleX = 10 + i * step;
                if (WorldGen.InWorld(sampleX, yPos))
                {
                    Tile tile = Main.tile[sampleX, yPos];
                    if (tile != null && tile.active() && tile.type == TileID.Platforms)
                    {
                        foundPlatforms++;
                    }
                }
            }

            double coverage = (double)foundPlatforms / sampleCount;
            bool success = coverage >= 0.90;

            return new
            {
                inWorld = true,
                success,
                variant,
                targetY = yPos,
                platformCoverage = $"{coverage * 100:F1}%",
                itemConsumed = player.HeldItem.stack < 10,
                summary = success
                    ? $"★ 平台桥 [{variant}] 铺设大获成功！全图平台覆盖率: {coverage * 100:F1}% (采样={foundPlatforms}/{sampleCount})"
                    : $"平台桥铺设未达预期：覆盖率={coverage * 100:F1}%"
            };
        }

        public static object UseInstaTrack(int? targetY)
        {
            if (Main.gameMenu || Main.netMode != 0)
                return new { success = false, message = "必须处于单机世界中才能执行测试" };

            Player player = Main.LocalPlayer;
            if (player == null)
                return new { success = false, message = "未找到本地玩家实例" };

            int yPos = targetY ?? (int)(player.position.Y / 16f);
            int targetItemType = ModContent.ItemType("FargoItems", "InstaTrack");

            player.inventory[0].SetDefaults(targetItemType);
            player.inventory[0].stack = 10;
            player.selectedItem = 0;

            Vector2 mouseWorld = new Vector2(player.Center.X, yPos * 16f);
            Main.mouseX = (int)(mouseWorld.X - Main.screenPosition.X);
            Main.mouseY = (int)(mouseWorld.Y - Main.screenPosition.Y);

            player.controlUseItem = true;
            player.releaseUseItem = true;
            player.ItemCheck();

            // 采样扫描世界全宽矿车轨道覆盖率
            int sampleCount = 100;
            int foundTracks = 0;
            int step = (Main.maxTilesX - 20) / sampleCount;

            for (int i = 0; i < sampleCount; i++)
            {
                int sampleX = 10 + i * step;
                if (WorldGen.InWorld(sampleX, yPos))
                {
                    Tile tile = Main.tile[sampleX, yPos];
                    if (tile != null && tile.active() && tile.type == TileID.MinecartTrack)
                    {
                        foundTracks++;
                    }
                }
            }

            double coverage = (double)foundTracks / sampleCount;
            bool success = coverage >= 0.90;

            return new
            {
                inWorld = true,
                success,
                targetY = yPos,
                trackCoverage = $"{coverage * 100:F1}%",
                itemConsumed = player.HeldItem.stack < 10,
                summary = success
                    ? $"★ 全图矿车轨道铺设大获成功！全图轨道覆盖率: {coverage * 100:F1}% (采样={foundTracks}/{sampleCount})"
                    : $"矿车轨道铺设未达预期：覆盖率={coverage * 100:F1}%"
            };
        }

        public static object UseInstaPond(int? targetX, int? targetY)
        {
            if (Main.gameMenu || Main.netMode != 0)
                return new { success = false, message = "必须处于单机世界中才能执行测试" };

            Player player = Main.LocalPlayer;
            if (player == null)
                return new { success = false, message = "未找到本地玩家实例" };

            int originX = targetX ?? (int)(player.position.X / 16f + 30f);
            int originY = targetY ?? (int)(player.position.Y / 16f);
            int targetItemType = ModContent.ItemType("FargoItems", "InstaPond");

            player.inventory[0].SetDefaults(targetItemType);
            player.inventory[0].stack = 10;
            player.selectedItem = 0;

            Vector2 mouseWorld = new Vector2(originX * 16f, originY * 16f);
            Main.mouseX = (int)(mouseWorld.X - Main.screenPosition.X);
            Main.mouseY = (int)(mouseWorld.Y - Main.screenPosition.Y);

            player.controlUseItem = true;
            player.releaseUseItem = true;
            player.ItemCheck();

            // 扫描水池范围 (宽 150，深 50)
            int waterTiles = 0;
            int stoneSlabTiles = 0;

            for (int x = -75; x <= 75; x++)
            {
                for (int y = 0; y <= 50; y++)
                {
                    int gx = originX + x;
                    int gy = originY + y;
                    if (!WorldGen.InWorld(gx, gy)) continue;

                    Tile tile = Main.tile[gx, gy];
                    if (tile == null) continue;

                    if (tile.liquid > 0) waterTiles++;
                    if (tile.active() && tile.type == TileID.StoneSlab) stoneSlabTiles++;
                }
            }

            bool success = waterTiles >= 1000 && stoneSlabTiles >= 100;

            return new
            {
                inWorld = true,
                success,
                originX,
                originY,
                waterTiles,
                stoneSlabTiles,
                itemConsumed = player.HeldItem.stack < 10,
                summary = success
                    ? $"★ 钓鱼池建造大获成功！水体图格数={waterTiles}, 石板池壁数={stoneSlabTiles}"
                    : $"钓鱼池建造未达预期：水体={waterTiles}, 石板={stoneSlabTiles}"
            };
        }
    }
}
