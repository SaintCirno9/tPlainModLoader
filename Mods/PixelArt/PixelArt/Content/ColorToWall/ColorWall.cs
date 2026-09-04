using Microsoft.Xna.Framework;

namespace PixelArt.Content.ColorToWall
{
    public struct ColorWall
    {
        public Color color;
        public int item;
        public ushort wall;
        public byte paint;

        public ColorWall(Color color, int item, ushort wall, byte paint)
        {
            this.color = color;
            this.item = item;
            this.wall = wall;
            this.paint = paint;
        }
    }
}
