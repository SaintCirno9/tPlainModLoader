using Terraria;

namespace Fargowiltas.Utilities.Extensions
{
    public static class PlayerExtensions
    {
        public static bool IsTileWithinRange(this Player player, int x, int y)
        {
            int extraRange = player.HeldItem.tileBoost;

            bool left = player.Left.ToTileCoordinates().X - Player.tileRangeX - extraRange <= x;
            bool right = player.Right.ToTileCoordinates().X + Player.tileRangeX + extraRange - 1f >= x;
            bool top = player.Top.ToTileCoordinates().Y - Player.tileRangeY - extraRange <= y;
            bool bottom = player.Bottom.ToTileCoordinates().Y + Player.tileRangeY + extraRange - 2f >= y;

            return left && right && top && bottom;
        }
    }
}
