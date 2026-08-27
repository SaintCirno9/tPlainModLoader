using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OptimizeAndTool.Content.Cheat.Function2;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using tContentPatch;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;

namespace OptimizeAndTool.Content.Cheat.QoL
{
    public class StructurePin
    {
        public Vector2 PositionInTiles;
        public string Name;
        public Color Color;
        public string Category; // "Plantera", "SwordShrine", "Larva", "Temple", "Shimmer", "Pyramid", "FloatingIsland", "LivingTree", "Dungeon", "Underworld", "MiniBiomes"
        public string CategoryLabel;
        public int ItemId;
        public Func<bool> CheckActive;
    }

    /// <summary>
    /// 关键结构小地图/全图标记系统（11+ 类世界核心遗迹、微群落与生成物）
    /// 作者: SaintCirno9
    /// </summary>
    public class StructureMarker : PatchMain
    {
        private static readonly object _pinsLock = new object();
        private static List<StructurePin> _pins = new List<StructurePin>();
        private static StructurePin[] _pinsSnapshot = Array.Empty<StructurePin>();
        private static volatile bool _isScanning = false;
        private static int _cleanTick = 0;

        public override void OnEnterWorld()
        {
            TriggerRescan();
        }

        public static void TriggerRescan()
        {
            if (_isScanning) return;

            _ = Task.Run(() =>
            {
                try
                {
                    _isScanning = true;
                    ScanWorldStructures();
                }
                catch (Exception ex)
                {
                    Main.NewText($"[结构标记] 扫描失败: {ex.Message}");
                }
                finally
                {
                    _isScanning = false;
                }
            });
        }

        private class GridCell
        {
            public int MarbleCount;
            public long MarbleSumX, MarbleSumY;

            public int GraniteCount;
            public long GraniteSumX, GraniteSumY;

            public int SpiderCount;
            public long SpiderSumX, SpiderSumY;

            public int SkyLakeCount;
            public long SkyLakeSumX, SkyLakeSumY;

            public int PyramidCount;
            public long PyramidSumX, PyramidSumY;

            public int ShimmerCount;
            public long ShimmerSumX, ShimmerSumY;
        }

        private static bool IsTileActiveAndType(Vector2 pos, params ushort[] types)
        {
            int x = (int)pos.X;
            int y = (int)pos.Y;
            if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY) return false;
            Tile t = Main.tile[x, y];
            if (!t.active()) return false;
            for (int i = 0; i < types.Length; i++)
            {
                if (t.type == types[i]) return true;
            }
            return false;
        }

