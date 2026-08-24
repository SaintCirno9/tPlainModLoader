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
                    structure.Tiles[x, y] = TileSnapshot.FromTile(tile);

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
