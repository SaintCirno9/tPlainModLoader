using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    /// Instavator 地狱直通车原生模组测试、建造快照与矿道物理扫描工具
    /// 作者: SaintCirno9
    /// </summary>
    public static class InstavatorTools
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

                default:
                    return null;
            }
        }

        public static object TestInstavator()
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
    }
}
