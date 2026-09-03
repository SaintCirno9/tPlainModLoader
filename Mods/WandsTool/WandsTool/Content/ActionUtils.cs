using Terraria;
using Terraria.ID;

namespace WandsTool.Content
{
    /// <summary>
    /// 图格与背景墙操作的轻量级网络同步工具类
    /// </summary>
    public static class ActionUtils
    {
        public static void updateData_placeTile(int x, int y, int style)
        {
            if (WorldGen.InWorld(x, y) == false) return;

            Tile tile = Main.tile[x, y];
            if (tile == null) return;

            NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 1, x, y, tile.type, style);

            if (tile.halfBrick())
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 7, x, y, 1f, 0, 0, 0);
            else if (tile.slope() > 0)
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 14, x, y, tile.slope());
        }

        public static void updateData_placeWall(int x, int y)
        {
            if (WorldGen.InWorld(x, y) == false) return;

            Tile tile = Main.tile[x, y];
            if (tile == null) return;

            if (tile.wall > 0)
            {
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 3, x, y, tile.wall);
            }
        }
    }
}
