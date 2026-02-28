using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace PixelArt.Content.ColorToWall
{
    internal interface IGetColorWall
    {
        void Init(List<ColorWall> colors);
        ColorWall Get(Color target);
    }
}
