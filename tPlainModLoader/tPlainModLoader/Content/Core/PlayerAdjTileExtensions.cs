using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.WorldBuilding;
using TPML.Core.Logging;

namespace TPML.Content
{
    /// <summary>
    /// Player.adjTile 安全访问与设置扩展方法集
    /// 防御性防止 null 引用、越界或未就绪状态导致的崩溃
    /// 作者: SaintCirno9
    /// </summary>
    public static class PlayerAdjTileExtensions
    {
        private static readonly ILogger Logger = LogManager.GetLogger("PlayerAdjTile");

        /// <summary>
        /// 全量安全扫描玩家周围图格并设置 adjTile 及相关制作环境（彻底替代原版易抛 NRE 的 Player.AdjTiles）
        /// </summary>
        public static void SafeScanAdjTiles(this Player player)
        {
            if (player == null) return;

            try
            {
                if (player.adjTile == null || player.adjTile.Length < 693)
                {
                    player.adjTile = new bool[693];
                }

                Array.Clear(player.adjTile, 0, player.adjTile.Length);
                player.oldAdjWaterSource = player.adjWaterSource;
                player.adjWaterSource = false;
                player.oldAdjHoney = player.adjHoney;
                player.adjHoney = false;
                player.oldAdjLava = player.adjLava;
                player.adjLava = false;
                player.alchemyTable = false;

                if (Main.tile == null)
                {
                    return;
                }

                Rectangle tileRegion = TileReachCheckSettings.Simple.GetTileRegion(player, player.ateArtisanBread ? 4 : 0);
                tileRegion = WorldUtils.ClampToWorld(tileRegion);

                for (int x = tileRegion.Left; x <= tileRegion.Right; x++)
                {
                    for (int y = tileRegion.Top; y <= tileRegion.Bottom; y++)
                    {
                        if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY) continue;
                        Tile tile = Main.tile[x, y];
                        if (tile == null) continue;

                        if (tile.active())
                        {
                            player.SafeSetAdjTileWithEquivalents(tile.type);
                            if (TileID.Sets.CountsAsWaterForCrafting != null &&
                                tile.type < TileID.Sets.CountsAsWaterForCrafting.Length &&
                                TileID.Sets.CountsAsWaterForCrafting[tile.type])
                            {
                                player.adjWaterSource = true;
                            }
                        }
                        if (tile.liquid > 200 && tile.liquidType() == 0)
                        {
                            player.adjWaterSource = true;
                        }
                        if (tile.liquid > 200 && tile.liquidType() == 2)
                        {
                            player.adjHoney = true;
                        }
                        if (tile.liquid > 200 && tile.liquidType() == 1)
                        {
                            player.adjLava = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"SafeScanAdjTiles 发生异常，已安全拦截: {ex.Message}", ex);
            }
        }

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
