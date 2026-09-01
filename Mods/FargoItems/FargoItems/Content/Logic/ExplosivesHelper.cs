using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace FargoItems.Content.Logic
{
    public static class ExplosivesHelper
    {
        public static bool OkayToDestroyTileAt(int x, int y, bool bypassVanillaCanPlace = false)
        {
            if (!WorldGen.InWorld(x, y)) return false;
            Tile tile = Main.tile[x, y];
            if (tile == null) return false;

            if (tile.active())
            {
                int type = tile.type;
                if (type == TileID.LihzahrdBrick || type == TileID.LihzahrdAltar) return false;
                if (type == TileID.BlueDungeonBrick || type == TileID.GreenDungeonBrick || type == TileID.PinkDungeonBrick)
                {
                    if (!NPC.downedBoss3) return false;
                }
                if (type == TileID.DemonAltar) return false;
            }
            return true;
        }

        public static bool TileIsLiterallyAir(Tile tile)
        {
            return !tile.active() && tile.wall == 0 && tile.liquid == 0;
        }

        public static bool TileBelongsToMagicStorage(Tile tile)
        {
            return false;
        }

        public static void ClearEverything(int x, int y, bool sendData = true)
        {
            Tile tile = Main.tile[x, y];
            bool hadLiquid = tile.liquid != 0;
            WorldGen.KillTile(x, y, noItem: true);
            tile.ClearEverything();

            if (Main.netMode == 2)
            {
                if (hadLiquid)
                    NetMessage.sendWater(x, y);
                if (sendData)
                    NetMessage.SendTileSquare(-1, x, y, 1);
            }
        }
    }
}
