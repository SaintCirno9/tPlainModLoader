using System;

namespace FargoItems.Content.Logic
{
    /// <summary>
    /// AutoHouse 环境材质与家具风格主题
    /// </summary>
    public class AutoHouseTheme
    {
        public string BiomeName { get; set; } = "森林 (Forest)";
        public int WallType { get; set; }
        public int TileType { get; set; }
        public int PlatformStyle { get; set; }
        public int DoorStyle { get; set; }
        public int ChairStyle { get; set; }
        public int TableStyle { get; set; }
        public int TorchStyle { get; set; }
    }

    /// <summary>
    /// 房屋图格角色枚举
    /// </summary>
    public enum HouseCellRole
    {
        None = 0,
        Floor,          // 实体地板
        Ceiling,        // 实体天花板
        CeilingPlatform,// 天花板中央平台
        LeftWall,       // 左外墙
        RightWall,      // 右外墙
        Door,           // 门 (高3格)
        Chair,          // 椅子 (高2格，宽1格)
        Table,          // 桌子 (高2格，宽2格)
        Torch,          // 照明火把 (天花板下方中心)
        InteriorSpace   // 房屋内部活动空间 (带背景墙)
    }

    /// <summary>
    /// AutoHouse 建筑规划器与几何拓扑模型
    /// 房屋规格：宽 10 格，高 6 格（内部空间 8x4 = 32 格，严格满足原版 NPC 房屋 >= 30 格入住标准）
    /// 作者: SaintCirno9
    /// </summary>
    public static class AutoHouseBuildPlan
    {
        public const int HouseWidth = 10;
        public const int HouseHeight = 6;
        public const int InteriorWidth = 8;
        public const int InteriorHeight = 4;
        public const int InteriorArea = InteriorWidth * InteriorHeight; // 32 格

        /// <summary>
        /// 解析环境生物群落材质与家具主题
        /// </summary>
        public static AutoHouseTheme ResolveTheme(
            bool isUnderworld,
            bool isSky,
            bool isGlowshroom,
            bool isHallow,
            bool isCrimson,
            bool isCorrupt,
            bool isJungle,
            bool isSnow,
            bool isDesert,
            bool isBeach)
        {
            var theme = new AutoHouseTheme();

            if (isUnderworld)
            {
                theme.BiomeName = "地狱 (Underworld)";
                theme.WallType = 14;      // WallID.ObsidianBrick
                theme.TileType = 75;      // TileID.ObsidianBrick
                theme.PlatformStyle = 13;
                theme.DoorStyle = 19;
                theme.ChairStyle = 16;
                theme.TableStyle = 13;
                theme.TorchStyle = 7;
            }
            else if (isSky)
            {
                theme.BiomeName = "太空/天岛 (Sky)";
                theme.WallType = 82;      // WallID.DiscWall
                theme.TileType = 189;     // TileID.Sunplate
                theme.PlatformStyle = 22;
                theme.DoorStyle = 27;
                theme.ChairStyle = 24;
                theme.TableStyle = 22;
                theme.TorchStyle = 18;
            }
            else if (isGlowshroom)
            {
                theme.BiomeName = "发光蘑菇地 (Glowshroom)";
                theme.WallType = 74;      // WallID.Mushroom
                theme.TileType = 190;     // TileID.MushroomBlock
                theme.PlatformStyle = 18;
                theme.DoorStyle = 20;
                theme.ChairStyle = 18;
                theme.TableStyle = 27;
                theme.TorchStyle = 20;
            }
            else if (isHallow)
            {
                theme.BiomeName = "神圣之地 (Hallow)";
                theme.WallType = 28;      // WallID.Pearlwood
                theme.TileType = 171;     // TileID.Pearlwood
                theme.PlatformStyle = 3;
                theme.DoorStyle = 4;
                theme.ChairStyle = 4;
                theme.TableStyle = 4;
                theme.TorchStyle = 21;
            }
            else if (isCrimson)
            {
                theme.BiomeName = "猩红之地 (Crimson)";
                theme.WallType = 81;      // WallID.Shadewood
                theme.TileType = 226;     // TileID.Shadewood
                theme.PlatformStyle = 5;
                theme.DoorStyle = 6;
                theme.ChairStyle = 6;
                theme.TableStyle = 5;
                theme.TorchStyle = 14;
            }
            else if (isCorrupt)
            {
                theme.BiomeName = "腐化之地 (Corrupt)";
                theme.WallType = 3;       // WallID.Ebonwood
                theme.TileType = 152;     // TileID.Ebonwood
                theme.PlatformStyle = 1;
                theme.DoorStyle = 2;
                theme.ChairStyle = 2;
                theme.TableStyle = 2;
                theme.TorchStyle = 13;
            }
            else if (isJungle)
            {
                theme.BiomeName = "丛林 (Jungle)";
                theme.WallType = 27;      // WallID.RichMaogany
                theme.TileType = 151;     // TileID.RichMahogany
                theme.PlatformStyle = 2;
                theme.DoorStyle = 3;
                theme.ChairStyle = 3;
                theme.TableStyle = 3;
                theme.TorchStyle = 19;
            }
            else if (isSnow)
            {
                theme.BiomeName = "雪原 (Snow)";
                theme.WallType = 80;      // WallID.BorealWood
                theme.TileType = 225;     // TileID.BorealWood
                theme.PlatformStyle = 19;
                theme.DoorStyle = 24;
                theme.ChairStyle = 20;
                theme.TableStyle = 19;
                theme.TorchStyle = 9;
            }
            else if (isDesert && !isBeach)
            {
                theme.BiomeName = "沙漠 (Desert)";
                theme.WallType = 79;      // WallID.Cactus
                theme.TileType = 224;     // TileID.CactusBlock
                theme.PlatformStyle = 25;
                theme.DoorStyle = 7;
                theme.ChairStyle = 7;
                theme.TableStyle = 6;
                theme.TorchStyle = 15;
            }
            else if (isBeach)
            {
                theme.BiomeName = "海滩 (Beach)";
                theme.WallType = 78;      // WallID.PalmWood
                theme.TileType = 223;     // TileID.PalmWood
                theme.PlatformStyle = 17;
                theme.DoorStyle = 21;
                theme.ChairStyle = 19;
                theme.TableStyle = 17;
                theme.TorchStyle = 18;
            }
            else
            {
                theme.BiomeName = "森林 (Forest)";
                theme.WallType = 4;       // WallID.Wood
                theme.TileType = 30;      // TileID.WoodBlock
                theme.PlatformStyle = 0;
                theme.DoorStyle = 0;
                theme.ChairStyle = 0;
                theme.TableStyle = 0;
                theme.TorchStyle = 0;
            }

            return theme;
        }

