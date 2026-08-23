using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI;
using Terraria.ID;

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
            public Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode toolMode;

            public tile(int x, int y, bool isTile, bool isWall, BlockType bt, bool isReplace = false)
            {
                this.x = x;
                this.y = y;
                this.isTile = isTile;
                this.isWall = isWall;
                this.bt = bt;
                this.isReplace = isReplace;
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
                this.toolMode = toolMode;
            }
        }

        public struct liquidTile
        {
            public int x;
            public int y;
            public gameMain.LiquidMode mode;
            public bool isInfinite;

            public liquidTile(int x, int y, gameMain.LiquidMode mode, bool isInfinite)
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

        public static int Count { get; protected set; } = 0;

        public static void Update(int updateCount = -1)
        {
            if (gameMain.Wand_UpdateCount < 1) gameMain.Wand_UpdateCount = 1;
            if (updateCount < 1) updateCount = gameMain.Wand_UpdateCount;

            if (tilePlace == null) tilePlace = new Queue<tile>();
            if (tileKill == null) tileKill = new Queue<tile>();
            if (liquidQueue == null) liquidQueue = new Queue<liquidTile>();
            if (wirePlace == null) wirePlace = new Queue<tile>();
            if (wireKill == null) wireKill = new Queue<tile>();
            if (wireLinePlaceAndKill == null) wireLinePlaceAndKill = new Queue<Projectile>();

            int batchSize = Math.Max(gameMain.Wand_BatchSize, 1);
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
            if (wirePlace.Count > 0)
            {
                int processCount = Math.Min(batchSize, wirePlace.Count);
                for (int i = 0; i < processCount; i++)
                {
                    tile t = wirePlace.Dequeue();
                    placeWire(t);
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

            if (--updateCount > 0) Update(updateCount);
        }

        private static void placeTile(tile t, Player player)
        {
            if (canTile(t) == false) return;

            Tile tile = Main.tile[t.x, t.y];
            Item item = FirstItem_Tile(player);

            if (item == null || item.createTile < 0) return;

            if (tile?.active() == true) // 目标已有方块
            {
                if (tile.type == item.createTile) // 相同方块只调整坡度
                {
                    SetSlopeFor(t.x, t.y, t.bt);
                    return;
                }

                // 未开启替换模式时，绝对不破坏/覆盖已有方块
                if (!t.isReplace && !gameMain.Wand_BlockReplace)
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
                    if (item.consumable && ModConfig.IsConsumablesItem())
                    {
                        item.stack -= 1;
                        if (item.stack <= 0) item.TurnToAir();
                    }

                    SetSlopeFor(t.x, t.y, t.bt);
                    action.updateData_placeTile(t.x, t.y, item.placeStyle);
                }
            }
            else // 空格直接放置
            {
                bool v = WorldGen.PlaceTile(t.x, t.y, item.createTile, true, true, player.whoAmI, item.placeStyle);
                if (v)
                {
                    if (item.consumable && ModConfig.IsConsumablesItem())
                    {
                        item.stack -= 1;
                        if (item.stack <= 0) item.TurnToAir();
                    }

                    SetSlopeFor(t.x, t.y, t.bt);
                    action.updateData_placeTile(t.x, t.y, item.placeStyle);
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
                // 未开启替换模式时，绝对不破坏/覆盖已有背景墙
                if (!t.isReplace && !gameMain.Wand_BlockReplace)
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
                    if (item.consumable && ModConfig.IsConsumablesItem())
                    {
                        item.stack -= 1;
                        if (item.stack <= 0) item.TurnToAir();
                    }

                    WorldGen.SquareWallFrame(t.x, t.y, false);
                    action.updateData_placeWall(t.x, t.y);
                }
            }
            else
            {
                WorldGen.PlaceWall(t.x, t.y, item.createWall, true);
                if (tile?.wall == item.createWall)
                {
                    if (item.consumable && ModConfig.IsConsumablesItem())
                    {
                        item.stack -= 1;
                        if (item.stack <= 0) item.TurnToAir();
                    }

                    WorldGen.SquareWallFrame(t.x, t.y, false);
                    action.updateData_placeWall(t.x, t.y);
                }
            }
        }

        private static void killTile(tile t, Player player)
        {
            if (canTile(t) == false) return;

            Tile tile = Main.tile[t.x, t.y];
            if (tile == null) return;

            if (t.isTile && tile.active())
            {
                WorldGen.KillTile(t.x, t.y, fail: false, effectOnly: false, noItem: false);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, t.x, t.y);
            }
            if (t.isWall && tile.wall > 0)
            {
                WorldGen.KillWall(t.x, t.y, fail: false);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 2, t.x, t.y);
            }

            if (gameMain.Wand_CollectDrops && player != null)
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
                case gameMain.LiquidMode.Absorb:
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

                case gameMain.LiquidMode.Clear:
                    if (tile.liquid > 0)
                    {
                        tile.liquid = 0;
                        WorldGen.SquareTileFrame(lt.x, lt.y, true);
                        Liquid.AddWater(lt.x, lt.y);
                        NetMessage.sendWater(lt.x, lt.y);
                    }
                    break;

                case gameMain.LiquidMode.Water:
                case gameMain.LiquidMode.Lava:
                case gameMain.LiquidMode.Honey:
                case gameMain.LiquidMode.Shimmer:
                    int targetType = 0;
                    int bucketType = ItemID.WaterBucket;
                    int bottomlessType = ItemID.BottomlessBucket;

                    if (lt.mode == gameMain.LiquidMode.Water)
                    {
                        targetType = 0;
                        bucketType = ItemID.WaterBucket;
                        bottomlessType = ItemID.BottomlessBucket;
                    }
                    else if (lt.mode == gameMain.LiquidMode.Lava)
                    {
                        targetType = 1;
                        bucketType = ItemID.LavaBucket;
                        bottomlessType = ItemID.BottomlessLavaBucket;
                    }
                    else if (lt.mode == gameMain.LiquidMode.Honey)
                    {
                        targetType = 2;
                        bucketType = ItemID.HoneyBucket;
                        bottomlessType = ItemID.BottomlessHoneyBucket;
                    }
                    else if (lt.mode == gameMain.LiquidMode.Shimmer)
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
        /// 优先将物品堆叠放入玩家背包，背包满时再实体掉落，避免掉落物风暴
        /// </summary>
        private static void GiveItemToPlayer(Player player, int itemType, int count = 1)
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

            // 3. 背包全满，掉落物生成
            if (remaining > 0)
            {
                player.QuickSpawnItem(null, itemType, remaining);
            }
        }

        private static bool TryConsumeLiquidBucket(Player player, int bucketType, int bottomlessType)
        {
            if (player?.inventory == null) return false;

            // 1. 检查是否存在无底桶（无底桶不消耗数量）
            for (int i = 0; i < player.inventory.Length; i++)
            {
                Item it = player.inventory[i];
                if (it != null && it.stack > 0 && it.type == bottomlessType) return true;
            }

            // 2. 检查普通液体桶并转为空桶存入背包
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

        private static void placeWire(tile t)
        {
            if (canTile(t) == false) return;

            if (t.toolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Red))
            {
                WorldGen.PlaceWire(t.x, t.y);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 5, t.x, t.y);
            }
            if (t.toolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Green))
            {
                WorldGen.PlaceWire3(t.x, t.y);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 12, t.x, t.y);
            }
            if (t.toolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Blue))
            {
                WorldGen.PlaceWire2(t.x, t.y);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 10, t.x, t.y);
            }
            if (t.toolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Yellow))
            {
                WorldGen.PlaceWire4(t.x, t.y);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 16, t.x, t.y);
            }
            if (t.toolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Actuator))
            {
                WorldGen.PlaceActuator(t.x, t.y);
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 8, t.x, t.y);
            }
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

        public static void AddTile(List<Point> tile, bool isTile, bool isWall, BlockType bt, bool isReplace = false)
        {
            if (tile == null) return;
            for (int i = 0; i < tile.Count; ++i)
            {
                tilePlace.Enqueue(new tile(tile[i].X, tile[i].Y, isTile, isWall, bt, isReplace));
            }
        }

        public static void DelTile(List<Point> tile, bool isTile, bool isWall, bool collectDrops = true)
        {
            if (tile == null) return;
            bool hasNull = false;
            Vector2 oldP = Vector2.Zero;
            Vector2 newP = Vector2.Zero;

            for (int i = 0; i < tile.Count; ++i)
            {
                tile t = new tile(tile[i].X, tile[i].Y, isTile, isWall, BlockType.Solid);

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

                if ((!isTile || !T.active()) &&
                    (!isWall || !(T.wall > 0))) continue;

                tileKill.Enqueue(t);
            }

            if (hasNull)
            {
                Main.Pings.Add(oldP);
                Main.Pings.Add(newP);
                Main.NewText("部分方块未加载, 靠近未加载区域来加载(大致范围已在地图上标记)");
            }
        }

        public static void HandleLiquid(List<Point> points, gameMain.LiquidMode mode, bool isInfinite)
        {
            if (points == null) return;
            for (int i = 0; i < points.Count; ++i)
            {
                liquidQueue.Enqueue(new liquidTile(points[i].X, points[i].Y, mode, isInfinite));
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

        private static Item FirstItem_Tile(Player player)
        {
            return FirstItem_TileOrWall(player, true);
        }

        private static Item FirstItem_Wall(Player player)
        {
            return FirstItem_TileOrWall(player, false);
        }

        private static Item FirstItem_TileOrWall(Player player, bool isTile)
        {
            if (player == null) return null;

            Item item = player.HeldItem;

            if (HasItem(item))
            {
                if (isTile)
                {
                    if (item.createTile >= 0) return item;
                }
                else
                {
                    if (item.createWall > 0) return item;
                }
            }

            if (player.inventory == null) return null;

            for (int i = 0; i < player.inventory.Length; ++i)
            {
                item = player.inventory[i];
                if (HasItem(item) != true) continue;

                if (isTile)
                {
                    if (item.createTile >= 0) return item;
                }
                else
                {
                    if (item.createWall > 0) return item;
                }
            }

            return null;
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
