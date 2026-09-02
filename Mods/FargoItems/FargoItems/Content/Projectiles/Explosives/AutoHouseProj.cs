using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TPML.Content;
using FargoItems.Content.Logic;

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

            bool isUnderworld = player.ZoneUnderworldHeight || tileY >= Main.UnderworldLayer;
            bool isSky = player.ZoneSkyHeight || tileY <= (float)Main.worldSurface * 0.35f;

            var theme = AutoHouseBuildPlan.ResolveTheme(
                isUnderworld,
                isSky,
                player.ZoneGlowshroom,
                player.ZoneHallow,
                player.ZoneCrimson,
                player.ZoneCorrupt,
                player.ZoneJungle,
                player.ZoneSnow,
                player.ZoneDesert,
                player.ZoneBeach);

            wallType = theme.WallType;
            tileType = theme.TileType;
            platformStyle = theme.PlatformStyle;
            biomeName = theme.BiomeName;
        }

        public static void GetFurniture(Player player, Vector2 position, out int doorStyle, out int chairStyle, out int tableStyle, out int torchStyle)
        {
            float tileY = position.Y / 16.0f;

            bool isUnderworld = player.ZoneUnderworldHeight || tileY >= Main.UnderworldLayer;
            bool isSky = player.ZoneSkyHeight || tileY <= (float)Main.worldSurface * 0.35f;

            var theme = AutoHouseBuildPlan.ResolveTheme(
                isUnderworld,
                isSky,
                player.ZoneGlowshroom,
                player.ZoneHallow,
                player.ZoneCrimson,
                player.ZoneCorrupt,
                player.ZoneJungle,
                player.ZoneSnow,
                player.ZoneDesert,
                player.ZoneBeach);

            doorStyle = theme.DoorStyle;
            chairStyle = theme.ChairStyle;
            tableStyle = theme.TableStyle;
            torchStyle = theme.TorchStyle;
        }

        public override void AI()
        {
            Projectile.Kill();
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
            int originX = (int)(position.X / 16.0f);
            int originY = (int)(position.Y / 16.0f);

            GetTiles(player, position, out int wallType, out int tileType, out int platformStyle, out _, out string biomeName);
            GetFurniture(player, position, out int doorStyle, out int chairStyle, out int tableStyle, out int torchStyle);

            logger.Info($"[AutoHouse] 正在为玩家 [{player.name}] 在坐标 ({position.X:F1}, {position.Y:F1}) [图格: {originX}, {originY}] 构建房屋... 环境: [{biomeName}], 材质: TileID={tileType}, WallID={wallType}");

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                logger.Warn("[AutoHouse] 处于联机客户端模式，已略过本地 Tile 放置");
                return;
            }

            int side = player.Center.X < position.X ? 1 : -1;
            var (minX, maxX, minY, maxY) = AutoHouseBuildPlan.GetHouseBounds(originX, originY, side);

            try
            {
                // 阶段 1：安全清空房屋覆盖矩形区域内的一切图格与流体
                for (int gx = minX - 1; gx <= maxX + 1; gx++)
                {
                    for (int gy = minY - 1; gy <= maxY + 1; gy++)
                    {
                        if (!WorldGen.InWorld(gx, gy)) continue;
                        if (ExplosivesHelper.OkayToDestroyTileAt(gx, gy, true))
                        {
                            if (gx >= minX && gx <= maxX && gy >= minY && gy <= maxY)
                            {
                                ExplosivesHelper.ClearEverything(gx, gy, false);
                            }
                        }
                    }
                }

                // 阶段 2：铺设实体外框（地板、天花板、外墙立柱）与背景墙
                for (int x = 1; x <= 10; x++)
                {
                    int currentX = (side == 1) ? minX + (x - 1) : maxX - (x - 1);

                    for (int y = -5; y <= 0; y++)
                    {
                        int currentY = originY + y;
                        if (!WorldGen.InWorld(currentX, currentY)) continue;

                        Tile t = Main.tile[currentX, currentY];
                        if (t == null)
                        {
                            t = new Tile();
                            Main.tile[currentX, currentY] = t;
                        }

                        // 内部区域全覆盖铺设背景墙
                        if (y != -5 && y != 0 && x != 1 && x != 10)
                        {
                            t.wall = (ushort)wallType;
                            WorldGen.SquareWallFrame(currentX, currentY);
                        }

                        // 地板
                        if (y == 0)
                        {
                            WorldGen.PlaceTile(currentX, currentY, tileType, mute: true, forced: true);
                        }
                        // 天花板平台 (x=3..5)
                        else if (y == -5 && x >= 3 && x <= 5)
                        {
                            WorldGen.PlaceTile(currentX, currentY, TileID.Platforms, mute: true, forced: true, style: platformStyle);
                        }
                        // 天花板实体块
                        else if (y == -5)
                        {
                            WorldGen.PlaceTile(currentX, currentY, tileType, mute: true, forced: true);
                        }
                        // 门上方的立柱实体块
                        else if ((x == 1 || x == 10) && y == -4)
                        {
                            WorldGen.PlaceTile(currentX, currentY, tileType, mute: true, forced: true);
                        }
                    }
                }

                // 阶段 3：放置家具（门、椅子、桌子、火把）
                int floorY = originY; // 地板图格 Y
                int interiorFloorContactY = floorY - 1; // 站在地板上的图格 Y

                // 3.1 放置左门与右门 (x=1, x=10)
                int leftDoorX = (side == 1) ? minX : maxX;
                int rightDoorX = (side == 1) ? maxX : minX;

                WorldGen.PlaceObject(leftDoorX, interiorFloorContactY, TileID.ClosedDoor, mute: true, style: doorStyle, direction: side);
                WorldGen.PlaceObject(rightDoorX, interiorFloorContactY, TileID.ClosedDoor, mute: true, style: doorStyle, direction: -side);

                // 3.2 放置椅子与桌子
                int chairX = (side == 1) ? minX + 3 : maxX - 3;
                int tableX = (side == 1) ? minX + 5 : maxX - 6;

                // 椅子面向桌子
                WorldGen.PlaceObject(chairX, interiorFloorContactY, TileID.Chairs, mute: true, style: chairStyle, direction: side);
                // 桌子 (2x2)
                WorldGen.PlaceObject(tableX, interiorFloorContactY, TileID.Tables, mute: true, style: tableStyle, direction: side);

                // 3.3 放置中心火把 (天花板下方中心)
                int torchX = (side == 1) ? minX + 4 : maxX - 4;
                int torchY = originY - 4;
                WorldGen.PlaceTile(torchX, torchY, TileID.Torches, mute: true, forced: true, style: torchStyle);

                // 阶段 4：区域帧刷新与网络同步
                for (int gx = minX - 1; gx <= maxX + 1; gx++)
                {
                    for (int gy = minY - 1; gy <= maxY + 1; gy++)
                    {
                        if (WorldGen.InWorld(gx, gy))
                        {
                            WorldGen.SquareTileFrame(gx, gy);
                            WorldGen.SquareWallFrame(gx, gy);
                        }
                    }
                }

                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendTileSquare(-1, minX - 1, minY - 1, AutoHouseBuildPlan.HouseWidth + 2, AutoHouseBuildPlan.HouseHeight + 2);
                }

                logger.Info($"[AutoHouse] ★ 房屋结构与内部家具生成完毕！边界: X:[{minX}, {maxX}], Y:[{minY}, {maxY}]");
            }
            catch (Exception ex)
            {
                logger.Error($"[AutoHouse] 房屋构建异常: {ex.Message}", ex);
            }
        }
    }
}
