using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;

namespace WandsTool.Content
{
    /// <summary>
    /// 单格操作前快照（物块类型、坡度/半砖、背景墙、液体、电线与油漆，供撤销时完整还原）
    /// 作者: SaintCirno9
    /// </summary>
    public struct WandTileSnapshot
    {
        public int X;
        public int Y;
        public bool WasActive;
        public bool WasInActive;
        public ushort TileType;
        public short FrameX;
        public short FrameY;
        public byte Slope;          // 0=实心, 1-4=四种斜坡, 5=半砖
        public byte TileColor;      // 物块油漆
        public ushort Wall;
        public int WallFrameX;
        public int WallFrameY;
        public byte WallColor;      // 墙壁油漆
        public byte Liquid;
        public byte LiquidType;
        public bool RedWire;
        public bool GreenWire;
        public bool BlueWire;
        public bool YellowWire;
        public bool Actuator;
    }

    /// <summary>
    /// 施工一键撤销管理系统 (Undo Stack)。
    /// 每次魔杖批量操作（放置/破坏/液体）在入队前抓取选区全量"操作前"快照，
    /// 队列实际处理完成后自动归档为一条历史记录（上限 30 步），
    /// 撤销时逐格还原世界并智能返还消耗的物料。
    /// 作者: SaintCirno9
    /// </summary>
    public static class WandHistory
    {
        /// <summary>
        /// 单次操作历史记录：操作前快照 + 实际消耗物料清单
        /// </summary>
        public class WandActionRecord
        {
            public List<WandTileSnapshot> Tiles = new List<WandTileSnapshot>();
            public Dictionary<int, int> ConsumedItems = new Dictionary<int, int>();
        }

        private const int MaxSteps = 30;
        private static readonly LinkedList<WandActionRecord> History = new LinkedList<WandActionRecord>();
        private static WandActionRecord _active = null;

        /// <summary>
        /// 当前是否存在尚未归档的活动操作
        /// </summary>
        public static bool HasActive => _active != null;

        /// <summary>
        /// 当前历史步数
        /// </summary>
        public static int StepCount => History.Count;

        /// <summary>
        /// 清空全部历史与活动记录（关闭魔杖时调用）
        /// </summary>
        public static void Clear()
        {
            History.Clear();
            _active = null;
        }

        /// <summary>
        /// 开始记录一次新操作：为选区每个坐标抓取"操作前"世界状态快照
        /// </summary>
        public static void BeginRecord(Player player, List<Point> points)
        {
            if (points == null || points.Count == 0) return;
            CheckFinalize(player); // 先归档上一个尚未完成的记录

            WandActionRecord record = new WandActionRecord();
            int tileCountW = Main.tile.GetLength(0);
            int tileCountH = Main.tile.GetLength(1);

            for (int i = 0; i < points.Count; i++)
            {
                Point p = points[i];
                if (p.X < 0 || p.X >= tileCountW || p.Y < 0 || p.Y >= tileCountH) continue;

                Tile tile = Main.tile[p.X, p.Y];
                if (tile == null) continue; // 未加载区域不记录，避免撤销时越界写回

                WandTileSnapshot snap = new WandTileSnapshot
                {
                    X = p.X,
                    Y = p.Y,
                    WasActive = tile.active(),
                    WasInActive = tile.inActive(),
                    TileType = tile.type,
                    FrameX = tile.frameX,
                    FrameY = tile.frameY,
                    Slope = tile.halfBrick() ? (byte)5 : tile.slope(),
                    TileColor = tile.color(),
                    Wall = tile.wall,
                    WallFrameX = tile.wallFrameX(),
                    WallFrameY = tile.wallFrameY(),
                    WallColor = tile.wallColor(),
                    Liquid = tile.liquid,
                    LiquidType = tile.liquidType(),
                    RedWire = tile.wire(),
                    GreenWire = tile.wire3(),
                    BlueWire = tile.wire2(),
                    YellowWire = tile.wire4(),
                    Actuator = tile.actuator(),
                };
                record.Tiles.Add(snap);
            }

            _active = record;
        }

        /// <summary>
        /// 累计记录当前活动操作实际消耗的物料（由 WandAction 扣费处调用）
        /// </summary>
        public static void AccumulateConsume(int itemType, int count)
        {
            if (_active == null || itemType <= 0 || count <= 0) return;
            if (_active.ConsumedItems.TryGetValue(itemType, out int cur))
            {
                _active.ConsumedItems[itemType] = cur + count;
            }
            else
            {
                _active.ConsumedItems[itemType] = count;
            }
        }

        /// <summary>
        /// 当操作队列处理完毕时归档活动记录；若世界无实际变化则直接丢弃
        /// </summary>
        public static void CheckFinalize(Player player)
        {
            if (_active == null) return;

            bool changed = _active.ConsumedItems.Count > 0;
            if (!changed)
            {
                for (int i = 0; i < _active.Tiles.Count; i++)
                {
                    if (IsDifferent(_active.Tiles[i]))
                    {
                        changed = true;
                        break;
                    }
                }
            }

            WandActionRecord record = _active;
            _active = null;

            if (!changed) return;

            if (History.Count >= MaxSteps)
            {
                History.RemoveFirst();
            }
            History.AddLast(record);
        }

