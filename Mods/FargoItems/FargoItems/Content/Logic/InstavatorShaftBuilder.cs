using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using TPML.Core.Logging;

namespace FargoItems.Content.Logic
{
    /// <summary>
    /// 地狱直通车世界采掘与垂直通道快速建造引擎。
    /// 建造任务按帧分批执行，自动保护特殊世界机制，安全提取地下宝箱物资，
    /// 并在玩家身旁动态生成黑曜石收纳箱整齐分类存放所有挖掘物资。
    /// </summary>
    public static class InstavatorShaftBuilder
    {
        private static readonly ILogger Logger = LogManager.GetLogger("FargoItems");
        private const int MaxCellsPerUpdate = 512;
        private const int LiquidSettlePasses = 6;
        private static BuildJob _activeJob;

        public static bool IsBuildRunning => _activeJob != null;
        public static bool IsInputLocked => false;
        public static int PendingCellCount => _activeJob == null ? 0 : _activeJob.Plan.CellCount - _activeJob.NextCell;
        public static InstavatorBuildSummary LastBuildSummary { get; private set; }

        public static bool CanUse(Player player)
        {
            if (player == null || player.whoAmI != Main.myPlayer)
            {
                return true;
            }

            return _activeJob == null;
        }

        /// <summary>
        /// 检查是否可以破坏该位置的方块。保护神庙、未击败骷髅王的地牢、恶魔/猩红祭坛、蜂巢幼虫、暗影珠等关键物块。
        /// </summary>
        public static bool OkayToDestroyTile(int x, int y)
        {
            if (!WorldGen.InWorld(x, y, 0)) return false;
            Tile tile = Main.tile[x, y];
            if (tile == null) return false;

            if (tile.active())
            {
                int type = tile.type;

                // 1. 保护神庙砖与神庙祭坛
                if (type == TileID.LihzahrdBrick || type == TileID.LihzahrdAltar) return false;

                // 2. 保护未击败骷髅王的地牢砖
                if (type == TileID.BlueDungeonBrick || type == TileID.GreenDungeonBrick || type == TileID.PinkDungeonBrick)
                {
                    if (!NPC.downedBoss3) return false;
                }

                // 3. 保护恶魔祭坛与猩红祭坛
                if (type == TileID.DemonAltar) return false;

                // 4. 保护蜂巢幼虫（防止误唤醒蜂王）
                if (type == TileID.Larva) return false;

                // 5. 保护暗影珠与猩红之心（防止误砸触发世界事件）
                if (type == TileID.ShadowOrbs) return false;
            }

            return true;
        }

        /// <summary>
        /// 检查是否可以破坏指定背景墙。保护神庙墙以及未击败骷髅王时的地牢墙。
        /// </summary>
        public static bool OkayToDestroyWall(int x, int y, ushort wallType)
        {
            if (wallType == 0) return true;

            // 1. 保护神庙不可破坏墙体
            if (wallType == WallID.LihzahrdBrickUnsafe) return false;

            // 2. 保护未击败骷髅王时的地牢背景墙
            if (wallType == WallID.BlueDungeonUnsafe || wallType == WallID.GreenDungeonUnsafe || wallType == WallID.PinkDungeonUnsafe ||
                wallType == WallID.BlueDungeonTileUnsafe || wallType == WallID.GreenDungeonTileUnsafe || wallType == WallID.PinkDungeonTileUnsafe ||
                wallType == WallID.BlueDungeonSlabUnsafe || wallType == WallID.GreenDungeonSlabUnsafe || wallType == WallID.PinkDungeonSlabUnsafe)
            {
                if (!NPC.downedBoss3) return false;
            }

            return true;
        }

