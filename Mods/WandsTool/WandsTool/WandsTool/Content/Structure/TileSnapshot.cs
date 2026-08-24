using System;
using Terraria;

namespace WandsTool.Content.Structure
{
    /// <summary>
    /// 单个图格的完整状态快照数据结构
    /// </summary>
    [Serializable]
    public struct TileSnapshot
    {
        public short TileType;      // 物块类型（-1 表示无物块/空气）
        public short WallType;      // 背景墙类型（0 表示无墙）
        public short TileFrameX;    // 物块纹理帧 X
        public short TileFrameY;    // 物块纹理帧 Y
        public short WallFrameX;    // 背景墙纹理帧 X
        public short WallFrameY;    // 背景墙纹理帧 Y
        public byte TileColor;      // 物块油漆颜色
        public byte WallColor;      // 墙壁油漆颜色
        public byte Slope;          // 坡度 (0=实体, 1=左下, 2=右下, 3=左上, 4=右上, 5=半砖)
        public bool RedWire;        // 红线
        public bool GreenWire;      // 绿线
        public bool BlueWire;       // 蓝线
        public bool YellowWire;     // 黄线
        public bool Actuator;       // 促动器
        public bool InActive;       // 是否已被虚化促动 (inActive)
        public byte Coating;        // 涂层 (夜光/发光/回声隐形涂层)

        public bool HasTile => TileType >= 0;
        public bool HasWall => WallType > 0;
        public bool HalfBlock => Slope == 5;

        /// <summary>
        /// 判断图格是否为杂草、野花、高草、藤蔓、自然碎屑等环境附着物（蓝图框选时自动过滤）
        /// </summary>
        public static bool IsIgnoredFoliage(int tileType)
        {
            if (tileType < 0) return false;

            // 1. 挥砍即可清除的自然杂草植被（草、高草、野花、藤蔓、苔藓植被、水草等）
            if (tileType < Main.tileCut.Length && Main.tileCut[tileType])
            {
                return true;
            }

            // 2. 地表自然附着的散落小碎石、钟乳石、野蘑菇等非建筑植被
            if (tileType == 5 || // 普通野蘑菇
                tileType == 71 || // 发光野蘑菇
                tileType == Terraria.ID.TileID.Stalactite ||
                tileType == Terraria.ID.TileID.SmallPiles ||
                tileType == Terraria.ID.TileID.LargePiles ||
                tileType == Terraria.ID.TileID.LargePiles2 ||
                tileType == Terraria.ID.TileID.Seaweed ||
                tileType == Terraria.ID.TileID.Cattail ||
                tileType == Terraria.ID.TileID.LilyPad)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 从世界图格采样构建快照（自动忽略自然杂草与环境附着物）
        /// </summary>
        public static TileSnapshot FromTile(Tile tile)
        {
            if (tile == null)
            {
                return new TileSnapshot { TileType = -1, WallType = 0 };
            }

            bool hasTile = tile.active() && !IsIgnoredFoliage(tile.type);

            TileSnapshot snapshot = new TileSnapshot
            {
                TileType = hasTile ? (short)tile.type : (short)-1,
                WallType = (short)tile.wall,
                TileFrameX = hasTile ? tile.frameX : (short)0,
                TileFrameY = hasTile ? tile.frameY : (short)0,
                WallFrameX = (short)tile.wallFrameX(),
                WallFrameY = (short)tile.wallFrameY(),
                TileColor = hasTile ? tile.color() : (byte)0,
                WallColor = tile.wallColor(),
                Slope = (byte)(hasTile ? (tile.halfBrick() ? 5 : (int)tile.slope()) : 0),
                RedWire = tile.wire(),
                GreenWire = tile.wire3(),
                BlueWire = tile.wire2(),
                YellowWire = tile.wire4(),
                Actuator = tile.actuator(),
                InActive = tile.inActive(),
                Coating = (byte)((tile.fullbrightBlock() ? 1 : 0) |
                                 (tile.invisibleBlock() ? 2 : 0) |
                                 (tile.fullbrightWall() ? 4 : 0) |
                                 (tile.invisibleWall() ? 8 : 0))
            };

            return snapshot;
        }

        /// <summary>
        /// 将水平翻转应用于自身坡度与半砖
        /// </summary>
        public void FlipSlopeHorizontal()
        {
            switch (Slope)
            {
                case 1: Slope = 2; break; // 左下 -> 右下 (SlopeDownRight <-> SlopeDownLeft)
                case 2: Slope = 1; break; // 右下 -> 左下
                case 3: Slope = 4; break; // 左上 -> 右上 (SlopeUpRight <-> SlopeUpLeft)
                case 4: Slope = 3; break; // 右上 -> 左上
                case 5: break;            // 半砖水平对称，保持 5
                case 6: break;            // 上半砖保持 6
            }
        }

        /// <summary>
        /// 将垂直翻转应用于自身坡度（精准区分普通方块与平台）
        /// </summary>
        public void FlipSlopeVertical(int tileType = -1)
        {
            bool isPlatform = tileType == Terraria.ID.TileID.Platforms ||
                              (tileType >= 0 && tileType < Terraria.ID.TileID.Sets.Platforms.Length && Terraria.ID.TileID.Sets.Platforms[tileType]);

            if (isPlatform)
            {
                // 平台垂直翻转：普通顶部(0) <-> 半砖底部(5)，楼梯(1 <-> 2)
                if (Slope == 0) Slope = 5;
                else if (Slope == 5) Slope = 0;
                else if (Slope == 1) Slope = 2;
                else if (Slope == 2) Slope = 1;
            }
            else
            {
                // 普通实体方块垂直翻转
                switch (Slope)
                {
                    case 1: Slope = 3; break; // 左下 -> 左上
                    case 3: Slope = 1; break; // 左上 -> 左下
                    case 2: Slope = 4; break; // 右下 -> 右上
                    case 4: Slope = 2; break; // 右上 -> 右下
                    case 5: Slope = 6; break; // 下半砖 -> 上半砖
                    case 6: Slope = 5; break; // 上半砖 -> 下半砖
                }
            }
        }
    }
}