        private static void ScanWorldStructures()
        {
            List<StructurePin> list = new List<StructurePin>();

            int maxX = Main.maxTilesX;
            int maxY = Main.maxTilesY;
            if (Main.tile == null || maxX <= 0 || maxY <= 0) return;

            // 1. 地牢入口探测
            if (Main.dungeonX > 0 && Main.dungeonY > 0)
            {
                list.Add(new StructurePin
                {
                    PositionInTiles = new Vector2(Main.dungeonX, Main.dungeonY),
                    Name = "地牢: 地表主入口",
                    Color = Color.DeepSkyBlue,
                    Category = "Dungeon",
                    CategoryLabel = "地牢与神器宝藏",
                    ItemId = ItemID.BoneKey
                });
            }

            // 2. 世界宝箱扫描 (地牢环境神器宝箱、天域箱、生命木箱、蛛网箱、地狱暗影箱)
            if (Main.chest != null)
            {
                for (int i = 0; i < Main.chest.Length; i++)
                {
                    Chest c = Main.chest[i];
                    if (c == null || c.x <= 0 || c.x >= maxX || c.y <= 0 || c.y >= maxY) continue;

                    Tile t = Main.tile[c.x, c.y];
                    if (!t.active()) continue;

                    Vector2 chestPos = new Vector2(c.x, c.y);

                    if (t.type == TileID.Containers)
                    {
                        int style = t.frameX / 36;
                        switch (style)
                        {
                            case 13: // 天域宝箱 (空岛)
                                list.Add(new StructurePin
                                {
                                    PositionInTiles = chestPos,
                                    Name = "空岛: 天域建筑",
                                    Color = Color.SkyBlue,
                                    Category = "FloatingIsland",
                                    CategoryLabel = "高空浮岛遗迹",
                                    ItemId = ItemID.SkywareChest,
                                    CheckActive = () => IsTileActiveAndType(chestPos, TileID.Containers, TileID.Containers2)
                                });
                                break;
                            case 12: // 生命木宝箱 (巨型生命树)
                                list.Add(new StructurePin
                                {
                                    PositionInTiles = chestPos,
                                    Name = "巨型生命树: 树根宝箱",
                                    Color = Color.LawnGreen,
                                    Category = "LivingTree",
                                    CategoryLabel = "地表巨型植被",
                                    ItemId = ItemID.LivingWoodChest,
                                    CheckActive = () => IsTileActiveAndType(chestPos, TileID.Containers, TileID.Containers2)
                                });
                                break;
                            case 15: // 蛛网宝箱 (蛛巢)
                                list.Add(new StructurePin
                                {
                                    PositionInTiles = chestPos,
                                    Name = "地下蛛巢: 蛛网宝箱",
                                    Color = Color.SlateGray,
                                    Category = "MiniBiomes",
                                    CategoryLabel = "地下微群落",
                                    ItemId = ItemID.WebCoveredChest,
                                    CheckActive = () => IsTileActiveAndType(chestPos, TileID.Containers, TileID.Containers2)
                                });
                                break;
                            case 3: // 暗影箱 (地狱)
                            case 4: // 锁住的暗影箱 (地狱)
                                if (c.y > Main.maxTilesY - 350)
                                {
                                    list.Add(new StructurePin
                                    {
                                        PositionInTiles = chestPos,
                                        Name = style == 4 ? "地狱废墟: 锁住的暗影箱" : "地狱废墟: 暗影箱",
                                        Color = Color.DarkOrchid,
                                        Category = "Underworld",
                                        CategoryLabel = "地狱暗影宝藏",
                                        ItemId = ItemID.ShadowChest,
                                        CheckActive = () => IsTileActiveAndType(chestPos, TileID.Containers, TileID.Containers2)
                                    });
                                }
                                break;
                            case 23: // 丛林神器宝箱
                                list.Add(new StructurePin
                                {
                                    PositionInTiles = chestPos,
                                    Name = "地牢: 丛林神器宝箱",
                                    Color = Color.LimeGreen,
                                    Category = "Dungeon",
                                    CategoryLabel = "地牢与神器宝藏",
                                    ItemId = ItemID.JungleKey,
                                    CheckActive = () => IsTileActiveAndType(chestPos, TileID.Containers, TileID.Containers2)
                                });
                                break;
                            case 24: // 腐化神器宝箱
                                list.Add(new StructurePin
                                {
                                    PositionInTiles = chestPos,
                                    Name = "地牢: 腐化神器宝箱",
                                    Color = Color.MediumPurple,
                                    Category = "Dungeon",
                                    CategoryLabel = "地牢与神器宝藏",
                                    ItemId = ItemID.CorruptionKey,
                                    CheckActive = () => IsTileActiveAndType(chestPos, TileID.Containers, TileID.Containers2)
                                });
                                break;
                            case 25: // 猩红神器宝箱
                                list.Add(new StructurePin
                                {
                                    PositionInTiles = chestPos,
                                    Name = "地牢: 猩红神器宝箱",
                                    Color = Color.Crimson,
                                    Category = "Dungeon",
                                    CategoryLabel = "地牢与神器宝藏",
                                    ItemId = ItemID.CrimsonKey,
                                    CheckActive = () => IsTileActiveAndType(chestPos, TileID.Containers, TileID.Containers2)
                                });
                                break;
                            case 26: // 神圣神器宝箱
                                list.Add(new StructurePin
                                {
                                    PositionInTiles = chestPos,
                                    Name = "地牢: 神圣神器宝箱",
                                    Color = Color.HotPink,
                                    Category = "Dungeon",
                                    CategoryLabel = "地牢与神器宝藏",
                                    ItemId = ItemID.HallowedKey,
                                    CheckActive = () => IsTileActiveAndType(chestPos, TileID.Containers, TileID.Containers2)
                                });
                                break;
                            case 27: // 冰霜神器宝箱
                                list.Add(new StructurePin
                                {
                                    PositionInTiles = chestPos,
                                    Name = "地牢: 冰霜神器宝箱",
                                    Color = Color.LightCyan,
                                    Category = "Dungeon",
                                    CategoryLabel = "地牢与神器宝藏",
                                    ItemId = ItemID.FrozenKey,
                                    CheckActive = () => IsTileActiveAndType(chestPos, TileID.Containers, TileID.Containers2)
                                });
                                break;
                        }
                    }
                    else if (t.type == TileID.Containers2)
                    {
                        int style2 = t.frameX / 36;
                        if (style2 == 0) // 沙漠神器宝箱
                        {
                            list.Add(new StructurePin
                            {
                                PositionInTiles = chestPos,
                                Name = "地牢: 沙漠神器宝箱",
                                Color = Color.Goldenrod,
                                Category = "Dungeon",
                                CategoryLabel = "地牢与神器宝藏",
                                ItemId = 4714, // 沙漠钥匙
                                CheckActive = () => IsTileActiveAndType(chestPos, TileID.Containers, TileID.Containers2)
                            });
                        }
                    }
                }
            }

            // 3. 空间统计网格初始化 (CellSize = 30)
            const int cellSize = 30;
            int gridW = (maxX + cellSize - 1) / cellSize;
            int gridH = (maxY + cellSize - 1) / cellSize;
            GridCell[] grid = new GridCell[gridW * gridH];

            // 4. 单次遍历全图图格进行精确匹配与空间聚类统计
            for (int x = 10; x < maxX - 10; x++)
            {
                int gx = x / cellSize;
                for (int y = 10; y < maxY - 10; y++)
                {
                    Tile tile = Main.tile[x, y];
                    int gy = y / cellSize;
                    int gIndex = gy * gridW + gx;

                    // 4.1 世纪之花花苞
                    if (tile.active() && tile.type == TileID.PlanteraBulb)
                    {
                        if (tile.frameX == 0 && tile.frameY == 0)
                        {
                            Vector2 pos = new Vector2(x, y);
                            list.Add(new StructurePin
                            {
                                PositionInTiles = pos,
                                Name = "世纪之花花苞",
                                Color = Color.Magenta,
                                Category = "Plantera",
                                CategoryLabel = "关键首领生成物",
                                ItemId = ItemID.PlanteraBossBag,
                                CheckActive = () => IsTileActiveAndType(pos, TileID.PlanteraBulb)
                            });
                        }
                    }

                    // 4.2 附魔剑冢
                    if (tile.active() && tile.type == TileID.LargePiles2)
                    {
                        if (tile.frameX >= 17 * 18 && tile.frameX <= 19 * 18 && tile.frameY == 0)
                        {
                            Vector2 pos = new Vector2(x, y);
                            list.Add(new StructurePin
                            {
                                PositionInTiles = pos,
                                Name = "附魔剑冢",
                                Color = Color.Cyan,
                                Category = "SwordShrine",
                                CategoryLabel = "珍贵自然生成物",
                                ItemId = ItemID.EnchantedSword,
                                CheckActive = () => IsTileActiveAndType(pos, TileID.LargePiles2)
                            });
                        }
                    }

                    // 4.3 蜂巢幼虫
                    if (tile.active() && tile.type == TileID.Larva)
                    {
                        if (tile.frameX == 0 && tile.frameY == 0)
                        {
                            Vector2 pos = new Vector2(x, y);
                            list.Add(new StructurePin
                            {
                                PositionInTiles = pos,
                                Name = "蜂巢幼虫",
                                Color = Color.Gold,
                                Category = "Larva",
                                CategoryLabel = "首领生成物",
                                ItemId = ItemID.Abeemination,
                                CheckActive = () => IsTileActiveAndType(pos, TileID.Larva)
                            });
                        }
                    }

                    // 4.4 丛林神庙祭坛
                    if (tile.active() && tile.type == TileID.LihzahrdAltar)
                    {
                        if (tile.frameX == 0 && tile.frameY == 0)
                        {
                            Vector2 pos = new Vector2(x, y);
                            list.Add(new StructurePin
                            {
                                PositionInTiles = pos,
                                Name = "丛林神庙石巨人祭坛",
                                Color = Color.OrangeRed,
                                Category = "Temple",
                                CategoryLabel = "核心遗迹祭坛",
                                ItemId = ItemID.LihzahrdPowerCell,
                                CheckActive = () => IsTileActiveAndType(pos, TileID.LihzahrdAltar)
                            });
                        }
                    }

                    // 4.5 微光液体统计
                    if (tile.liquidType() == LiquidID.Shimmer && tile.liquid > 150)
                    {
                        if (grid[gIndex] == null) grid[gIndex] = new GridCell();
                        grid[gIndex].ShimmerCount++;
                        grid[gIndex].ShimmerSumX += x;
                        grid[gIndex].ShimmerSumY += y;
                    }

                    // 4.6 沙漠金字塔砖块 (放宽至岩石层以上，兼容深埋金字塔)
                    if (tile.active() && tile.type == TileID.SandstoneBrick && y < Main.rockLayer)
                    {
                        if (grid[gIndex] == null) grid[gIndex] = new GridCell();
                        grid[gIndex].PyramidCount++;
                        grid[gIndex].PyramidSumX += x;
                        grid[gIndex].PyramidSumY += y;
                    }

                    // 4.7 高空天湖水池统计 (高空且包含大量水)
                    if (y < Main.worldSurface * 0.38f && tile.liquid > 180 && tile.liquidType() == LiquidID.Water)
                    {
                        if (grid[gIndex] == null) grid[gIndex] = new GridCell();
                        grid[gIndex].SkyLakeCount++;
                        grid[gIndex].SkyLakeSumX += x;
                        grid[gIndex].SkyLakeSumY += y;
                    }

                    // 4.8 地下大理石群落
                    if (tile.active() && (tile.type == TileID.Marble || tile.type == TileID.MarbleBlock) && y > Main.worldSurface)
                    {
                        if (grid[gIndex] == null) grid[gIndex] = new GridCell();
                        grid[gIndex].MarbleCount++;
                        grid[gIndex].MarbleSumX += x;
                        grid[gIndex].MarbleSumY += y;
                    }

                    // 4.9 地下花岗岩群落
                    if (tile.active() && (tile.type == TileID.Granite || tile.type == TileID.GraniteBlock) && y > Main.worldSurface)
                    {
                        if (grid[gIndex] == null) grid[gIndex] = new GridCell();
                        grid[gIndex].GraniteCount++;
                        grid[gIndex].GraniteSumX += x;
                        grid[gIndex].GraniteSumY += y;
                    }

                    // 4.10 地下蛛巢墙体
                    if (tile.wall == WallID.SpiderUnsafe && y > Main.worldSurface)
                    {
                        if (grid[gIndex] == null) grid[gIndex] = new GridCell();
                        grid[gIndex].SpiderCount++;
                        grid[gIndex].SpiderSumX += x;
                        grid[gIndex].SpiderSumY += y;
                    }
                }
            }

            // 5. 空间连通区域聚类与质心提取
            ClusterFeature(grid, gridW, gridH,
                cell => cell?.ShimmerCount ?? 0,
                cell => cell.ShimmerSumX,
                cell => cell.ShimmerSumY,
                minCellCount: 5, minTotalCount: 20,
                (centroid, count) =>
                {
                    list.Add(new StructurePin
                    {
                        PositionInTiles = centroid,
                        Name = "以太微光湖",
                        Color = Color.MediumPurple,
                        Category = "Shimmer",
                        CategoryLabel = "特殊液体群落",
                        ItemId = ItemID.BottomlessShimmerBucket
                    });
                });

            ClusterFeature(grid, gridW, gridH,
                cell => cell?.PyramidCount ?? 0,
                cell => cell.PyramidSumX,
                cell => cell.PyramidSumY,
                minCellCount: 15, minTotalCount: 80,
                (centroid, count) =>
                {
                    list.Add(new StructurePin
                    {
                        PositionInTiles = centroid,
                        Name = "沙漠金字塔",
                        Color = Color.SandyBrown,
                        Category = "Pyramid",
                        CategoryLabel = "地表沙漠遗迹",
                        ItemId = ItemID.FlyingCarpet
                    });
                });

            ClusterFeature(grid, gridW, gridH,
                cell => cell?.SkyLakeCount ?? 0,
                cell => cell.SkyLakeSumX,
                cell => cell.SkyLakeSumY,
                minCellCount: 6, minTotalCount: 30,
                (centroid, count) =>
                {
                    // 避免与已有天域宝箱重复标记
                    if (!HasNearbyPin(list, centroid, 80f, "FloatingIsland"))
                    {
                        list.Add(new StructurePin
                        {
                            PositionInTiles = centroid,
                            Name = "空岛: 高空天湖",
                            Color = Color.DeepSkyBlue,
                            Category = "FloatingIsland",
                            CategoryLabel = "高空浮岛遗迹",
                            ItemId = ItemID.BottomlessBucket
                        });
                    }
                });

            ClusterFeature(grid, gridW, gridH,
                cell => cell?.MarbleCount ?? 0,
                cell => cell.MarbleSumX,
                cell => cell.MarbleSumY,
                minCellCount: 8, minTotalCount: 70,
                (centroid, count) =>
                {
                    list.Add(new StructurePin
                    {
                        PositionInTiles = centroid,
                        Name = "地下大理石洞",
                        Color = Color.GhostWhite,
                        Category = "MiniBiomes",
                        CategoryLabel = "地下微群落",
                        ItemId = ItemID.MarbleBlock
                    });
                });

            ClusterFeature(grid, gridW, gridH,
                cell => cell?.GraniteCount ?? 0,
                cell => cell.GraniteSumX,
                cell => cell.GraniteSumY,
                minCellCount: 8, minTotalCount: 70,
                (centroid, count) =>
                {
                    list.Add(new StructurePin
                    {
                        PositionInTiles = centroid,
                        Name = "地下花岗岩洞",
                        Color = Color.CornflowerBlue,
                        Category = "MiniBiomes",
                        CategoryLabel = "地下微群落",
                        ItemId = ItemID.GraniteBlock
                    });
                });

            ClusterFeature(grid, gridW, gridH,
                cell => cell?.SpiderCount ?? 0,
                cell => cell.SpiderSumX,
                cell => cell.SpiderSumY,
                minCellCount: 10, minTotalCount: 60,
                (centroid, count) =>
                {
                    if (!HasNearbyPin(list, centroid, 60f, "MiniBiomes"))
                    {
                        list.Add(new StructurePin
                        {
                            PositionInTiles = centroid,
                            Name = "地下蛛巢",
                            Color = Color.SlateGray,
                            Category = "MiniBiomes",
                            CategoryLabel = "地下微群落",
                            ItemId = ItemID.Cobweb
                        });
                    }
                });

            lock (_pinsLock)
            {
                _pins = list;
                _pinsSnapshot = list.ToArray();
            }

            Main.NewText($"[结构标记] 扫描完成，共索引 {_pinsSnapshot.Length} 处关键世界结构！");
        }