        /// <summary>
        /// 安全提取指定位置的宝箱/容器内的所有物品并注销原宝箱数据
        /// </summary>
        private static void HandleChestAt(BuildJob job, int x, int y, Tile tile)
        {
            if (tile == null || !tile.active()) return;

            int type = tile.type;
            if (type != TileID.Containers && type != TileID.Containers2 && type != TileID.Dressers)
            {
                return;
            }

            int chestLeftX = x;
            int chestTopY = y;

            if (type == TileID.Dressers)
            {
                chestLeftX = x - (tile.frameX % 54) / 18;
                chestTopY = y - (tile.frameY % 36) / 18;
            }
            else
            {
                chestLeftX = x - (tile.frameX % 36) / 18;
                chestTopY = y - (tile.frameY % 36) / 18;
            }

            var coordPoint = new Point(chestLeftX, chestTopY);
            if (!job.HandledChests.Add(coordPoint))
            {
                // 该宝箱已被提取过，避免 2x2 格子重复处理
                return;
            }

            int chestIndex = Chest.FindChest(chestLeftX, chestTopY);
            if (chestIndex >= 0)
            {
                Chest chest = Main.chest[chestIndex];
                if (chest?.item != null)
                {
                    for (int i = 0; i < chest.item.Length; i++)
                    {
                        Item it = chest.item[i];
                        if (it != null && !it.IsAir && it.type > 0 && it.stack > 0)
                        {
                            job.CollectedItems.Add(it.Clone());
                            job.RecoveredChestLootCount++;
                            it.TurnToAir();
                        }
                    }
                }

                Chest.DestroyChest(chestLeftX, chestTopY);
                if (Main.netMode == 2)
                {
                    NetMessage.SendData(34, -1, -1, null, 1, chestLeftX, chestTopY, 0);
                }
            }
        }

        /// <summary>
        /// 常见矿石物品列表，用于排序评分加权
        /// </summary>
        private static readonly HashSet<int> OreItemIds = new HashSet<int>
        {
            ItemID.CopperOre, ItemID.TinOre, ItemID.IronOre, ItemID.LeadOre,
            ItemID.SilverOre, ItemID.TungstenOre, ItemID.GoldOre, ItemID.PlatinumOre,
            ItemID.DemoniteOre, ItemID.CrimtaneOre, ItemID.Meteorite, ItemID.Hellstone,
            ItemID.CobaltOre, ItemID.PalladiumOre, ItemID.MythrilOre, ItemID.OrichalcumOre,
            ItemID.AdamantiteOre, ItemID.TitaniumOre, ItemID.ChlorophyteOre, ItemID.LunarOre
        };

        /// <summary>
        /// 安全破坏方块并捕获其产生的掉落物，存入暂存池，避免地面实体堆积超标或掉入岩浆烧毁
        /// </summary>
        private static void SafeClearTileAndCollect(BuildJob job, int x, int y)
        {
            if (!WorldGen.InWorld(x, y, 0)) return;
            Tile tile = Main.tile[x, y];
            if (tile == null) return;

            // 1. 若为宝箱，先安全提取内容物与注销
            if (tile.active())
            {
                HandleChestAt(job, x, y, tile);
            }

            // 2. 破坏方块并捕获掉落物
            if (tile.active())
            {
                bool[] itemActiveBefore = new bool[Main.maxItems];
                for (int i = 0; i < Main.maxItems; i++)
                {
                    WorldItem wi = Main.item[i];
                    itemActiveBefore[i] = wi != null && wi.active;
                }

                WorldGen.KillTile(x, y, false, false, false);
                if (tile.active())
                {
                    WorldGen.KillTile(x, y, false, false, false);
                }

                for (int i = 0; i < Main.maxItems; i++)
                {
                    WorldItem wi = Main.item[i];
                    if (wi != null && wi.active && !itemActiveBefore[i])
                    {
                        if (wi.inner != null && !wi.inner.IsAir && wi.inner.type > 0 && wi.inner.stack > 0)
                        {
                            job.CollectedItems.Add(wi.inner.Clone());
                        }
                        wi.type = 0;
                        wi.stack = 0;
                        if (wi.inner != null) wi.inner.TurnToAir();
                    }
                }

                if (tile.active())
                {
                    tile.ClearEverything();
                }
            }

            // 3. 破坏墙壁并捕获掉落物（受保护的特殊墙体如神庙/地牢墙直接保留）
            if (tile.wall > 0 && OkayToDestroyWall(x, y, tile.wall))
            {
                bool[] wallItemActiveBefore = new bool[Main.maxItems];
                for (int i = 0; i < Main.maxItems; i++)
                {
                    WorldItem wi = Main.item[i];
                    wallItemActiveBefore[i] = wi != null && wi.active;
                }

                WorldGen.KillWall(x, y, false);

                for (int i = 0; i < Main.maxItems; i++)
                {
                    WorldItem wi = Main.item[i];
                    if (wi != null && wi.active && !wallItemActiveBefore[i])
                    {
                        if (wi.inner != null && !wi.inner.IsAir && wi.inner.type > 0 && wi.inner.stack > 0)
                        {
                            job.CollectedItems.Add(wi.inner.Clone());
                        }
                        wi.type = 0;
                        wi.stack = 0;
                        if (wi.inner != null) wi.inner.TurnToAir();
                    }
                }

                tile.wall = 0;
            }
        }

