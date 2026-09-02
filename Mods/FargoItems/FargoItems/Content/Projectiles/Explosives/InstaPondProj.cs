using FargoItems.Content.Items.Explosives;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TPML.Content;

namespace FargoItems.Content.Projectiles.Explosives
{
    public class InstaPondProj : ModProjectile
    {
        public override string Texture => "Fargowiltas/Items/Explosives/InstaPond";
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
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
            SoundEngine.PlaySound(SoundID.Item15, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            Player player = Main.player[Projectile.owner];
            BuildPond(player, Projectile.Center);
        }

        public static void BuildPond(Player player, Vector2 position)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            const int width = 150;
            const int height = 50;

            int originX = (int)(position.X / 16.0f);
            int originY = (int)(position.Y / 16.0f);

            for (int x = -width / 2; x <= width / 2; x++)
            {
                for (int y = 0; y <= height; y++)
                {
                    int xPosition = originX + x;
                    int yPosition = originY + y;

                    if (!WorldGen.InWorld(xPosition, yPosition))
                        continue;

                    Tile tile = Main.tile[xPosition, yPosition];
                    if (tile == null)
                    {
                        tile = new Tile();
                        Main.tile[xPosition, yPosition] = tile;
                    }

                    if (!FargoItems.Content.Logic.ExplosivesHelper.OkayToDestroyTileAt(xPosition, yPosition))
                        continue;

                    FargoItems.Content.Logic.ExplosivesHelper.ClearEverything(xPosition, yPosition, false);

                    if (y == height || Math.Abs(x) == width / 2)
                    {
                        // 铺设石板边缘与池底
                        WorldGen.PlaceTile(xPosition, yPosition, TileID.StoneSlab, mute: true, forced: true);
                        WorldGen.SquareTileFrame(xPosition, yPosition);
                    }
                    else
                    {
                        // 填充水
                        WorldGen.PlaceLiquid(xPosition, yPosition, (byte)LiquidID.Water, byte.MaxValue);
                    }
                }
            }

            Liquid.QuickWater(3);
            Main.refreshMap = true;
        }
    }
}
