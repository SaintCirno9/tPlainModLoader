using Microsoft.Xna.Framework;

namespace PixelArt.Content.ColorToWall
{
    public struct ColorWall
    {
        public byte R => color.R;
        public byte G => color.G;
        public byte B => color.B;
        public Color color;
        public int item;
        public ushort wall;

        public ColorWall(Color color, int item, ushort wall)
        {
            this.color = color;
            this.item = item;
            this.wall = wall;
        }
    }
}
