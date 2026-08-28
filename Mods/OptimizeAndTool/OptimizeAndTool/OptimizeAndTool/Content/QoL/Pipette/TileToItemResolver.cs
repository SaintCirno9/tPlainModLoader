using Terraria;
using TPML.Content.Core;

namespace OptimizeAndTool.Content.QoL.Pipette
{
    /// <summary>
    /// 吸管图格与背景墙到物品的反向智能映射解析器
    /// 委托底层 TPML.Content.Core.TileItemResolver 统一高性能引擎
    /// 作者: SaintCirno9
    /// </summary>
    public static class TileToItemResolver
    {
        /// <summary>
        /// 初始化反向映射字典与缓存
        /// </summary>
        public static void Initialize()
        {
            TPML.Content.Core.TileItemResolver.Initialize();
        }

        /// <summary>
        /// 根据图格信息与玩家背包解析对应的放置物品 ID（支持开门/关门、变种家具、草种子、木材、宝石等）
        /// </summary>
        public static int ResolveTileOrWallToItemId(Tile tile, Player player, bool allowWall)
        {
            return TPML.Content.Core.TileItemResolver.ResolveTileOrWallToItemId(tile, player, allowWall);
        }

        /// <summary>
        /// 利用 TileObjectData 计算物块的样式 Style
        /// </summary>
        public static int CalculateTileStyle(Tile tile)
        {
            if (tile == null) return 0;
            return TPML.Content.Core.TileItemResolver.CalculateTileStyle(tile.type, tile.frameX, tile.frameY);
        }
    }
}
