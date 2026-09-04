using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI;
using Terraria.ID;
using TPML.Content.Fusion;
using TPML.Core.Diagnostics;

namespace WandsTool.Content
{
    public class WandAction
    {
        public enum BlockType
        {
            /// <summary>
            /// 固体
            /// </summary>
            Solid,
            /// <summary>
            /// 半砖
            /// </summary>
            HalfBlock,
            SlopeUpLeft,
            SlopeUpRight,
            SlopeDownLeft,
            SlopeDownRight,
        }

        public struct tile
        {
            public int x;
            public int y;
            public bool isTile;
            public bool isWall;
            public BlockType bt;
            public bool isReplace;
            public int filterTileType;
            public int filterWallType;
            public Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode toolMode;

            public tile(int x, int y, bool isTile, bool isWall, BlockType bt, bool isReplace = false, int filterTile = -1, int filterWall = -1)
            {
                this.x = x;
                this.y = y;
                this.isTile = isTile;
                this.isWall = isWall;
                this.bt = bt;
                this.isReplace = isReplace;
                this.filterTileType = filterTile;
                this.filterWallType = filterWall;
                this.toolMode = 0;
            }

            public tile(int x, int y, Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode toolMode)
            {
                this.x = x;
                this.y = y;
                this.isTile = false;
                this.isWall = false;
                this.bt = 0;
                this.isReplace = false;
                this.filterTileType = -1;
                this.filterWallType = -1;
                this.toolMode = toolMode;
            }
        }

        public struct liquidTile
        {
            public int x;
            public int y;
            public GameMain.LiquidMode mode;
            public bool isInfinite;

            public liquidTile(int x, int y, GameMain.LiquidMode mode, bool isInfinite)
            {
                this.x = x;
                this.y = y;
                this.mode = mode;
                this.isInfinite = isInfinite;
            }
        }

        // 使用 Queue<T> 替代 List<T>，实现 O(1) 出队，大幅提升高频大范围操作吞吐量
        protected static Queue<tile> tilePlace = new Queue<tile>();
        protected static Queue<tile> tileKill = new Queue<tile>();
        protected static Queue<liquidTile> liquidQueue = new Queue<liquidTile>();
        protected static Queue<tile> wirePlace = new Queue<tile>();
        protected static Queue<tile> wireKill = new Queue<tile>();
        protected static Queue<Projectile> wireLinePlaceAndKill = new Queue<Projectile>();

        // 物料来源追踪：当前 FirstItem_TileOrWall 找到的物品所属 Fusion 源（背包/光标源为 null）
        private static IFusionItemSource _lastFusionSource = null;
        // 批量操作期间发生变动的 Fusion 源集合（队列处理完毕后统一 OnModified 持久化，避免高频写盘）
        private static readonly HashSet<IFusionItemSource> _fusionDirty = new HashSet<IFusionItemSource>();

        public static int Count { get; protected set; } = 0;

        public static void Clear()
        {
            tilePlace?.Clear();
            tileKill?.Clear();
            liquidQueue?.Clear();
            wirePlace?.Clear();
            wireKill?.Clear();
            wireLinePlaceAndKill?.Clear();
            _fusionDirty?.Clear();
            _lastFusionSource = null;
            Count = 0;
        }

        public static void Update(int updateCount = -1)
        {
            using (PerformanceProfiler.Measure("WandsTool", "WandAction.Update"))
            {
                if (GameMain.Wand_UpdateCount < 1) GameMain.Wand_UpdateCount = 1;
                if (updateCount < 1) updateCount = GameMain.Wand_UpdateCount;

                if (tilePlace == null) tilePlace = new Queue<tile>();
                if (tileKill == null) tileKill = new Queue<tile>();
                if (liquidQueue == null) liquidQueue = new Queue<liquidTile>();
                if (wirePlace == null) wirePlace = new Queue<tile>();
                if (wireKill == null) wireKill = new Queue<tile>();
                if (wireLinePlaceAndKill == null) wireLinePlaceAndKill = new Queue<Projectile>();

                int batchSize = Math.Max(GameMain.Wand_BatchSize, 1);
                Player player = Main.LocalPlayer;

                // 1. 批量处理放置与替换
                if (tilePlace.Count > 0 && player != null)
                {
                    int processCount = Math.Min(batchSize, tilePlace.Count);
                    for (int i = 0; i < processCount; i++)
                    {
                        tile t = tilePlace.Dequeue();
                        if (t.isTile) placeTile(t, player);
                        if (t.isWall) placeWall(t, player);
                    }
                }

                // 2. 批量处理破坏/星爆
                if (tileKill.Count > 0)
                {
                    int processCount = Math.Min(batchSize, tileKill.Count);
                    for (int i = 0; i < processCount; i++)
                    {
                        tile t = tileKill.Dequeue();
                        killTile(t, player);
                    }
                }

                // 3. 批量处理液体操作
                if (liquidQueue.Count > 0)
                {
                    int processCount = Math.Min(batchSize, liquidQueue.Count);
                    for (int i = 0; i < processCount; i++)
                    {
                        liquidTile lt = liquidQueue.Dequeue();
                        processLiquid(lt, player);
                    }
                }

                // 4. 电线放置
                if (wirePlace.Count > 0 && player != null)
                {
                    int processCount = Math.Min(batchSize, wirePlace.Count);
                    for (int i = 0; i < processCount; i++)
                    {
                        tile t = wirePlace.Dequeue();
                        placeWire(t, player);
                    }
                }

                // 5. 电线拆除
                if (wireKill.Count > 0)
                {
                    int processCount = Math.Min(batchSize, wireKill.Count);
                    for (int i = 0; i < processCount; i++)
                    {
                        tile t = wireKill.Dequeue();
                        killWire(t);
                    }
                }

                // 6. 批量电线连接操作
                if (wireLinePlaceAndKill.Count > 0 && player != null)
                {
                    int processCount = Math.Min(batchSize, wireLinePlaceAndKill.Count);
                    for (int i = 0; i < processCount; i++)
                    {
                        Projectile p = wireLinePlaceAndKill.Dequeue();
                        wireLineAction(p, player);
                    }
                }

                Count = tilePlace.Count + tileKill.Count + liquidQueue.Count + wirePlace.Count + wireKill.Count + wireLinePlaceAndKill.Count;

                // 队列全部处理完毕：统一持久化 Fusion 源变动，并归档当前撤销记录（未实际变化则丢弃）
                if (Count == 0 && player != null)
                {
                    FlushFusionDirty(player);
                    WandHistory.CheckFinalize(player);
                }

                if (--updateCount > 0) Update(updateCount);
            }
        }