        /// <summary>
        /// 根据局部坐标获取对应图格的角色
        /// localX: [1, 10], localY: [-5, 0] (y=0 为地板, y=-5 为天花板)
        /// </summary>
        public static HouseCellRole GetCellRole(int localX, int localY, int side)
        {
            if (localX < 1 || localX > 10 || localY < -5 || localY > 0)
            {
                return HouseCellRole.None;
            }

            // 地板
            if (localY == 0)
            {
                return HouseCellRole.Floor;
            }

            // 天花板
            if (localY == -5)
            {
                if (localX >= 3 && localX <= 5)
                {
                    return HouseCellRole.CeilingPlatform;
                }
                return HouseCellRole.Ceiling;
            }

            // 门 (高3格: -3, -2, -1)
            if ((localX == 1 || localX == 10) && localY >= -3 && localY <= -1)
            {
                return HouseCellRole.Door;
            }

            // 门上方的实体方块
            if ((localX == 1 || localX == 10) && localY == -4)
            {
                return (localX == 1) ? HouseCellRole.LeftWall : HouseCellRole.RightWall;
            }

            // 家具：火把 (x=5, y=-4)
            if (localX == 5 && localY == -4)
            {
                return HouseCellRole.Torch;
            }

            // 家具：桌子 (x=5..6, y=-2..-1)
            if ((localX == 5 || localX == 6) && (localY == -2 || localY == -1))
            {
                return HouseCellRole.Table;
            }

            // 家具：椅子 (x=4, y=-2..-1)
            if (localX == 4 && (localY == -2 || localY == -1))
            {
                return HouseCellRole.Chair;
            }

            // 内部其余空余空间（带背景墙）
            return HouseCellRole.InteriorSpace;
        }

        /// <summary>
        /// 获取房屋在世界图格坐标系中的绝对边界
        /// </summary>
        public static (int minX, int maxX, int minY, int maxY) GetHouseBounds(int originTileX, int originTileY, int side)
        {
            int startX = (side * -1) + originTileX;
            int x1 = startX + (1 * side);
            int x2 = startX + (10 * side);

            int minX = Math.Min(x1, x2);
            int maxX = Math.Max(x1, x2);
            int minY = originTileY - 5;
            int maxY = originTileY;

            return (minX, maxX, minY, maxY);
        }
    }
}
