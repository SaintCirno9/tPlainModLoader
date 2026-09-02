using FargoItems.Content.Items.Explosives;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TPML.Content;

namespace FargoItems.Content.Projectiles.Explosives
{
    public class InstabridgeProj : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Instabridge");
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
            Vector2 position = Projectile.Center;
            SoundEngine.PlaySound(SoundID.Item14, position);
            Player player = Main.player[Projectile.owner];
            BuildBridge(player, position, Projectile.ai[2] == 2);
        }

        public static void BuildBridge(Player player, Vector2 position, bool isAlt)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int originY = (int)(position.Y / 16.0f);

            // 贯穿整个世界 X 轴
            for (int x = 1; x < Main.maxTilesX - 1; x++)
            {
                // 清理上方 5 格空间，最底格 (y=0) 铺设木平台
                for (int y = -5; y <= 0; y++)
                {
                    int xPosition = x;
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

                    if (y == 0 && !isAlt)
                    {
                        FargoItems.Content.Logic.ExplosivesHelper.ClearEverything(xPosition, yPosition, false);
                        WorldGen.PlaceTile(xPosition, yPosition, TileID.Platforms, mute: true, forced: true);
                    }
                    else
                    {
                        if (!FargoItems.Content.Logic.ExplosivesHelper.TileIsLiterallyAir(tile))
                            FargoItems.Content.Logic.ExplosivesHelper.ClearEverything(xPosition, yPosition, false);
                    }
                }
            }

            // 刷新全图平台连接帧
            for (int x = 1; x < Main.maxTilesX - 1; x++)
            {
                if (WorldGen.InWorld(x, originY))
                {
                    WorldGen.SquareTileFrame(x, originY);
                }
            }
        }
    }
}