        /// <summary>
        /// 判定物品是否属于草种、地被植物（苔藓）或绿化法杖
        /// </summary>
        public static bool IsGroundCoverItem(Item item)
        {
            if (item == null || item.IsAir) return false;
            if (item.type == ItemID.StaffofRegrowth || item.type == ItemID.AcornAxe) return true;
            if (item.type > 0 && item.type < ItemID.Count)
            {
                if (ItemID.Sets.GrassSeeds[item.type]) return true;
                if (ItemID.Sets.Moss[item.type]) return true;
            }
            if (item.createTile >= 0 && item.createTile < Main.tileMoss.Length && Main.tileMoss[item.createTile]) return true;
            return false;
        }

        private static bool IsDirtFamily(int type)
        {
            return type == TileID.Dirt || type == TileID.Grass || type == TileID.CorruptGrass || 
                   type == TileID.CrimsonGrass || type == TileID.HallowedGrass;
        }

        private static bool IsMudFamily(int type)
        {
            return type == TileID.Mud || type == TileID.JungleGrass || type == TileID.MushroomGrass || 
                   type == TileID.CorruptJungleGrass || type == TileID.CrimsonJungleGrass;
        }

        private static bool IsAshFamily(int type)
        {
            return type == TileID.Ash || type == TileID.AshGrass;
        }

        private static int GetBrickMossType(int mossTile)
        {
            if (mossTile == 381) return 517;
            if (mossTile == 534) return 535;
            if (mossTile == 536) return 537;
            if (mossTile == 539) return 540;
            if (mossTile == 625) return 626;
            if (mossTile == 627) return 628;
            if (mossTile >= 179 && mossTile <= 184) return 512 + mossTile - 179;
            return mossTile;
        }

        private static void CleanIncompatibleVegetationAbove(int x, int y, int grassType)
        {
            if (y <= 0) return;
            Tile above = Main.tile[x, y - 1];
            if (above == null || !above.active()) return;

            int aboveType = above.type;
            bool kill = false;

            if (aboveType == TileID.Plants || aboveType == TileID.Plants2)
            {
                if (grassType != TileID.Grass && grassType != TileID.HallowedGrass) kill = true;
            }
            else if (aboveType == TileID.CorruptPlants || aboveType == TileID.CorruptThorns)
            {
                if (grassType != TileID.CorruptGrass && grassType != TileID.CorruptJungleGrass) kill = true;
            }
            else if (aboveType == TileID.CrimsonPlants || aboveType == TileID.CrimsonThorns)
            {
                if (grassType != TileID.CrimsonGrass && grassType != TileID.CrimsonJungleGrass) kill = true;
            }
            else if (aboveType == TileID.HallowedPlants || aboveType == TileID.HallowedPlants2)
            {
                if (grassType != TileID.HallowedGrass) kill = true;
            }
            else if (aboveType == TileID.JunglePlants || aboveType == TileID.JunglePlants2)
            {
                if (grassType != TileID.JungleGrass) kill = true;
            }
            else if (aboveType == TileID.MushroomPlants)
            {
                if (grassType != TileID.MushroomGrass) kill = true;
            }
            else if (aboveType == TileID.AshPlants)
            {
                if (grassType != TileID.AshGrass) kill = true;
            }

            if (kill)
            {
                WorldGen.KillTile(x, y - 1, fail: false, effectOnly: false, noItem: false);
                if (Main.netMode == 1)
                {
                    NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, x, y - 1);
                }
            }
            else
            {
                WorldGen.TileFrame(x, y - 1, resetFrame: true);
            }
        }