        public static bool TryStartFullInstavator(Player player, Vector2 mouseWorld)
        {
            return TryStartBuild(player, mouseWorld, InstavatorVariant.Full, Main.maxTilesY - 40, -3, 3);
        }

        public static bool TryStartHalfInstavator(Player player, Vector2 mouseWorld)
        {
            int targetY = (int)(Main.rockLayer + ((Main.maxTilesY - 200) - Main.rockLayer) / 2.0);
            return TryStartBuild(player, mouseWorld, InstavatorVariant.Half, targetY, -2, 2);
        }

        public static bool TryStartDoubleObsidianInstavator(Player player, Vector2 mouseWorld)
        {
            return TryStartBuild(player, mouseWorld, InstavatorVariant.DoubleObsidian, Main.maxTilesY - 40, -5, 5);
        }

        public static void BuildFullInstavator(Player player, Vector2 mouseWorld)
        {
            TryStartFullInstavator(player, mouseWorld);
        }

        public static void BuildHalfInstavator(Player player, Vector2 mouseWorld)
        {
            TryStartHalfInstavator(player, mouseWorld);
        }

        public static void BuildDoubleObsidianInstavator(Player player, Vector2 mouseWorld)
        {
            TryStartDoubleObsidianInstavator(player, mouseWorld);
        }

