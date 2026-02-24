using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Terraria;

namespace PixelArt.Content
{
    public partial class PixelArt
    {
        //获取两个颜色的 RGB 分量差之和
        public static int ColorDifference(Color color1, Color color2)
        {
            int rDiff = Math.Abs(color1.R - color2.R);
            int gDiff = Math.Abs(color1.G - color2.G);
            int bDiff = Math.Abs(color1.B - color2.B);
            return rDiff + gDiff + bDiff;
        }

        private static List<PixelInfo> LoadImgToPixelInfo(string filePath, ref int width, ref int height, CancellationToken ct)
        {
            if (File.Exists(filePath) == false)
            {
                throw new Exception($"文件[{filePath}]不存在");
            }

            List<PixelInfo> pixelInfos = new List<PixelInfo>();

            using (System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(filePath))
            {
                width = bitmap.Width;
                height = bitmap.Height;

                for (int y = 0; y < height; ++y)
                {
                    for (int x = 0; x < width; ++x)
                    {
                        System.Drawing.Color c = bitmap.GetPixel(x, y);

                        PixelInfo pi = null;

                        if (c.A == byte.MaxValue)
                        {
                            Color color = new Color(c.R, c.G, c.B, c.A);
                            pi = new PixelInfo(color, x, y);
                        }
                        
                        pixelInfos.Add(pi);
                    }
                }
            }

            for (int i = 0; i < pixelInfos.Count; ++i)
            {
                ct.ThrowIfCancellationRequested();

                if (pixelInfos[i] == null) continue;

                Item item = LookupColorSimilarWallItem(pixelInfos[i].color);
                if (item == null)
                {
                    pixelInfos[i] = null;
                    continue;
                }

                pixelInfos[i].itemId = item.type;
                pixelInfos[i].wallId = (ushort)item.createWall;
            }

            return pixelInfos;
        }

        private static Item LookupColorSimilarWallItem(Color color)
        {
            int difference = -1;
            Item item = null;

            for (int i = 1; i < wallItemIds.Count; ++i)
            {
                int createWall = wallItemIds[i].createWall;

                ushort wallId = Terraria.Map.MapHelper.wallLookup[createWall];
                Color mapColor = MapHelper.colorLookup[wallId];
                if (mapColor.A != 255) continue;

                int s = ColorDifference(color, mapColor);
                if (s == 0)
                {
                    return wallItemIds[i];
                }
                if (difference == -1)
                {
                    difference = s;
                    item = wallItemIds[i];
                    continue;
                }
                if (difference > s)
                {
                    difference = s;
                    item = wallItemIds[i];
                }
            }

            return item;
        }
    }
}
