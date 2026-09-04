using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PixelArt.Content.ColorToWall
{
    internal class Get1 : IGetColorWall
    {
        private List<ColorWall> cws = null;

        public ColorWall Get(Color target)
        {
            return LookupColorSimilarWallItem(target);
        }

        public void Init(List<ColorWall> colors)
        {
            cws = colors.ToList();
            if (cws.Count < 1) throw new ArgumentException("可用颜色小于1");
        }

        //获取两个颜色的 RGB 分量差之和
        public static int ColorDifference(Color color1, Color color2)
        {
            int rDiff = Math.Abs(color1.R - color2.R);
            int gDiff = Math.Abs(color1.G - color2.G);
            int bDiff = Math.Abs(color1.B - color2.B);
            return rDiff + gDiff + bDiff;
        }

        private ColorWall LookupColorSimilarWallItem(Color color)
        {
            int difference = -1;
            ColorWall? cw = null;

            for (int i = 0; i < cws.Count; ++i)
            {
                int s = ColorDifference(color, cws[i].color);
                if (s == 0) return cws[i];

                if (difference == -1)
                {
                    difference = s;
                    cw = cws[i];
                }
                else if (difference > s)
                {
                    difference = s;
                    cw = cws[i];
                }
            }

            if (cw == null) return cws[0];
            return cw.Value;
        }
    }
}