        private static bool TryPlantGroundCover(tile t, Player player, Item item)
        {
            if (!WorldGen.InWorld(t.x, t.y)) return false;
            Tile tile = Main.tile[t.x, t.y];
            if (tile == null || !tile.active()) return false; // 严格跳过空气/空地

            int curType = tile.type;
            int targetTileType = -1;
            bool isRegrowth = item.type == ItemID.StaffofRegrowth || item.type == ItemID.AcornAxe;

            // 1. 计算目标方块类型与基质智能适配
            if (isRegrowth)
            {
                if (IsDirtFamily(curType) || IsMudFamily(curType))
                {
                    targetTileType = TileID.Grass;
                }
                else if (curType == TileID.Stone)
                {
                    targetTileType = TileID.GreenMoss;
                }
                else if (curType == TileID.GrayBrick)
                {
                    targetTileType = 512;
                }
            }
            else if (item.type == ItemID.GrassSeeds)
            {
                if (IsDirtFamily(curType) || IsMudFamily(curType))
                {
                    targetTileType = TileID.Grass;
                }
            }
            else if (item.type == ItemID.HallowedSeeds)
            {
                if (IsDirtFamily(curType) || IsMudFamily(curType))
                {
                    targetTileType = TileID.HallowedGrass;
                }
            }
            else if (item.type == ItemID.JungleGrassSeeds)
            {
                if (IsMudFamily(curType) || IsDirtFamily(curType))
                {
                    targetTileType = TileID.JungleGrass;
                }
            }
            else if (item.type == ItemID.MushroomGrassSeeds)
            {
                if (IsMudFamily(curType) || IsDirtFamily(curType))
                {
                    targetTileType = TileID.MushroomGrass;
                }
            }
            else if (item.type == ItemID.CorruptSeeds)
            {
                if (IsMudFamily(curType))
                {
                    targetTileType = TileID.CorruptJungleGrass;
                }
                else if (IsDirtFamily(curType))
                {
                    targetTileType = TileID.CorruptGrass;
                }
            }
            else if (item.type == ItemID.CrimsonSeeds)
            {
                if (IsMudFamily(curType))
                {
                    targetTileType = TileID.CrimsonJungleGrass;
                }
                else if (IsDirtFamily(curType))
                {
                    targetTileType = TileID.CrimsonGrass;
                }
            }
            else if (item.type == ItemID.AshGrassSeeds)
            {
                if (IsAshFamily(curType))
                {
                    targetTileType = TileID.AshGrass;
                }
            }
            else if ((item.type > 0 && item.type < ItemID.Count && ItemID.Sets.Moss[item.type]) ||
                     (item.createTile >= 0 && item.createTile < Main.tileMoss.Length && Main.tileMoss[item.createTile]))
            {
                int mossTile = item.createTile;
                if (curType == TileID.Stone || (curType >= 0 && curType < Main.tileMoss.Length && Main.tileMoss[curType]))
                {
                    targetTileType = mossTile;
                }
                else if (curType == TileID.GrayBrick)
                {
                    targetTileType = GetBrickMossType(mossTile);
                }
            }

            // 非合法底土或无法转化的方块，安全跳过
            if (targetTileType < 0) return false;

            // 已经是同种草/苔藓，只校准坡度
            if (curType == targetTileType)
            {
                SetSlopeFor(t.x, t.y, t.bt);
                return true;
            }

            // 2. 准入模式判断：
            // 裸土（纯泥土、纯泥块、纯灰烬、纯石头、纯灰砖）在常规放置模式和替换模式下均可直接播种；
            // 已经长了草/苔藓的方块，必须开启替换模式（isReplace 或 Wand_ReplaceExisting）才允许替换
            bool isBareSubstrate = curType == TileID.Dirt || curType == TileID.Mud || curType == TileID.Ash || 
                                   curType == TileID.Stone || curType == TileID.GrayBrick;
            if (!isBareSubstrate && !t.isReplace && !GameMain.Wand_ReplaceExisting)
            {
                return false;
            }

            // 3. 同材质过滤：若开启且起点有效，当前方块类型不匹配则跳过
            if (GameMain.Wand_MatchFilter && t.filterTileType >= 0 && curType != t.filterTileType)
            {
                return false;
            }

            // 4. 执行方块类型变更与坡度设置
            tile.type = (ushort)targetTileType;
            SetSlopeFor(t.x, t.y, t.bt);
            WorldGen.SquareTileFrame(t.x, t.y, resetFrame: true);

            // 5. 检查并清理上方不兼容的地表野生植物
            CleanIncompatibleVegetationAbove(t.x, t.y, targetTileType);

            // 6. 网络同步
            ActionUtils.updateData_placeTile(t.x, t.y, 0);
            if (Main.netMode == 1)
            {
                NetMessage.SendTileSquare(-1, t.x, t.y, 1);
            }

            // 7. 物料扣除
            if (!isRegrowth)
            {
                ConsumeMaterial(player, item);
            }

            // 8. 音效反馈
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, new Vector2(t.x * 16, t.y * 16));