        public static void Update()
        {
            if (Main.gameMenu)
            {
                _activeJob = null;
                return;
            }

            if (_activeJob == null)
            {
                return;
            }

            try
            {
                bool finished = false;
                int processed = 0;
                while (_activeJob != null && processed++ < MaxCellsPerUpdate)
                {
                    if (_activeJob.NextCell >= _activeJob.Plan.CellCount)
                    {
                        if (_activeJob.RemainingDrainPasses > 0)
                        {
                            DrainResidualLiquidsPass(_activeJob);
                            _activeJob.RemainingDrainPasses--;
                        }
                        else
                        {
                            finished = true;
                        }
                        break;
                    }

                    InstavatorBuildCell cell = _activeJob.Plan.GetCell(_activeJob.NextCell++);
                    _activeJob.ProcessedCells++;
                    ProcessCell(_activeJob, cell);
                }

                if (finished)
                {
                    FinishJob(_activeJob);
                    _activeJob = null;
                }
                else if (_activeJob != null)
                {
                    _activeJob.FramesElapsed++;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("分帧建造异常，已停止当前任务", ex);
                _activeJob = null;
            }
        }

        private static void FinishJob(BuildJob job)
        {
            if (job == null) return;
            job.Stopwatch.Stop();

            // 整理与分类所有收集到的物资
            List<Item> sortedItems = ConsolidateAndSortItems(job.CollectedItems);

            // 获取发起建造的玩家并在其身旁部署黑曜石收纳箱
            Player player = Main.player[job.PlayerWhoAmI];
            int placedChestsCount = 0;
            if (player != null && player.active)
            {
                placedChestsCount = DeployLootToObsidianChests(job, player, sortedItems);
            }

            LastBuildSummary = new InstavatorBuildSummary
            {
                Variant = job.Variant.ToString(),
                StartX = job.Plan.StartX,
                StartY = job.Plan.StartY,
                TargetY = job.Plan.EndY,
                MinOffset = job.Plan.MinOffset,
                MaxOffset = job.Plan.MaxOffset,
                Width = job.Plan.MaxOffset - job.Plan.MinOffset + 1,
                TotalDepth = Math.Abs(job.Plan.EndY - job.Plan.StartY),
                TotalCells = job.Plan.CellCount,
                ProcessedCells = job.ProcessedCells,
                DurationMs = job.Stopwatch.ElapsedMilliseconds,
                DurationFrames = job.FramesElapsed,
                ClearedTiles = job.ClearedTiles,
                ClearedWalls = job.ClearedWalls,
                PlacedRopes = job.PlacedRopes,
                PlacedTorches = job.PlacedTorches,
                PlacedBricks = job.PlacedBricks,
                DrainedLiquids = job.DrainedLiquids,
                DrainedResidualLiquids = job.DrainedResidualLiquids,
                BypassedProtectedTiles = job.BypassedProtectedTiles,
                CollectedItemsCount = sortedItems.Count,
                RecoveredChestLootCount = job.RecoveredChestLootCount,
                PlacedChestsCount = placedChestsCount,
                CompletedAt = DateTime.Now
            };

            Logger.Info($"建造完成: 类型={job.Variant}, 深度={LastBuildSummary.TotalDepth}, 耗时={LastBuildSummary.DurationMs}ms ({LastBuildSummary.DurationFrames} 帧), 收纳箱={placedChestsCount} 个, 收集物资种类={sortedItems.Count}, 宝箱战利品={job.RecoveredChestLootCount} 件, 避让保护物块={job.BypassedProtectedTiles}");

            if (player != null && player.whoAmI == Main.myPlayer)
            {
                if (placedChestsCount > 0)
                {
                    Main.NewText($"[FargoItems] 直通车建造完成！深度: {LastBuildSummary.TotalDepth} 格，已将沿途所有宝箱战利品与矿石物资存入身旁的 {placedChestsCount} 个黑曜石箱中！", 255, 200, 80);
                }
                else
                {
                    Main.NewText($"[FargoItems] 直通车建造完成！深度: {LastBuildSummary.TotalDepth} 格", 255, 200, 80);
                }
            }
        }

        /// <summary>
        /// 合并堆叠同类物品，并按【战利品/高阶装备/矿石宝石优先，基础方块靠后】的规则进行智能排序
        /// </summary>
        public static List<Item> ConsolidateAndSortItems(List<Item> rawItems)
        {
            if (rawItems == null || rawItems.Count == 0) return new List<Item>();

            var stackCounts = new Dictionary<int, int>();
            var uniqueTemplates = new List<Item>();

            foreach (var it in rawItems)
            {
                if (it == null || it.type <= 0 || it.stack <= 0) continue;

                // 装备/带词缀道具/不可堆叠道具单独保留
                if (it.prefix > 0 || it.maxStack <= 1)
                {
                    uniqueTemplates.Add(it.Clone());
                }
                else
                {
                    if (stackCounts.ContainsKey(it.type))
                    {
                        stackCounts[it.type] += it.stack;
                    }
                    else
                    {
                        stackCounts[it.type] = it.stack;
                        Item template = new Item();
                        template.netDefaults(it.type);
                        template.Prefix(it.prefix);
                        uniqueTemplates.Add(template);
                    }
                }
            }

            var consolidated = new List<Item>();
            foreach (var template in uniqueTemplates)
            {
                if (template.prefix > 0 || template.maxStack <= 1)
                {
                    consolidated.Add(template);
                }
                else if (stackCounts.TryGetValue(template.type, out int totalStack))
                {
                    stackCounts.Remove(template.type);
                    int maxStack = template.maxStack > 0 ? template.maxStack : 9999;
                    while (totalStack > 0)
                    {
                        int cur = Math.Min(totalStack, maxStack);
                        Item split = template.Clone();
                        split.stack = cur;
                        consolidated.Add(split);
                        totalStack -= cur;
                    }
                }
            }

            consolidated.Sort((a, b) =>
            {
                int scoreA = GetItemPriorityScore(a);
                int scoreB = GetItemPriorityScore(b);
                if (scoreA != scoreB)
                {
                    return scoreB.CompareTo(scoreA); // 分数高的排在前面
                }
                return a.type.CompareTo(b.type);
            });

            return consolidated;
        }

        private static int GetItemPriorityScore(Item item)
        {
            if (item == null || item.type <= 0) return 0;

            int score = 0;

            // 武器、饰品、防具、高级工具
            if (item.damage > 0) score += 50000;
            if (item.accessory) score += 50000;
            if (item.headSlot > 0 || item.bodySlot > 0 || item.legSlot > 0) score += 40000;
            if (item.pick > 0 || item.axe > 0 || item.hammer > 0) score += 30000;

            // 稀有度加权
            score += Math.Max(0, item.rare) * 10000;

            // 卖价加权
            score += Math.Min(20000, item.value / 10);

            // 生命水晶 / 永久提升道具
            if (item.type == ItemID.LifeCrystal || item.type == ItemID.ManaCrystal || item.type == ItemID.LifeFruit)
            {
                score += 80000;
            }

            // 宝石
            if (item.type == ItemID.Diamond || item.type == ItemID.Ruby || item.type == ItemID.Emerald ||
                item.type == ItemID.Sapphire || item.type == ItemID.Topaz || item.type == ItemID.Amethyst || item.type == ItemID.Amber)
            {
                score += 25000;
            }

            // 矿石与贵重金属
            if (OreItemIds.Contains(item.type) || (item.createTile >= 0 && TileID.Sets.Ore[item.createTile]))
            {
                score += 20000;
            }

            // 宝箱本身
            if (item.createTile == TileID.Containers || item.createTile == TileID.Containers2)
            {
                score += 15000;
            }

            // 药水与消耗品
            if (item.healLife > 0 || item.healMana > 0 || item.buffType > 0)
            {
                score += 10000;
            }

            // 大宗常规物块大幅降权排在最后
            if (item.createTile == TileID.Dirt || item.createTile == TileID.Stone || item.createTile == TileID.Mud ||
                item.createTile == TileID.Sand || item.createTile == TileID.SnowBlock || item.createTile == TileID.Ash ||
                item.createTile == TileID.Granite || item.createTile == TileID.Marble || item.createTile == TileID.IceBlock)
            {
                score = 100;
            }

            return score;
        }

        /// <summary>
        /// 在玩家身旁就近寻找平坦地面并生成黑曜石收纳箱，将物资依次存入
        /// </summary>
        private static int DeployLootToObsidianChests(BuildJob job, Player player, List<Item> items)
        {
            if (items == null || items.Count == 0) return 0;

            int originTileX = (int)(player.Center.X / 16f);
            int originTileY = (int)((player.position.Y + player.height) / 16f) - 1;

            // 候选水平偏移（向两侧依次排开）
            int[] offsets = new int[] { 3, -4, 6, -7, 9, -10, 12, -13, 15, -16, 18, -19, 21, -22, 24, -25, 27, -28, 30, -31 };
            int itemCursor = 0;
            int placedCount = 0;

            foreach (int offset in offsets)
            {
                if (itemCursor >= items.Count) break;

                int chestLeftX = originTileX + offset;
                int chestTopY = originTileY - 1;
                int baseY = chestTopY + 2;

                if (!WorldGen.InWorld(chestLeftX, chestTopY, 10)) continue;

                // 1. 铺设 2 格黑曜石砖底座保障箱子稳固
                for (int bx = chestLeftX; bx <= chestLeftX + 1; bx++)
                {
                    Tile baseTile = Framing.GetTileSafely(bx, baseY);
                    if (!baseTile.active() || !Main.tileSolid[baseTile.type])
                    {
                        WorldGen.PlaceTile(bx, baseY, TileID.ObsidianBrick, false, true, job.PlayerWhoAmI, 0);
                    }
                }

                // 2. 已有箱子则跳过该格，避免静默毁掉玩家箱子与内容
                bool occupiedByChest = false;
                for (int cx = chestLeftX; cx <= chestLeftX + 1 && !occupiedByChest; cx++)
                {
                    for (int cy = chestTopY; cy <= chestTopY + 1; cy++)
                    {
                        Tile cur = Framing.GetTileSafely(cx, cy);
                        if (cur.active() && (cur.type == TileID.Containers || cur.type == TileID.Containers2 || TileID.Sets.BasicChest[cur.type]))
                        {
                            occupiedByChest = true;
                            break;
                        }
                    }
                }
                if (occupiedByChest) continue;

                for (int cx = chestLeftX; cx <= chestLeftX + 1; cx++)
                {
                    for (int cy = chestTopY; cy <= chestTopY + 1; cy++)
                    {
                        Tile cur = Framing.GetTileSafely(cx, cy);
                        if (cur.active())
                        {
                            WorldGen.KillTile(cx, cy, false, false, true);
                            cur.ClearEverything();
                        }
                    }
                }

                // 3. 放置黑曜石箱 (style 44)
                int chestIndex = WorldGen.PlaceChest(chestLeftX, chestTopY, (ushort)TileID.Containers, false, 44);
                if (chestIndex < 0)
                {
                    chestIndex = Chest.CreateChest(chestLeftX, chestTopY);
                    if (chestIndex >= 0)
                    {
                        for (int cx = 0; cx < 2; cx++)
                        {
                            for (int cy = 0; cy < 2; cy++)
                            {
                                Tile t = Framing.GetTileSafely(chestLeftX + cx, chestTopY + cy);
                                t.active(true);
                                t.type = TileID.Containers;
                                t.frameX = (short)(44 * 36 + cx * 18);
                                t.frameY = (short)(cy * 18);
                            }
                        }
                    }
                }

                if (chestIndex >= 0)
                {
                    Chest chest = Main.chest[chestIndex];
                    if (chest != null)
                    {
                        for (int slot = 0; slot < 40 && itemCursor < items.Count; slot++)
                        {
                            chest.item[slot] = items[itemCursor++].Clone();
                        }

                        if (Main.netMode == 2)
                        {
                            NetMessage.SendData(34, -1, -1, null, 0, chestLeftX, chestTopY, 44);
                            for (int slot = 0; slot < 40; slot++)
                            {
                                NetMessage.SendData(32, -1, -1, null, chestIndex, slot);
                            }
                        }
                        placedCount++;
                    }
                }
            }

            // 4. 若出现极端无空间放置情况，剩余物品在玩家身旁安全掉落兜底
            while (itemCursor < items.Count)
            {
                Item leftover = items[itemCursor++];
                if (leftover != null && leftover.type > 0 && leftover.stack > 0)
                {
                    Item.NewItem(null, player.Center, leftover.type, leftover.stack);
                }
            }

            return placedCount;
        }

        private static bool TryStartBuild(Player player, Vector2 mouseWorld, InstavatorVariant variant, int endY, int minOffset, int maxOffset)
        {
            if (player == null || player.whoAmI != Main.myPlayer || _activeJob != null)
            {
                return false;
            }

            int startX = (int)(mouseWorld.X / 16f);
            int startY = (int)(mouseWorld.Y / 16f);
            var plan = new InstavatorBuildPlan(startX, startY, endY, minOffset, maxOffset);
            if (plan.CellCount == 0)
            {
                return false;
            }

            _activeJob = new BuildJob(plan, variant, player.whoAmI);
            _activeJob.Stopwatch.Start();
            SoundEngine.PlaySound(SoundID.Item14, mouseWorld);
            return true;
        }

        private static void ProcessCell(BuildJob job, InstavatorBuildCell cell)
        {
            int x = cell.X;
            int y = cell.Y;
            if (x < 10 || x >= Main.maxTilesX - 10 || y < 10 || y >= Main.maxTilesY - 10) return;

            // 保护特殊物块（神庙、地牢、祭坛、幼虫、暗影珠等）
            if (!OkayToDestroyTile(x, y))
            {
                job.BypassedProtectedTiles++;
                return;
            }

            Tile tile = Main.tile[x, y];
            int desiredTile = GetDesiredTile(job.Variant, cell.Offset, y);
            bool alreadyHasDesiredTile = desiredTile > 0 && tile.active() && tile.type == desiredTile;

            // 已经是目标方块时不清除，重复运行只补缺失的墙或设施，避免绳索被反复重建。
            if (!alreadyHasDesiredTile && (tile.active() || tile.wall > 0 || tile.liquid > 0))
            {
                if (tile.active()) job.ClearedTiles++;
                if (tile.wall > 0) job.ClearedWalls++;
                if (tile.liquid > 0) job.DrainedLiquids++;

                SafeClearTileAndCollect(job, x, y);
                tile = Main.tile[x, y];
            }

            bool changed = false;
            int desiredWall = GetDesiredWall(job.Variant, cell.Offset, y);
            if (desiredWall > 0 && tile.wall != desiredWall && OkayToDestroyWall(x, y, tile.wall))
            {
                WorldGen.PlaceWall(x, y, desiredWall, false);
                if (tile.wall == desiredWall)
                {
                    job.PlacedWalls++;
                    changed = true;
                }
            }

            if (desiredTile > 0 && (!tile.active() || tile.type != desiredTile))
            {
                WorldGen.PlaceTile(x, y, desiredTile, false, false, job.PlayerWhoAmI, 0);
                changed = changed || (tile.active() && tile.type == desiredTile);
                if (desiredTile == TileID.Rope) job.PlacedRopes++;
                else if (desiredTile == TileID.Torches) job.PlacedTorches++;
                else if (desiredTile == TileID.ObsidianBrick) job.PlacedBricks++;
            }

            if (changed && Main.netMode == 2)
            {
                NetMessage.SendTileSquare(-1, x, y, 1, 1, TileChangeType.None);
            }
        }

        private static int GetDesiredWall(InstavatorVariant variant, int offset, int y)
        {
            if (variant == InstavatorVariant.Half)
            {
                return 0; // 半程直通车不铺设背景墙
            }

            if (variant == InstavatorVariant.DoubleObsidian)
            {
                return WallID.ObsidianBrick; // 双轨黑曜石直通车铺设黑曜石砖墙
            }

            return WallID.Stone; // 标准直通车铺设石墙
        }

        private static int GetDesiredTile(InstavatorVariant variant, int offset, int y)
        {
            if (variant == InstavatorVariant.Full)
            {
                if (offset == -3 || offset == 3) return TileID.ObsidianBrick;
                if ((offset == -2 || offset == 2) && y % 10 == 0) return TileID.Torches;
                if (offset == 0) return TileID.Rope;
            }
            else if (variant == InstavatorVariant.Half)
            {
                if (offset == 0) return TileID.Rope;
            }
            else
            {
                if (offset == -5 || offset == 5 || offset == 0) return TileID.ObsidianBrick;
                if ((offset == -4 || offset == 4 || offset == -1 || offset == 1) && y % 10 == 0) return TileID.Torches;
                if (offset == -2 || offset == 2) return TileID.Rope;
            }

            return 0;
        }

        private static bool IsInternalOffset(InstavatorVariant variant, int offset)
        {
            if (variant == InstavatorVariant.Full)
            {
                return offset != -3 && offset != 3;
            }

            if (variant == InstavatorVariant.DoubleObsidian)
            {
                return offset != -5 && offset != 0 && offset != 5;
            }

            return true;
        }

        private static void DrainResidualLiquidsPass(BuildJob job)
        {
            for (int i = 0; i < job.Plan.CellCount; i++)
            {
                InstavatorBuildCell cell = job.Plan.GetCell(i);
                if (!IsInternalOffset(job.Variant, cell.Offset)) continue;
                if (cell.X < 10 || cell.X >= Main.maxTilesX - 10 || cell.Y < 10 || cell.Y >= Main.maxTilesY - 10) continue;

                Tile tile = Main.tile[cell.X, cell.Y];
                if (tile == null || tile.liquid == 0) continue;

                tile.liquid = 0;
                tile.liquidType(0);
                tile.checkingLiquid(false);
                tile.skipLiquid(false);
                job.DrainedResidualLiquids++;
            }
        }

        private sealed class BuildJob
        {
            public BuildJob(InstavatorBuildPlan plan, InstavatorVariant variant, int playerWhoAmI)
            {
                Plan = plan;
                Variant = variant;
                PlayerWhoAmI = playerWhoAmI;
                Stopwatch = new System.Diagnostics.Stopwatch();
                RemainingDrainPasses = LiquidSettlePasses;
            }

            public InstavatorBuildPlan Plan { get; }
            public InstavatorVariant Variant { get; }
            public int PlayerWhoAmI { get; }
            public int NextCell { get; set; }
            public int ProcessedCells { get; set; }
            public int FramesElapsed { get; set; }
            public int ClearedTiles { get; set; }
            public int ClearedWalls { get; set; }
            public int PlacedWalls { get; set; }
            public int PlacedRopes { get; set; }
            public int PlacedTorches { get; set; }
            public int PlacedBricks { get; set; }
            public int DrainedLiquids { get; set; }
            public int DrainedResidualLiquids { get; set; }
            public int BypassedProtectedTiles { get; set; }
            public int RemainingDrainPasses { get; set; }
            public System.Diagnostics.Stopwatch Stopwatch { get; }

            public List<Item> CollectedItems { get; } = new List<Item>();
            public HashSet<Point> HandledChests { get; } = new HashSet<Point>();
            public int RecoveredChestLootCount { get; set; }
        }
    }

    /// <summary>
    /// 直通车建造执行快照汇总
    /// </summary>
    public class InstavatorBuildSummary
    {
        public string Variant { get; set; }
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int TargetY { get; set; }
        public int MinOffset { get; set; }
        public int MaxOffset { get; set; }
        public int Width { get; set; }
        public int TotalDepth { get; set; }
        public int TotalCells { get; set; }
        public int ProcessedCells { get; set; }
        public long DurationMs { get; set; }
        public int DurationFrames { get; set; }
        public int ClearedTiles { get; set; }
        public int ClearedWalls { get; set; }
        public int PlacedWalls { get; set; }
        public int PlacedRopes { get; set; }
        public int PlacedTorches { get; set; }
        public int PlacedBricks { get; set; }
        public int DrainedLiquids { get; set; }
        public int DrainedResidualLiquids { get; set; }
        public int BypassedProtectedTiles { get; set; }
        public int CollectedItemsCount { get; set; }
        public int RecoveredChestLootCount { get; set; }
        public int PlacedChestsCount { get; set; }
        public DateTime CompletedAt { get; set; }
    }

    public enum InstavatorVariant
    {
        Full,
        Half,
        DoubleObsidian
    }
}
