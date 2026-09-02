using FargoItems.Content.Items.Explosives;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TPML.Content;
using Terraria.ObjectData;

namespace FargoItems.Content.Projectiles.Explosives
{
    public class LihzahrdInstactuationBombProj : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Lihzahrd Instactuation Bomb");
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 36;
            Projectile.aiStyle = 16;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override void AI()
        {
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            Player player = Main.player[Projectile.owner];
            ActuateTemple(player, Projectile.Center);
        }

        public static void ActuateTemple(Player player, Vector2 position)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int xPos = (int)position.X / 16;
            int yPos = (int)position.Y / 16;

            bool WipeColumn(int i)
            {
                for (int j = 0; j >= -60; j--)
                {
                    int tileX = xPos + i;
                    int tileY = yPos + j;

                    if (!WorldGen.InWorld(tileX, tileY))
                    {
                        if (j == 0) return false;
                        continue;
                    }

                    Tile tile = Framing.GetTileSafely(tileX, tileY);

                    if (tile.type == TileID.LihzahrdAltar)
                        continue;

                    if (tile.wall != WallID.LihzahrdBrickUnsafe)
                    {
                        if (j == 0) return false;
                        continue;
                    }

                    // 检查上方是否有宝箱
                    Tile tileAbove = Framing.GetTileSafely(tileX, tileY - 1);
                    if (tileAbove.active() && TileID.Sets.BasicChest[tileAbove.type])
                    {
                        TileObjectData data = TileObjectData.GetTileData(tileAbove.type, 0);
                        int x = tileX - (tileAbove.frameX / 18 % (data?.Width ?? 2));
                        int y = tileY - 1 - (tileAbove.frameY / 18 % (data?.Height ?? 2));

                        WorldGen.KillTile(x, y, noItem: true);
                        if (tileAbove.active() && TileID.Sets.BasicChest[tileAbove.type])
                            continue;
                    }

                    if (tile.active() && TileID.Sets.BasicChest[tile.type])
                    {
                        TileObjectData data = TileObjectData.GetTileData(tile.type, 0);
                        int x = tileX - tile.frameX / 18 % (data?.Width ?? 2);
                        int y = tileY - tile.frameY / 18 % (data?.Height ?? 2);

                        WorldGen.KillTile(x, y, noItem: true);
                        continue;
                    }

                    if (tile.active() && tile.type == TileID.LihzahrdBrick)
                    {
                        tile.inActive(true);
                        WorldGen.SquareTileFrame(tileX, tileY);
                        continue;
                    }

                    if (tile.active())
                    {
                        WorldGen.KillTile(tileX, tileY, noItem: true);
                    }
                }

                return true;
            }

            int leftMax = 60;
            int rightMax = 60;

            int leftTry = 0;
            for (; leftTry >= -leftMax; leftTry--)
            {
                if (!WipeColumn(leftTry))
                {
                    rightMax += leftMax - Math.Abs(leftTry);
                    break;
                }
            }

            for (int rightTry = 0; rightTry <= rightMax; rightTry++)
            {
                if (!WipeColumn(rightTry))
                {
                    leftMax += rightMax - rightTry;
                    for (; leftTry >= -leftMax; leftTry--)
                    {
                        if (!WipeColumn(leftTry))
                            break;
                    }
                    break;
                }
            }

            Main.refreshMap = true;
        }
    }
}