        /// <summary>
        /// 执行一步撤销：还原世界、广播多人同步并返还消耗物料
        /// </summary>
        /// <returns>-2 = 上一操作仍在处理中; -1 = 无历史; 其他 = 还原格数</returns>
        public static int Undo(Player player)
        {
            if (_active != null)
            {
                return -2; // 上一次操作队列尚未处理完，强行撤销会与批处理冲突
            }
            if (History.Count == 0)
            {
                return -1;
            }

            WandActionRecord record = History.Last.Value;
            History.RemoveLast();

            // 1. 逐格还原世界并计算包围盒（用于多人广播）
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            for (int i = 0; i < record.Tiles.Count; i++)
            {
                WandTileSnapshot snap = record.Tiles[i];
                Restore(snap);
                if (snap.X < minX) minX = snap.X;
                if (snap.Y < minY) minY = snap.Y;
                if (snap.X > maxX) maxX = snap.X;
                if (snap.Y > maxY) maxY = snap.Y;
            }

            if (Main.netMode == 1 && player != null && maxX >= minX && maxY >= minY)
            {
                NetMessage.SendTileSquare(player.whoAmI, minX, minY, maxX - minX + 1, maxY - minY + 1);
            }

            // 2. 智能物料回滚：把本次操作实际消耗的物块/墙壁/桶返还给玩家（主背包 -> Fusion 容器 -> 掉落）
            if (player != null)
            {
                foreach (var kvp in record.ConsumedItems)
                {
                    WandAction.GiveItemToPlayer(player, kvp.Key, kvp.Value);
                }
            }

            return record.Tiles.Count;
        }

        /// <summary>
        /// 比较当前世界格与快照是否不同（用于判断操作是否有实际效果）
        /// </summary>
        private static bool IsDifferent(WandTileSnapshot snap)
        {
            if (snap.X < 0 || snap.X >= Main.tile.GetLength(0) || snap.Y < 0 || snap.Y >= Main.tile.GetLength(1)) return false;

            Tile tile = Main.tile[snap.X, snap.Y];
            if (tile == null) return true;

            if (tile.active() != snap.WasActive) return true;
            if (snap.WasActive)
            {
                if (tile.type != snap.TileType) return true;
                if (tile.frameX != snap.FrameX || tile.frameY != snap.FrameY) return true;
                byte currentSlope = tile.halfBrick() ? (byte)5 : tile.slope();
                if (currentSlope != snap.Slope) return true;
                if (tile.inActive() != snap.WasInActive) return true;
                if (tile.color() != snap.TileColor) return true;
            }
            if (tile.wall != snap.Wall) return true;
            if (tile.wallColor() != snap.WallColor) return true;
            if (tile.liquid != snap.Liquid || tile.liquidType() != snap.LiquidType) return true;
            if (tile.wire() != snap.RedWire || tile.wire3() != snap.GreenWire ||
                tile.wire2() != snap.BlueWire || tile.wire4() != snap.YellowWire ||
                tile.actuator() != snap.Actuator) return true;
            return false;
        }

        /// <summary>
        /// 将快照写回世界格（全新 Tile 重建，保证彻底还原）
        /// </summary>
        private static void Restore(WandTileSnapshot snap)
        {
            if (snap.X < 0 || snap.X >= Main.tile.GetLength(0) || snap.Y < 0 || snap.Y >= Main.tile.GetLength(1)) return;

            Tile tile = new Tile();

            if (snap.WasActive)
            {
                tile.active(true);
                tile.type = snap.TileType;
                tile.frameX = snap.FrameX;
                tile.frameY = snap.FrameY;
                tile.inActive(snap.WasInActive);
                if (snap.Slope == 5)
                {
                    tile.slope(0);
                    tile.halfBrick(true);
                }
                else
                {
                    tile.halfBrick(false);
                    tile.slope(snap.Slope);
                }
                tile.color(snap.TileColor);
            }

            if (snap.Wall != 0)
            {
                tile.wall = snap.Wall;
                tile.wallFrameX(snap.WallFrameX);
                tile.wallFrameY(snap.WallFrameY);
                tile.wallColor(snap.WallColor);
            }

            tile.liquid = snap.Liquid;
            tile.liquidType(snap.LiquidType);
            tile.wire(snap.RedWire);
            tile.wire2(snap.BlueWire);
            tile.wire3(snap.GreenWire);
            tile.wire4(snap.YellowWire);
            tile.actuator(snap.Actuator);

            Main.tile[snap.X, snap.Y] = tile;
            WorldGen.SquareTileFrame(snap.X, snap.Y, true);
        }
    }
}