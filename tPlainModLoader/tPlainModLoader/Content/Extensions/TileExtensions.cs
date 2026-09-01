using Terraria;

namespace TPML.Content
{
    /// <summary>
    /// 对齐 tML 的 Tile 便捷方法
    /// 作者: SaintCirno9
    /// </summary>
    public static class TileExtensions
    {
        public static ushort TileType(this Tile tile) => tile.type;
        public static void TileType(this Tile tile, ushort type) => tile.type = type;
        public static ushort WallType(this Tile tile) => tile.wall;
        public static void WallType(this Tile tile, ushort wall) => tile.wall = wall;
        public static short TileFrameX(this Tile tile) => tile.frameX;
        public static void TileFrameX(this Tile tile, short frameX) => tile.frameX = frameX;
        public static short TileFrameY(this Tile tile) => tile.frameY;
        public static void TileFrameY(this Tile tile, short frameY) => tile.frameY = frameY;
        public static bool HasTile(this Tile tile) => tile.active();
        public static void HasTile(this Tile tile, bool active) => tile.active(active);
        public static bool IsActuated(this Tile tile) => tile.inActive();
        public static void IsActuated(this Tile tile, bool inActive) => tile.inActive(inActive);
        public static byte LiquidAmount(this Tile tile) => tile.liquid;
        public static void LiquidAmount(this Tile tile, byte liquid) => tile.liquid = liquid;
        public static bool HasUnactuatedTile(this Tile tile) => tile.active() && !tile.inActive();
    }
}
