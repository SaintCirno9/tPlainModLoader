using FargoItems.Content.Items.Explosives;

using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TPML.Content;

namespace FargoItems.Content.Projectiles.Explosives
{
    public class AutoHouseProj : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.timeLeft = 1;
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
        }
        public static void GetTiles(Player player, Vector2 position, out int wallType, out int tileType, out int platformStyle, out bool moddedPlatform, out string biomeName)
        {
            moddedPlatform = false;
            float tileY = position.Y / 16.0f;

            if (player.ZoneUnderworldHeight || tileY >= Main.UnderworldLayer)
            {
                wallType = WallID.ObsidianBrick;
                tileType = TileID.ObsidianBrick;
                platformStyle = 13;
                biomeName = "地狱 (Underworld)";
            }
            else if (player.ZoneSkyHeight || tileY <= (float)Main.worldSurface * 0.35f)
            {
                wallType = WallID.DiscWall;
                tileType = TileID.Sunplate;
                platformStyle = 22;
                biomeName = "太空/天岛 (Sky)";
            }
            else if (player.ZoneGlowshroom)
            {
                wallType = WallID.Mushroom;
                tileType = TileID.MushroomBlock;
                platformStyle = 18;
                biomeName = "发光蘑菇地 (Glowshroom)";
            }
            else if (player.ZoneHallow)
            {
                wallType = WallID.Pearlwood;
                tileType = TileID.Pearlwood;
                platformStyle = 3;
                biomeName = "神圣之地 (Hallow)";
            }
            else if (player.ZoneCrimson)
            {
                wallType = WallID.Shadewood;
                tileType = TileID.Shadewood;
                platformStyle = 5;
                biomeName = "猩红之地 (Crimson)";
            }
            else if (player.ZoneCorrupt)
            {
                wallType = WallID.Ebonwood;
                tileType = TileID.Ebonwood;
                platformStyle = 1;
                biomeName = "腐化之地 (Corrupt)";
            }
            else if (player.ZoneJungle)
            {
                wallType = WallID.RichMaogany;
                tileType = TileID.RichMahogany;
                platformStyle = 2;
                biomeName = "丛林 (Jungle)";
            }
            else if (player.ZoneSnow)
            {
                wallType = WallID.BorealWood;
                tileType = TileID.BorealWood;
                platformStyle = 19;
                biomeName = "雪原 (Snow)";
            }
            else if (player.ZoneDesert && !player.ZoneBeach)
            {
                wallType = WallID.Cactus;
                tileType = TileID.CactusBlock;
                platformStyle = 25;
                biomeName = "沙漠 (Desert)";
            }
            else if (player.ZoneBeach)
            {
                wallType = WallID.PalmWood;
                tileType = TileID.PalmWood;
                platformStyle = 17;
                biomeName = "海滩 (Beach)";
            }
            else
            {
                wallType = WallID.Wood;
                tileType = TileID.WoodBlock;
                platformStyle = 0;
                biomeName = "森林 (Forest)";
            }
        }
        public static void PlaceHouse(int x, int y, Vector2 position, int side, Player player)
        {
            int xPosition = (int)((side * -1) + x + position.X / 16.0f);
            int yPosition = (int)(y + position.Y / 16.0f);
            Tile tile = Main.tile[xPosition, yPosition];

            // Testing for blocks that should not be destroyed
            if (!FargoItems.Content.Logic.ExplosivesHelper.OkayToDestroyTileAt(xPosition, yPosition, true))
                return;

            GetTiles(player, position, out int wallType, out int tileType, out int platformStyle, out bool moddedPlatform, out _);

            if (x == 10 * side || x == 1 * side)
            {
                //dont act if the right tile already above (but DO replace a corner platform)
                if (y == -5 && tile.type == tileType)
                    return;

                //dont act on correct block above/below door, destroying them will break it
                if ((y == -4 || y == 0) && tile.type == tileType)
                    return;
                
                if ((y == -1 || y == -2 || y == -3) && (tile.type == TileID.ClosedDoor || tile.type == TileID.OpenDoor))
                    return;
            }
            else //for blocks besides those on the left/right edges where doors are placed, its okay to have platform as floor
            {
                //dont act if the right blocks already above
                if (y == -5 && (tile.type == TileID.Platforms || tile.type == tileType))
                    return;

                if (y == 0 && (tile.type == TileID.Platforms || tile.type == tileType))
                    return;
            }

            //doing it this way so the code still runs to place bg walls behind open door
            if (!((x == 9 * side || x == 2 * side) && (y == -1 || y == -2 || y == -3) && tile.type == TileID.OpenDoor))
                FargoItems.Content.Logic.ExplosivesHelper.ClearEverything(xPosition, yPosition);

            // Spawn walls
            if (y != -5 && y != 0 && x != (10 * side) && x != (1 * side))
            {
                WorldGen.PlaceWall(xPosition, yPosition, wallType);
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendTileSquare(-1, xPosition, yPosition, 1);
            }

            //platforms on top
            if (y == -5 && Math.Abs(x) >= 3 && Math.Abs(x) <= 5)
            {
                int type = TileID.Platforms;
                int style = 0;
                if (moddedPlatform) type = platformStyle;
                else style = platformStyle;
                WorldGen.PlaceTile(xPosition, yPosition, type, style: style);
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 1, xPosition, yPosition, type, style);
            }
            // Spawn border
            else if ((y == -5) || (y == 0) || (x == (10 * side)) || (x == (1 * side) && y == -4))
            {
                WorldGen.PlaceTile(xPosition, yPosition, tileType);
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendTileSquare(-1, xPosition, yPosition, 1);
            }
        }
        public static void GetFurniture(Player player, Vector2 position, out int doorStyle, out int chairStyle, out int tableStyle, out int torchStyle)
        {
            float tileY = position.Y / 16.0f;

            if (player.ZoneUnderworldHeight || tileY >= Main.UnderworldLayer)
            {
                doorStyle = 19;
                chairStyle = 16;
                tableStyle = 13;
                torchStyle = 7;
            }
            else if (player.ZoneSkyHeight || tileY <= (float)Main.worldSurface * 0.35f)
            {
                doorStyle = 9;
                chairStyle = 10;
                tableStyle = 7;
                torchStyle = 0;
            }
            else if (player.ZoneGlowshroom)
            {
                doorStyle = 6;
                chairStyle = 9;
                tableStyle = 27;
                torchStyle = 22;
            }
            else if (player.ZoneHallow)
            {
                doorStyle = 3;
                chairStyle = 4;
                tableStyle = 3;
                torchStyle = 20;
            }
            else if (player.ZoneCrimson)
            {
                doorStyle = 10;
                chairStyle = 11;
                tableStyle = 8;
                torchStyle = 19;
            }
            else if (player.ZoneCorrupt)
            {
                doorStyle = 1;
                chairStyle = 2;
                tableStyle = 1;
                torchStyle = 18;
            }
            else if (player.ZoneJungle)
            {
                doorStyle = 2;
                chairStyle = 3;
                tableStyle = 2;
                torchStyle = 21;
            }
            else if (player.ZoneSnow)
            {
                doorStyle = 30;
                chairStyle = 30;
                tableStyle = 28;
                torchStyle = 9;
            }
            else if (player.ZoneDesert && !player.ZoneBeach)
            {
                doorStyle = 4;
                chairStyle = 6;
                tableStyle = 30;
                torchStyle = 16;
            }
            else if (player.ZoneBeach)
            {
                doorStyle = 29;
                chairStyle = 29;
                tableStyle = 26;
                torchStyle = 17;
            }
            else
            {
                doorStyle = 0;
                chairStyle = 0;
                tableStyle = 0;
                torchStyle = 0;
            }
        }
        public static void PlaceFurniture(int x, int y, Vector2 position, int side, Player player)
        {
            int xPosition = (int)((side * -1) + x + position.X / 16.0f);
            int yPosition = (int)(y + position.Y / 16.0f);

            Tile tile = Main.tile[xPosition, yPosition];
            // Testing for blocks that should not be destroyed
            if (!FargoItems.Content.Logic.ExplosivesHelper.OkayToDestroyTileAt(xPosition, yPosition, true))
                return;

            GetFurniture(player, position, out int doorStyle, out int chairStyle, out int tableStyle, out int torchStyle);
            int style = 0;
            int type = 0;
            if (y == -1)
            {
                if (Math.Abs(x) == 1)
                {
                    if (doorStyle > TileID.Count)
                    {
                        type = doorStyle;
                        style = 0;
                    }
                    else
                    {
                        type = TileID.ClosedDoor;
                        style = doorStyle;
                    }

                    WorldGen.PlaceTile(xPosition, yPosition, type, style: style);
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendTileSquare(-1, xPosition, yPosition - 2, 1, 3);
                }

                if (x == (5 * side))
                {
                    if (chairStyle > TileID.Count)
                    {
                        type = chairStyle;
                        style = 0;
                    }
                    else
                    {
                        type = TileID.Chairs;
                        style = chairStyle;
                    }

                    WorldGen.PlaceObject(xPosition, yPosition, type, direction: side, style: style);
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 1, xPosition, yPosition, type, style);
                }

                if (x == (7 * side))
                {
                    if (tableStyle > TileID.Count)
                    {
                        type = tableStyle;
                        style = 0;
                    }
                    else
                    {
                        type = TileID.Tables;
                        style = tableStyle;
                    }


                    WorldGen.PlaceTile(xPosition, yPosition, type, style: style);
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 1, xPosition, yPosition, type, style);
                }
            }

            if (x == (7 * side) && y == -4)
            {
                if (torchStyle > TileID.Count)
                {
                    type = torchStyle;
                    style = 0;
                }
                else
                {
                    type = TileID.Torches;
                    style = torchStyle;
                }
                WorldGen.PlaceTile(xPosition, yPosition, type, style: style);
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 1, xPosition, yPosition, type, style);
            }
        }

        public static void UpdateWall(int x, int y, Vector2 position, int side, Player player)
        {
            int xPosition = (int)((side * -1) + x + position.X / 16.0f);
            int yPosition = (int)(y + position.Y / 16.0f);

            WorldGen.SquareWallFrame(xPosition, yPosition);
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendTileSquare(-1, xPosition, yPosition, 1);
        }

        public override void OnKill(int timeLeft)
        {
            Vector2 position = Projectile.Center;
            SoundEngine.PlaySound(SoundID.Item14, position);
            Player player = Main.player[Projectile.owner];
            BuildHouse(player, position);
        }

        public static void BuildHouse(Player player, Vector2 position)
        {
            if (player == null) return;
            var logger = TPML.Core.Logging.LogManager.GetLogger("AutoHouse");
            int startTileX = (int)(position.X / 16.0f);
            int startTileY = (int)(position.Y / 16.0f);
            GetTiles(player, position, out int sampleWall, out int sampleTile, out _, out _, out string biomeName);
            logger.Info($"[AutoHouse] 正在为玩家 [{player.name}] 在坐标 ({position.X:F1}, {position.Y:F1}) [图格: {startTileX}, {startTileY}] 构建房屋... 判定环境: [{biomeName}], 材质: TileID={sampleTile}, WallID={sampleWall}");

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                logger.Warn("[AutoHouse] 处于联机客户端模式，已略过本地 Tile 放置");
                return;
            }

            try
            {
                if (player.Center.X < position.X)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        for (int x = 11; x > -1; x--)
                        {
                            if (i != 2 && (x == 11 || x == 0))
                                continue;

                            for (int y = -6; y <= 1; y++)
                            {
                                if (i != 2 && (y == -6 || y == 1))
                                    continue;

                                if (i == 0)
                                    PlaceHouse(x, y, position, 1, player);
                                else if (i == 1)
                                    PlaceFurniture(x, y, position, 1, player);
                                else
                                    UpdateWall(x, y, position, 1, player);
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < 3; i++)
                    {
                        for (int x = -11; x < 1; x++)
                        {
                            if (i != 2 && (x == -11 || x == 0))
                                continue;

                            for (int y = -6; y <= 1; y++)
                            {
                                if (i != 2 && (y == -6 || y == 1))
                                    continue;

                                if (i == 0)
                                    PlaceHouse(x, y, position, -1, player);
                                else if (i == 1)
                                    PlaceFurniture(x, y, position, -1, player);
                                else
                                    UpdateWall(x, y, position, -1, player);
                            }
                        }
                    }
                }
                logger.Info("[AutoHouse] ★ 房屋结构与内部家具生成完毕！");
            }
            catch (Exception ex)
            {
                logger.Error($"[AutoHouse] 房屋构建异常: {ex.Message}", ex);
            }
        }
    }
}