            return true;
        }

        private static void placeTile(tile t, Player player)
        {
            if (canTile(t) == false) return;

            Tile tile = Main.tile[t.x, t.y];
            Item item = FirstItem_Tile(player);

            if (item == null) return;
            if (item.createTile < 0 && !IsGroundCoverItem(item)) return;

            // 专属草种/地被植物/法杖播种与转换管线（纯转化/播种，绝不执行 KillTile 破坏土壤成空气）
            if (IsGroundCoverItem(item))
            {
                TryPlantGroundCover(t, player, item);
                return;
            }

            if (tile?.active() == true) // 目标已有方块
            {
                if (tile.type == item.createTile) // 相同方块只调整坡度
                {
                    SetSlopeFor(t.x, t.y, t.bt);
                    return;
                }

                // 未开启替换已有方块时，跳过，绝对不破坏/覆盖已有方块
                if (!t.isReplace && !GameMain.Wand_ReplaceExisting)
                {
                    return;
                }

                // 开启同材质过滤替换时：若起点选中了有效方块且当前方块类型不匹配，则跳过（保护家具与其余物块）
                if (GameMain.Wand_MatchFilter && t.filterTileType >= 0 && tile.type != t.filterTileType)
                {
                    return;
                }

                // 尝试安全替换
                bool replaced = false;
                try
                {
                    replaced = WorldGen.ReplaceTile(t.x, t.y, (ushort)item.createTile, item.placeStyle);
                }
                catch
                {
                    replaced = false;
                }

                if (!replaced)
                {
                    WorldGen.KillTile(t.x, t.y, fail: false, effectOnly: false, noItem: false);
                    replaced = WorldGen.PlaceTile(t.x, t.y, item.createTile, true, true, player.whoAmI, item.placeStyle);
                }

                // 成功放置后再扣除物品并同步
                if (replaced)
                {
                    ConsumeMaterial(player, item);

                    SetSlopeFor(t.x, t.y, t.bt);
                    ActionUtils.updateData_placeTile(t.x, t.y, item.placeStyle);
                }
            }
            else // 空格放置
            {
                // 未开启填充空处时，跳过，绝对不填塞空白处
                if (!GameMain.Wand_FillEmpty)
                {
                    return;
                }

                bool v = WorldGen.PlaceTile(t.x, t.y, item.createTile, true, true, player.whoAmI, item.placeStyle);
                if (v)
                {
                    ConsumeMaterial(player, item);

                    SetSlopeFor(t.x, t.y, t.bt);
                    ActionUtils.updateData_placeTile(t.x, t.y, item.placeStyle);
                }
            }
        }

        private static void placeWall(tile t, Player player)
        {
            if (canTile(t) == false) return;

            Tile tile = Main.tile[t.x, t.y];
            Item item = FirstItem_Wall(player);

            if (item == null || item.createWall <= 0) return;
            if (tile?.wall == item.createWall) return;

            if (tile?.wall > 0) // 已有背景墙
            {
                // 未开启替换已有墙壁时，跳过，绝对不覆盖已有背景墙
                if (!t.isReplace && !GameMain.Wand_ReplaceExisting)
                {
                    return;
                }

                // 开启同材质过滤替换时：若起点选中了有效背景墙且当前背景墙类型不匹配，则跳过保护
                if (GameMain.Wand_MatchFilter && t.filterWallType > 0 && tile.wall != t.filterWallType)
                {
                    return;
                }

                bool replaced = false;
                try
                {
                    replaced = WorldGen.ReplaceWall(t.x, t.y, (ushort)item.createWall);
                }
                catch
                {
                    replaced = false;
                }

                if (!replaced)
                {
                    WorldGen.KillWall(t.x, t.y, fail: false);
                    WorldGen.PlaceWall(t.x, t.y, item.createWall, true);
                }

                if (tile.wall == item.createWall)
                {
                    ConsumeMaterial(player, item);

                    WorldGen.SquareWallFrame(t.x, t.y, false);
                    ActionUtils.updateData_placeWall(t.x, t.y);
                }
            }
            else // 空格放置背景墙
            {
                // 未开启填充空处时，跳过
                if (!GameMain.Wand_FillEmpty)
                {
                    return;
                }

                WorldGen.PlaceWall(t.x, t.y, item.createWall, true);
                if (tile?.wall == item.createWall)
                {
                    ConsumeMaterial(player, item);

                    WorldGen.SquareWallFrame(t.x, t.y, false);
                    ActionUtils.updateData_placeWall(t.x, t.y);
                }
            }
        }

        private static void killTile(tile t, Player player)
        {
            if (canTile(t) == false) return;

            Tile tile = Main.tile[t.x, t.y];
            if (tile == null) return;

            bool killed = false;
            if (t.isTile && tile.active())
            {
                if (t.filterTileType < 0 || tile.type == t.filterTileType)
                {
                    WorldGen.KillTile(t.x, t.y, fail: false, effectOnly: false, noItem: false);
                    NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, t.x, t.y);
                    killed = true;
                }
            }
            if (t.isWall && tile.wall > 0)
            {
                if (t.filterWallType < 0 || tile.wall == t.filterWallType)
                {
                    WorldGen.KillWall(t.x, t.y, fail: false);
                    NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 2, t.x, t.y);
                    killed = true;
                }
            }

            if (killed && GameMain.Wand_CollectDrops && player != null)
            {
                CollectDropsNear(t.x, t.y, player);
            }
        }

        public static void CollectDropsNear(int tileX, int tileY, Player player)
        {
            Vector2 tilePos = new Vector2(tileX * 16 + 8, tileY * 16 + 8);
            float maxDistSq = 96 * 96; // 判定半径约6格内的掉落物
            for (int i = 0; i < 400; i++)
            {
                var it = Main.item[i];
                if (it != null && it.active && it.stack > 0)
                {
                    if (Vector2.DistanceSquared(it.Center, tilePos) < maxDistSq)
                    {
                        it.position = player.Center - new Vector2(it.width / 2f, it.height / 2f);
                        it.velocity = Vector2.Zero;
                        it.beingGrabbed = true;
                    }
                }
            }
        }

        private static void processLiquid(liquidTile lt, Player player)
        {
            if (canTile(new tile(lt.x, lt.y, false, false, BlockType.Solid)) == false) return;

            Tile tile = Main.tile[lt.x, lt.y];
            if (tile == null) return;

            switch (lt.mode)
            {
                case GameMain.LiquidMode.Absorb:
                    if (tile.liquid > 0)
                    {
                        int liquidType = (int)tile.liquidType();
                        tile.liquid = 0;
                        WorldGen.SquareTileFrame(lt.x, lt.y, true);
                        Liquid.AddWater(lt.x, lt.y);
                        NetMessage.sendWater(lt.x, lt.y);

                        // 吸收水/岩浆/蜂蜜装桶（微光不可装桶，不给予无底微光桶）
                        if (ModConfig.IsConsumablesItem() && !lt.isInfinite && player != null && liquidType != 3)
                        {
                            TryFillBucket(player, liquidType);
                        }
                    }
                    break;

                case GameMain.LiquidMode.Clear:
                    if (tile.liquid > 0)
                    {
                        tile.liquid = 0;
                        WorldGen.SquareTileFrame(lt.x, lt.y, true);
                        Liquid.AddWater(lt.x, lt.y);
                        NetMessage.sendWater(lt.x, lt.y);
                    }
                    break;

                case GameMain.LiquidMode.Water:
                case GameMain.LiquidMode.Lava:
                case GameMain.LiquidMode.Honey:
                case GameMain.LiquidMode.Shimmer:
                    int targetType = 0;
                    int bucketType = ItemID.WaterBucket;
                    int bottomlessType = ItemID.BottomlessBucket;

                    if (lt.mode == GameMain.LiquidMode.Water)
                    {
                        targetType = 0;
                        bucketType = ItemID.WaterBucket;
                        bottomlessType = ItemID.BottomlessBucket;
                    }
                    else if (lt.mode == GameMain.LiquidMode.Lava)
                    {
                        targetType = 1;
                        bucketType = ItemID.LavaBucket;
                        bottomlessType = ItemID.BottomlessLavaBucket;
                    }
                    else if (lt.mode == GameMain.LiquidMode.Honey)
                    {
                        targetType = 2;
                        bucketType = ItemID.HoneyBucket;
                        bottomlessType = ItemID.BottomlessHoneyBucket;
                    }
                    else if (lt.mode == GameMain.LiquidMode.Shimmer)
                    {
                        targetType = 3;
                        bucketType = ItemID.BottomlessShimmerBucket;
                        bottomlessType = ItemID.BottomlessShimmerBucket;

                        // 微光仅在无限模式或背包拥有无底微光桶时允许放置
                        if (!lt.isInfinite && player != null && !HasItemInInventory(player, ItemID.BottomlessShimmerBucket))
                        {
                            return;
                        }
                    }

                    // 若已有相同满液体则跳过
                    if (tile.liquid == 255 && tile.liquidType() == targetType) return;

                    // 非无限模式且需要消耗物品时检查背包桶
                    if (!lt.isInfinite && ModConfig.IsConsumablesItem() && player != null && targetType != 3)
                    {
                        if (!TryConsumeLiquidBucket(player, bucketType, bottomlessType))
                        {
                            return;
                        }
                    }

                    tile.liquidType(targetType);
                    tile.liquid = 255;
                    WorldGen.SquareTileFrame(lt.x, lt.y, true);
                    Liquid.AddWater(lt.x, lt.y);
                    NetMessage.sendWater(lt.x, lt.y);
                    break;
            }
        }

        private static bool HasItemInInventory(Player player, int itemType)
        {
            if (player?.inventory == null) return false;
            for (int i = 0; i < player.inventory.Length; i++)
            {
                Item it = player.inventory[i];
                if (it != null && it.stack > 0 && it.type == itemType) return true;
            }
            return false;
        }

        /// <summary>
        /// 优先将物品堆叠放入玩家背包，背包满时优先注入激活的 Fusion 容器（同类堆叠 -> 空格），最后才实体掉落，彻底消除掉落物风暴
        /// </summary>
        public static void GiveItemToPlayer(Player player, int itemType, int count = 1)
        {
            if (player?.inventory == null || count <= 0) return;

            int remaining = count;

            // 1. 优先尝试与背包已有相同物品堆叠
            for (int i = 0; i < 50; i++)
            {
                Item it = player.inventory[i];
                if (it != null && it.type == itemType && it.stack < it.maxStack)
                {
                    int add = Math.Min(remaining, it.maxStack - it.stack);
                    it.stack += add;
                    remaining -= add;
                    if (remaining <= 0) return;
                }
            }

            // 2. 放入背包空格
            for (int i = 0; i < 50; i++)
            {
                Item it = player.inventory[i];
                if (it == null || it.type == ItemID.None || it.stack <= 0)
                {
                    player.inventory[i] = new Item();
                    player.inventory[i].SetDefaults(itemType);
                    int add = Math.Min(remaining, player.inventory[i].maxStack);
                    player.inventory[i].stack = add;
                    remaining -= add;
                    if (remaining <= 0) return;
                }
            }

            // 3. 背包全满时穿透注入激活的 Fusion 容器（猪猪存钱罐、保险箱、虚空袋、护卫熔炉等）
            if (remaining > 0)
            {
                var sources = InventoryFusionManager.GetActiveSources(player);
                for (int s = 0; s < sources.Count && remaining > 0; s++)
                {
                    var src = sources[s];
                    Item[] slots = src.GetSlots(player);
                    if (slots == null || slots.Length == 0) continue;

                    bool modified = false;

                    // 3.1 先注入同类堆叠
                    for (int i = 0; i < slots.Length && remaining > 0; i++)
                    {
                        Item it = slots[i];
                        if (it != null && !it.IsAir && it.type == itemType && it.stack < it.maxStack)
                        {
                            int add = Math.Min(remaining, it.maxStack - it.stack);
                            it.stack += add;
                            remaining -= add;
                            modified = true;
                        }
                    }

                    // 3.2 再放入空格
                    for (int i = 0; i < slots.Length && remaining > 0; i++)
                    {
                        Item it = slots[i];
                        if (it == null || it.IsAir)
                        {
                            slots[i] = new Item();
                            slots[i].SetDefaults(itemType);
                            int add = Math.Min(remaining, slots[i].maxStack);
                            slots[i].stack = add;
                            remaining -= add;
                            modified = true;
                        }
                    }

                    if (modified)
                    {
                        try { src.OnModified(player); } catch { }
                    }
                }
            }

            // 4. 主背包与 Fusion 均已满，掉落物生成
            if (remaining > 0)
            {
                player.QuickSpawnItem(null, itemType, remaining);
            }
        }

        /// <summary>
        /// 检查背包与激活 Fusion 容器中的液体桶（无底桶不消耗；普通桶扣除并返还空桶）
        /// </summary>
        private static bool TryConsumeLiquidBucket(Player player, int bucketType, int bottomlessType)
        {
            if (player?.inventory == null) return false;

            // 1. 检查是否存在无底桶（无底桶不消耗数量，背包与 Fusion 容器均可）
            for (int i = 0; i < player.inventory.Length; i++)
            {
                Item it = player.inventory[i];
                if (it != null && it.stack > 0 && it.type == bottomlessType) return true;
            }
            var sources = InventoryFusionManager.GetActiveSources(player);
            for (int s = 0; s < sources.Count; s++)
            {
                var src = sources[s];
                Item[] slots = src.GetSlots(player);
                if (slots == null) continue;
                for (int i = 0; i < slots.Length; i++)
                {
                    Item it = slots[i];
                    if (it != null && !it.IsAir && it.stack > 0 && it.type == bottomlessType) return true;
                }
            }

            // 2. 检查普通液体桶并转为空桶存入背包（先主背包后 Fusion 容器）
            for (int i = 0; i < player.inventory.Length; i++)
            {
                Item it = player.inventory[i];
                if (it != null && it.stack > 0 && it.type == bucketType)
                {
                    it.stack -= 1;
                    if (it.stack <= 0) it.TurnToAir();

                    GiveItemToPlayer(player, ItemID.EmptyBucket, 1);
                    return true;
                }
            }
            for (int s = 0; s < sources.Count; s++)
            {
                var src = sources[s];
                Item[] slots = src.GetSlots(player);
                if (slots == null) continue;
                for (int i = 0; i < slots.Length; i++)
                {
                    Item it = slots[i];
                    if (it != null && !it.IsAir && it.stack > 0 && it.type == bucketType)
                    {
                        it.stack -= 1;
                        if (it.stack <= 0) it.TurnToAir();
                        _fusionDirty.Add(src);

                        GiveItemToPlayer(player, ItemID.EmptyBucket, 1);
                        return true;
                    }
                }
            }

            return false;
        }

        private static void TryFillBucket(Player player, int liquidType)
        {
            if (player?.inventory == null) return;

            for (int i = 0; i < player.inventory.Length; i++)
            {
                Item it = player.inventory[i];
                if (it != null && it.stack > 0 && it.type == ItemID.EmptyBucket)
                {
                    it.stack -= 1;
                    if (it.stack <= 0) it.TurnToAir();

                    int filledId = ItemID.WaterBucket;
                    if (liquidType == 1) filledId = ItemID.LavaBucket;
                    else if (liquidType == 2) filledId = ItemID.HoneyBucket;

                    GiveItemToPlayer(player, filledId, 1);
                    break;
                }
            }
        }

        public static void SetSlopeFor(int x, int y, BlockType bt)
        {
            Tile tile = Main.tile[x, y];
            if (WorldGen.CanPoundTile(x, y)) // 能否被锤击
            {
                switch (bt)
                {
                    case BlockType.Solid: WorldGen.SlopeTile(x, y, 0); break;
                    case BlockType.SlopeDownLeft: WorldGen.SlopeTile(x, y, 1); break;
                    case BlockType.SlopeDownRight: WorldGen.SlopeTile(x, y, 2); break;
                    case BlockType.SlopeUpLeft: WorldGen.SlopeTile(x, y, 3); break;
                    case BlockType.SlopeUpRight: WorldGen.SlopeTile(x, y, 4); break;
                    case BlockType.HalfBlock: tile?.halfBrick(true); break;
                    default: break;
                }

                WorldGen.SquareTileFrame(x, y, false);
            }
        }

        private static void placeWire(tile t, Player player)
        {
            if (canTile(t) == false) return;
            Tile existing = Main.tile[t.x, t.y];
            if (existing == null) return;

            TryPlaceOneWire(t, player, Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Red, ItemID.Wire, existing.wire(), WorldGen.PlaceWire, 5);
            TryPlaceOneWire(t, player, Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Green, ItemID.Wire, existing.wire3(), WorldGen.PlaceWire3, 12);
            TryPlaceOneWire(t, player, Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Blue, ItemID.Wire, existing.wire2(), WorldGen.PlaceWire2, 10);
            TryPlaceOneWire(t, player, Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Yellow, ItemID.Wire, existing.wire4(), WorldGen.PlaceWire4, 16);
            TryPlaceOneWire(t, player, Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Actuator, ItemID.Actuator, existing.actuator(), WorldGen.PlaceActuator, 8);
        }

        private static void TryPlaceOneWire(tile t, Player player, Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode mode, int consumeType, bool alreadyPresent, Func<int, int, bool> place, int tileManipulationKind)
        {
            if (!t.toolMode.HasFlag(mode) || alreadyPresent) return;
            if (ModConfig.IsConsumablesItem() && !TryConsumeWireOrActuator(player, consumeType)) return;
            if (!place(t.x, t.y)) return;
            WandHistory.AccumulateConsume(consumeType, ModConfig.IsConsumablesItem() ? 1 : 0);
            NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, tileManipulationKind, t.x, t.y);
        }

        private static bool TryConsumeWireOrActuator(Player player, int itemType)
        {
            if (player?.inventory == null || itemType <= 0) return false;
            for (int i = 0; i < 58; i++)
            {
                Item item = player.inventory[i];
                if (item != null && item.type == itemType && item.stack > 0)
                {
                    item.stack--;
                    if (item.stack <= 0) item.TurnToAir();
                    return true;
                }
            }
            if (InventoryFusionManager.ConsumeItem(player, itemType))
            {
                return true;
            }
            return false;
        }

        private static void killWire(tile t)
        {
            if (canTile(t) == false) return;

            if (t.toolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Red))
            {
                WorldGen.KillWire(t.x, t.y);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 6, t.x, t.y);
            }
            if (t.toolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Green))
            {
                WorldGen.KillWire3(t.x, t.y);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 13, t.x, t.y);
            }
            if (t.toolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Blue))
            {
                WorldGen.KillWire2(t.x, t.y);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 11, t.x, t.y);
            }
            if (t.toolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Yellow))
            {
                WorldGen.KillWire4(t.x, t.y);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 17, t.x, t.y);
            }
            if (t.toolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Actuator))
            {
                WorldGen.KillActuator(t.x, t.y);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 9, t.x, t.y);
            }
        }

        private static void wireLineAction(Projectile proj, Player player)
        {
            Point ps = new Vector2(proj.ai[0], proj.ai[1]).ToPoint();
            Point pe = proj.Center.ToTileCoordinates();

            if (Main.netMode == 1)
            {
                NetMessage.SendData(109, -1, -1, null, ps.X, ps.Y, pe.X, pe.Y, (int)WiresUI.Settings.ToolMode);
            }
            else
            {
                Wiring.MassWireOperation(ps, pe, player);
            }
        }

        public static void AddTile(List<Point> tile, bool isTile, bool isWall, BlockType bt, bool isReplace = false, int filterTile = -1, int filterWall = -1)
        {
            if (tile == null) return;
            for (int i = 0; i < tile.Count; ++i)
            {
                tilePlace.Enqueue(new tile(tile[i].X, tile[i].Y, isTile, isWall, bt, isReplace, filterTile, filterWall));
            }
        }

        public static void DelTile(List<Point> tile, bool isTile, bool isWall, bool collectDrops = true, int filterTile = -1, int filterWall = -1)
        {
            if (tile == null) return;
            bool hasNull = false;
            Vector2 oldP = Vector2.Zero;
            Vector2 newP = Vector2.Zero;

            for (int i = 0; i < tile.Count; ++i)
            {
                tile t = new tile(tile[i].X, tile[i].Y, isTile, isWall, BlockType.Solid, false, filterTile, filterWall);

                if (canTile(t) == false) continue;

                Tile T = Main.tile[t.x, t.y];

                if (T == null)
                {
                    if (!hasNull)
                    {
                        hasNull = true;
                        oldP = new Vector2(t.x, t.y);
                    }
                    newP = new Vector2(t.x, t.y);
                    continue;
                }

                bool tileValid = isTile && T.active() && (filterTile < 0 || T.type == filterTile);
                bool wallValid = isWall && (T.wall > 0) && (filterWall < 0 || T.wall == filterWall);

                if (!tileValid && !wallValid) continue;

                t.isTile = tileValid;
                t.isWall = wallValid;

                tileKill.Enqueue(t);
            }

            if (hasNull)
            {
                Main.Pings.Add(oldP);
                Main.Pings.Add(newP);
                Main.NewText("部分方块未加载, 靠近未加载区域来加载(大致范围已在地图上标记)");
            }
        }

        public static void HandleLiquid(List<Point> points, GameMain.LiquidMode mode, bool isInfinite)
        {
            if (points == null) return;
            for (int i = 0; i < points.Count; ++i)
            {
                liquidQueue.Enqueue(new liquidTile(points[i].X, points[i].Y, mode, isInfinite));
            }
        }

        /// <summary>
        /// 批量环境改造与地形净化
        /// </summary>
        public static void HandleBiomeConversion(List<Point> points, GameMain.BiomeMode mode, bool includeWall)
        {
            if (points == null || points.Count == 0 || mode == GameMain.BiomeMode.None) return;

            int conversionType;
            string modeName;
            Microsoft.Xna.Framework.Color textColor;

            switch (mode)
            {
                case GameMain.BiomeMode.Purity:
                    conversionType = 0;
                    modeName = "纯净净化";
                    textColor = Microsoft.Xna.Framework.Color.LightGreen;
                    break;
                case GameMain.BiomeMode.Corruption:
                    conversionType = 1;
                    modeName = "腐化";
                    textColor = Microsoft.Xna.Framework.Color.MediumPurple;
                    break;
                case GameMain.BiomeMode.Hallow:
                    conversionType = 2;
                    modeName = "神圣化";
                    textColor = Microsoft.Xna.Framework.Color.Cyan;
                    break;
                case GameMain.BiomeMode.Mushroom:
                    conversionType = 3;
                    modeName = "发光蘑菇化";
                    textColor = Microsoft.Xna.Framework.Color.RoyalBlue;
                    break;
                case GameMain.BiomeMode.Crimson:
                    conversionType = 4;
                    modeName = "猩红化";
                    textColor = Microsoft.Xna.Framework.Color.Crimson;
                    break;
                case GameMain.BiomeMode.Desert:
                    conversionType = 5;
                    modeName = "沙漠化";
                    textColor = Microsoft.Xna.Framework.Color.Gold;
                    break;
                case GameMain.BiomeMode.Snow:
                    conversionType = 6;
                    modeName = "冰雪化";
                    textColor = Microsoft.Xna.Framework.Color.LightCyan;
                    break;
                default:
                    return;
            }

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            int count = 0;

            for (int i = 0; i < points.Count; i++)
            {
                Point p = points[i];
                if (p.X < 0 || p.X >= Main.maxTilesX || p.Y < 0 || p.Y >= Main.maxTilesY) continue;

                WorldGen.Convert(p.X, p.Y, conversionType, tiles: true, walls: includeWall);
                count++;

                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }

            if (count > 0)
            {
                if (Main.netMode == 1 && minX <= maxX && minY <= maxY)
                {
                    NetMessage.SendTileSquare(Main.LocalPlayer.whoAmI, minX, minY, maxX - minX + 1, maxY - minY + 1);
                }

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, Main.LocalPlayer.position);
                Terraria.CombatText.NewText(Main.LocalPlayer.getRect(), textColor, $"已{modeName} ({count} 格)", true, false);
            }
        }

        public static void AddWire(List<Point> wire, Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode toolMode)
        {
            if (wire == null) return;
            for (int i = 0; i < wire.Count; ++i)
            {
                wirePlace.Enqueue(new tile(wire[i].X, wire[i].Y, toolMode));
            }
        }

        public static void DelWire(List<Point> wire, Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode toolMode)
        {
            if (wire == null) return;
            for (int i = 0; i < wire.Count; ++i)
            {
                tile t = new tile(wire[i].X, wire[i].Y, toolMode);

                if (canTile(t) == false) continue;

                Tile T = Main.tile[t.x, t.y];
                if (T == null) continue;

                if (!(T.wire() && toolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Red)) &&
                    !(T.wire3() && toolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Green)) &&
                    !(T.wire2() && toolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Blue)) &&
                    !(T.wire4() && toolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Yellow)) &&
                    !(T.actuator() && toolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Actuator))) continue;

                wireKill.Enqueue(t);
            }
        }

        public static void AddWireLine(Vector2 ps, Vector2 pe, Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode toolMode)
        {
            if (ps.HasNaNs() || pe.HasNaNs()) return;

            Vector2 v = ps;
            ps = new Vector2(Math.Min(ps.X, pe.X), Math.Min(ps.Y, pe.Y));
            pe = new Vector2(Math.Max(v.X, pe.X), Math.Max(v.Y, pe.Y));

            Point psp = ps.ToTileCoordinates();
            Point pep = pe.ToTileCoordinates();

            if (canTile(new tile() { x = psp.X, y = psp.Y }) == false) return;
            if (canTile(new tile() { x = pep.X, y = pep.Y }) == false) return;

            int height = pep.Y - psp.Y + 1;

            for (int i = 0; i < height; ++i)
            {
                Projectile proj = new Projectile();

                proj.Center = new Vector2(ps.X, ps.Y + i * 16);
                proj.ai = new float[3];
                proj.ai[0] = pep.X;
                proj.ai[1] = psp.Y + i;

                wireLinePlaceAndKill.Enqueue(proj);
            }

            WiresUI.Settings.ToolMode = toolMode;
        }

        private static bool canTile(tile t)
        {
            return t.x >= 0 && t.x < Main.tile.GetLength(0) && t.y >= 0 && t.y < Main.tile.GetLength(1);
        }

        public static Item FirstItem_Tile(Player player)
        {
            return FirstItem_TileOrWall(player, true);
        }

        public static Item FirstItem_Wall(Player player)
        {
            return FirstItem_TileOrWall(player, false);
        }

        public static Item FirstItem_TileOrWall(Player player, bool isTile)
        {
            if (player == null) return null;

            // 0. 光标抓取物最高优先级：背包开启时用鼠标抓起材料直接框选铺设（消耗同步扣除光标堆叠）
            Item mouse = Main.mouseItem;
            if (HasItem(mouse))
            {
                if (isTile)
                {
                    if (mouse.createTile >= 0 || IsGroundCoverItem(mouse))
                    {
                        _lastFusionSource = null;
                        return mouse;
                    }
                }
                else
                {
                    if (mouse.createWall > 0)
                    {
                        _lastFusionSource = null;
                        return mouse;
                    }
                }
            }

            // 1. 手持物品
            Item item = player.HeldItem;

            if (HasItem(item))
            {
                if (isTile)
                {
                    if (item.createTile >= 0 || IsGroundCoverItem(item))
                    {
                        _lastFusionSource = null;
                        return item;
                    }
                }
                else
                {
                    if (item.createWall > 0)
                    {
                        _lastFusionSource = null;
                        return item;
                    }
                }
            }

            if (player.inventory == null) return null;

            // 2. 主背包检索
            for (int i = 0; i < player.inventory.Length; ++i)
            {
                item = player.inventory[i];
                if (HasItem(item) != true) continue;

                if (isTile)
                {
                    if (item.createTile >= 0 || IsGroundCoverItem(item))
                    {
                        _lastFusionSource = null;
                        return item;
                    }
                }
                else
                {
                    if (item.createWall > 0)
                    {
                        _lastFusionSource = null;
                        return item;
                    }
                }
            }

            // 3. Fusion 容器穿透检索（猪猪存钱罐、保险箱、虚空袋、护卫熔炉及外部容器等）
            var sources = InventoryFusionManager.GetActiveSources(player);
            for (int s = 0; s < sources.Count; s++)
            {
                var src = sources[s];
                Item[] slots = src.GetSlots(player);
                if (slots == null) continue;

                for (int i = 0; i < slots.Length; i++)
                {
                    item = slots[i];
                    if (HasItem(item) != true) continue;

                    if (isTile)
                    {
                        if (item.createTile >= 0 || IsGroundCoverItem(item))
                        {
                            _lastFusionSource = src;
                            return item;
                        }
                    }
                    else
                    {
                        if (item.createWall > 0)
                        {
                            _lastFusionSource = src;
                            return item;
                        }
                    }
                }
            }

            _lastFusionSource = null;
            return null;
        }

        /// <summary>
        /// 统一物料消耗入口：扣除物品堆叠、同步 Fusion 源持久化并累计撤销记录
        /// </summary>
        private static void ConsumeMaterial(Player player, Item item)
        {
            if (item == null || item.stack <= 0) return;
            if (ModConfig.IsConsumablesItem() == false) return;
            if (item.consumable == false) return;

            int itemType = item.type;
            item.stack -= 1;
            if (item.stack <= 0) item.TurnToAir();

            if (_lastFusionSource != null)
            {
                _fusionDirty.Add(_lastFusionSource);
                _lastFusionSource = null;
            }

            WandHistory.AccumulateConsume(itemType, 1);
        }

        /// <summary>
        /// 对批量操作期间发生变动的 Fusion 源统一触发 OnModified 持久化
        /// </summary>
        private static void FlushFusionDirty(Player player)
        {
            if (_fusionDirty == null || _fusionDirty.Count == 0) return;

            foreach (var src in _fusionDirty)
            {
                if (src == null) continue;
                try
                {
                    src.OnModified(player);
                }
                catch
                {
                    // 单个源持久化失败不影响其余源
                }
            }
            _fusionDirty.Clear();
        }

        private static bool HasItem(Item item)
        {
            if (item == null) return false;
            if (item.stack < 1) return false;
            if (item.type == ItemID.None) return false;
            return true;
        }
    }
}
