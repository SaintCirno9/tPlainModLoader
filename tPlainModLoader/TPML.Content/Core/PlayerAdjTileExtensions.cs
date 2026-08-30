using System;
using Terraria;

namespace TPML.Content
{
    /// <summary>
    /// Player.adjTile 安全访问与设置扩展方法集
    /// 防御性防止 null 引用、越界或未就绪状态导致的崩溃
    /// 作者: SaintCirno9
    /// </summary>
    public static class PlayerAdjTileExtensions
    {
        /// <summary>
        /// 安全获取/判断玩家是否处于指定制作站图格环境（等价于 player.adjTile[tileType]）
        /// </summary>
        public static bool SafeGetAdjTile(this Player player, int tileType)
        {
            if (player?.adjTile == null) return false;
            if (tileType < 0 || tileType >= player.adjTile.Length) return false;
            return player.adjTile[tileType];
        }

        /// <summary>
        /// 安全判断玩家是否处于指定制作站图格环境
        /// </summary>
        public static bool SafeHasAdjTile(this Player player, int tileType)
        {
            return SafeGetAdjTile(player, tileType);
        }

        /// <summary>
        /// 安全设置玩家制作站图格标志
        /// </summary>
        public static void SafeSetAdjTile(this Player player, int tileType, bool value = true)
        {
            if (player?.adjTile == null) return;
            if (tileType < 0 || tileType >= player.adjTile.Length) return;
            player.adjTile[tileType] = value;
        }

        /// <summary>
        /// 安全设置玩家制作站图格标志，并自动递归向下兼任低阶制作站（带越界、null 保护与炼金桌环境识别）
        /// </summary>
        public static void SafeSetAdjTileWithEquivalents(this Player player, int tileType)
        {
            if (player?.adjTile == null) return;
            if (tileType < 0 || tileType >= player.adjTile.Length) return;

            player.adjTile[tileType] = true;
            if (tileType == 355 || tileType == 699)
            {
                player.alchemyTable = true;
            }

            if (Recipe.TileCountsAs != null && tileType < Recipe.TileCountsAs.Length)
            {
                var list = Recipe.TileCountsAs[tileType];
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        player.SafeSetAdjTileWithEquivalents(list[i]);
                    }
                }
            }
        }
    }
}
