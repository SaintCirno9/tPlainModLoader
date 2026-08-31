using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace WandsTool.Content.Structure
{
    /// <summary>
    /// 选区结构扫描与快照抓取器
    /// </summary>
    public static class StructureCapture
    {
        /// <summary>
        /// 从世界指定图格矩形区域抓取结构数据
        /// </summary>
        public static StructureData Capture(Rectangle tileRect, string name = "建筑结构")
        {
            if (tileRect.Width <= 0 || tileRect.Height <= 0) return null;

            int minX = Math.Max(0, tileRect.X);
            int maxX = Math.Min(Main.tile.GetLength(0) - 1, tileRect.X + tileRect.Width - 1);
            int minY = Math.Max(0, tileRect.Y);
            int maxY = Math.Min(Main.tile.GetLength(1) - 1, tileRect.Y + tileRect.Height - 1);

            int w = maxX - minX + 1;
            int h = maxY - minY + 1;

            if (w <= 0 || h <= 0) return null;

            StructureData structure = new StructureData(w, h, name)
            {
                OriginX = w / 2,
                OriginY = h - 1 // 默认底部居中对齐，方便地基贴合
            };

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    int worldX = minX + x;
                    int worldY = minY + y;

                    Tile tile = Main.tile[worldX, worldY];
                    TileSnapshot snap = TileSnapshot.FromTile(tile);

                    // 若为打开的门（TileID 11），自动在门轴处规范化为标准闭门（TileID 10），门扇展开区域还原为空气
                    if (tile != null && tile.active() && tile.type == Terraria.ID.TileID.OpenDoor)
                    {
                        if (TPML.Content.Core.TileItemResolver.NormalizeOpenDoor(tile, out short normType, out short normFrameX, out short normFrameY, out bool isHinge))
                        {
                            snap.TileType = normType;
                            snap.TileFrameX = normFrameX;
                            snap.TileFrameY = normFrameY;
                            if (normType < 0)
                            {
                                snap.TileColor = 0;
                                snap.Slope = 0;
                            }
                        }
                    }

                    // 若关闭背景墙开关，抓取时不记录任何背景墙
                    if (!gameMain.Wand_StructureIncludeWall)
                    {
                        snap.WallType = 0;
                        snap.WallColor = 0;
                    }

                    structure.Tiles[x, y] = snap;

                    // 抓取标牌文本
                    if (tile != null && tile.active() && Main.tileSign[tile.type])
                    {
                        if (tile.frameX / 18 % 2 == 0 && tile.frameY / 18 == 0)
                        {
                            int signId = Sign.ReadSign(worldX, worldY, false);
                            if (signId >= 0 && Main.sign[signId] != null && !string.IsNullOrEmpty(Main.sign[signId].text))
                            {
                                structure.SignTexts.Add(Main.sign[signId].text);
                            }
                        }
                    }
                }
            }

            return structure;
        }
    }
}