        private static bool HasNearbyPin(List<StructurePin> list, Vector2 pos, float maxDistance, string category)
        {
            float maxDistSq = maxDistance * maxDistance;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Category == category && Vector2.DistanceSquared(list[i].PositionInTiles, pos) <= maxDistSq)
                {
                    return true;
                }
            }
            return false;
        }

        private static void ClusterFeature(GridCell[] grid, int gridW, int gridH,
            Func<GridCell, int> getCount, Func<GridCell, long> getSumX, Func<GridCell, long> getSumY,
            int minCellCount, int minTotalCount, Action<Vector2, int> onClusterFound)
        {
            bool[] visited = new bool[gridW * gridH];
            Queue<Point> queue = new Queue<Point>();

            for (int y = 0; y < gridH; y++)
            {
                for (int x = 0; x < gridW; x++)
                {
                    int index = y * gridW + x;
                    if (visited[index]) continue;

                    GridCell c = grid[index];
                    int count = getCount(c);
                    if (count < minCellCount) continue;

                    // 开始 BFS 洪泛搜索相邻网格
                    int totalCount = 0;
                    long totalSumX = 0;
                    long totalSumY = 0;

                    visited[index] = true;
                    queue.Enqueue(new Point(x, y));

                    while (queue.Count > 0)
                    {
                        Point p = queue.Dequeue();
                        int currIndex = p.Y * gridW + p.X;
                        GridCell curr = grid[currIndex];

                        int cellCount = getCount(curr);
                        totalCount += cellCount;
                        totalSumX += getSumX(curr);
                        totalSumY += getSumY(curr);

                        // 8 邻域扩散
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                if (dx == 0 && dy == 0) continue;
                                int nx = p.X + dx;
                                int ny = p.Y + dy;
                                if (nx < 0 || nx >= gridW || ny < 0 || ny >= gridH) continue;

                                int nIndex = ny * gridW + nx;
                                if (visited[nIndex]) continue;

                                GridCell nextCell = grid[nIndex];
                                if (getCount(nextCell) >= minCellCount)
                                {
                                    visited[nIndex] = true;
                                    queue.Enqueue(new Point(nx, ny));
                                }
                            }
                        }
                    }

                    if (totalCount >= minTotalCount)
                    {
                        Vector2 centroid = new Vector2((float)totalSumX / totalCount, (float)totalSumY / totalCount);
                        onClusterFound(centroid, totalCount);
                    }
                }
            }
        }

        public override void DrawMapPostfix(GameTime gameTime)
        {
            if (!QoLValSet.markStructuresOnMap.val) return;
            if (!Main.mapEnabled || !Main.mapReady) return;

            // 动态生命周期清理：每隔 60 帧对标记存活状态进行一次检查
            _cleanTick++;
            if (_cleanTick % 60 == 0)
            {
                lock (_pinsLock)
                {
                    int removed = _pins.RemoveAll(p => p.CheckActive != null && !p.CheckActive());
                    if (removed > 0)
                    {
                        _pinsSnapshot = _pins.ToArray();
                    }
                }
            }

            // 零分配读取快照
            StructurePin[] currentPins = _pinsSnapshot;
            if (currentPins == null || currentPins.Length == 0) return;

            string hoveredTooltip = null;

            for (int i = 0; i < currentPins.Length; i++)
            {
                StructurePin pin = currentPins[i];
                if (pin == null) continue;

                // 分类开关过滤
                if (pin.Category == "Plantera" && !QoLValSet.markPlanteraBulb.val) continue;
                if (pin.Category == "SwordShrine" && !QoLValSet.markSwordShrine.val) continue;
                if (pin.Category == "Larva" && !QoLValSet.markBeeHive.val) continue;
                if (pin.Category == "Temple" && !QoLValSet.markTempleAltar.val) continue;
                if (pin.Category == "Shimmer" && !QoLValSet.markShimmer.val) continue;
                if (pin.Category == "Pyramid" && !QoLValSet.markPyramid.val) continue;
                if (pin.Category == "FloatingIsland" && !QoLValSet.markFloatingIsland.val) continue;
                if (pin.Category == "LivingTree" && !QoLValSet.markLivingTree.val) continue;
                if (pin.Category == "Dungeon" && !QoLValSet.markDungeon.val) continue;
                if (pin.Category == "Underworld" && !QoLValSet.markUnderworld.val) continue;
                if (pin.Category == "MiniBiomes" && !QoLValSet.markMiniBiomes.val) continue;

                // 1. 全屏大地图
                if (Main.mapFullscreen)
                {
                    Vector2 centerPos = new Vector2(Main.screenWidth, Main.screenHeight) / 2f;
                    Vector2 drawPos = centerPos - Main.mapFullscreenPos * Main.mapFullscreenScale;
                    drawPos += pin.PositionInTiles * Main.mapFullscreenScale;

                    bool isHovered = IsMouseHovering(drawPos, 14f * Main.UIScale);
                    DrawPinMarker(drawPos, pin, 1.25f, isHovered);

                    if (isHovered)
                    {
                        hoveredTooltip = $"[c/FFE45E:{pin.Name}]\n类型: {pin.CategoryLabel}\n坐标: [X: {(int)pin.PositionInTiles.X}, Y: {(int)pin.PositionInTiles.Y}]";
                    }
                }
                // 2. 右上角小地图
                else if (Main.mapStyle == 1)
                {
                    float scale = (Main.mapMinimapScale * 0.25f * 2f + 1f) / 3f;
                    if (scale > 1f) scale = 1f;

                    Vector2 worldCenter = Main.screenPosition;
                    worldCenter.X += Main.screenWidth / 2f;
                    worldCenter.Y += Main.screenHeight / 2f;

                    Vector2 offset = pin.PositionInTiles * 16f - worldCenter;
                    Vector2 drawPos = new Vector2(Main.miniMapX + Main.miniMapWidth / 2f, Main.miniMapY + Main.miniMapHeight / 2f);
                    drawPos += (offset / 16f) * Main.mapMinimapScale;

                    if (drawPos.X > Main.miniMapX + 4 &&
                        drawPos.X < Main.miniMapX + Main.miniMapWidth - 4 &&
                        drawPos.Y > Main.miniMapY + 4 &&
                        drawPos.Y < Main.miniMapY + Main.miniMapHeight - 4)
                    {
                        bool isHovered = IsMouseHovering(drawPos, 10f);
                        DrawPinMarker(drawPos, pin, scale, isHovered);

                        if (isHovered)
                        {
                            hoveredTooltip = $"[c/FFE45E:{pin.Name}]\n类型: {pin.CategoryLabel}\n坐标: [X: {(int)pin.PositionInTiles.X}, Y: {(int)pin.PositionInTiles.Y}]";
                        }
                    }
                }
                // 3. 屏幕中央半透明覆盖地图
                else if (Main.mapStyle == 2)
                {
                    float scale = (Main.mapOverlayScale * 0.2f * 2f + 1f) / 3f;
                    if (scale > 1f) scale = 1f;
                    scale *= Main.UIScale;

                    Vector2 worldCenter = Main.screenPosition;
                    worldCenter.X += Main.screenWidth / 2f;
                    worldCenter.Y += Main.screenHeight / 2f;

                    Vector2 offset = pin.PositionInTiles * 16f - worldCenter;
                    Vector2 drawPos = new Vector2(Main.screenWidth, Main.screenHeight) / 2f;
                    drawPos += (offset / 16f) * Main.mapOverlayScale;

                    bool isHovered = IsMouseHovering(drawPos, 12f * scale);
                    DrawPinMarker(drawPos, pin, scale, isHovered);

                    if (isHovered)
                    {
                        hoveredTooltip = $"[c/FFE45E:{pin.Name}]\n类型: {pin.CategoryLabel}\n坐标: [X: {(int)pin.PositionInTiles.X}, Y: {(int)pin.PositionInTiles.Y}]";
                    }
                }
            }

            if (hoveredTooltip != null)
            {
                Main.instance.MouseText(hoveredTooltip);
            }
        }

        private static void DrawPinMarker(Vector2 pos, StructurePin pin, float scale, bool isHovered)
        {
            Texture2D magicPixel = TextureAssets.MagicPixel.Value;
            if (magicPixel == null) return;

            // 1. 尺寸计算 (图标底框)
            int baseSize = 22;
            int size = (int)(baseSize * Math.Max(0.75f, scale));
            if (isHovered) size += 4;
            Rectangle rect = new Rectangle((int)pos.X - size / 2, (int)pos.Y - size / 2, size, size);

            // 2. 绘制深色半透明底衬与发光外边框
            Color borderColor = pin.Color * (isHovered ? 1f : 0.85f);
            Color bgColor = Color.Black * (isHovered ? 0.85f : 0.72f);

            // 外边框发光
            Main.spriteBatch.Draw(magicPixel, new Rectangle(rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4), borderColor);
            // 内部深色背景
            Main.spriteBatch.Draw(magicPixel, rect, bgColor);

            // 3. 绘制代表性图标
            Texture2D iconTex = null;
            if (pin.ItemId > 0)
            {
                try
                {
                    Main.instance.LoadItem(pin.ItemId);
                    iconTex = TextureAssets.Item[pin.ItemId]?.Value;
                }
                catch { }
            }

            if (iconTex != null)
            {
                float maxDim = Math.Max(iconTex.Width, iconTex.Height);
                float iconScale = (size - 6f) / maxDim;
                Vector2 iconOrigin = new Vector2(iconTex.Width / 2f, iconTex.Height / 2f);
                Main.spriteBatch.Draw(iconTex, pos, null, Color.White * (isHovered ? 1f : 0.95f), 0f, iconOrigin, iconScale, SpriteEffects.None, 0f);
            }
            else
            {
                // 兜底几何方块
                Rectangle coreRect = new Rectangle(rect.X + 3, rect.Y + 3, rect.Width - 6, rect.Height - 6);
                Main.spriteBatch.Draw(magicPixel, coreRect, pin.Color * 0.9f);
            }
        }

        private static bool IsMouseHovering(Vector2 pos, float size)
        {
            return Main.mouseX >= pos.X - size && Main.mouseX <= pos.X + size &&
                   Main.mouseY >= pos.Y - size && Main.mouseY <= pos.Y + size;
        }
    }
}


