using FargoItems.Content.Items.Explosives;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TPML.Content;

namespace FargoItems.Content.Projectiles.Explosives
{
    public class DoubleObsInstaBridgeProj : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Double Obsidian Instabridge");
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
            BuildBridge(player, position);
        }

        public static void BuildBridge(Player player, Vector2 position)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int originY = (int)(position.Y / 16.0f);

            // 贯穿整个世界 X 轴
            for (int x = 1; x < Main.maxTilesX - 1; x++)
            {
                for (int y = -40; y <= 0; y++)
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

                    if (y == -20 || y == 0)
                    {
                        FargoItems.Content.Logic.ExplosivesHelper.ClearEverything(xPosition, yPosition, false);
                        WorldGen.PlaceTile(xPosition, yPosition, TileID.Platforms, mute: true, forced: true, style: 13);
                    }
                    else
                    {
                        if (!FargoItems.Content.Logic.ExplosivesHelper.TileIsLiterallyAir(tile))
                            FargoItems.Content.Logic.ExplosivesHelper.ClearEverything(xPosition, yPosition, false);
                    }
                }
            }

            // 刷新双层平台连接帧
            for (int x = 1; x < Main.maxTilesX - 1; x++)
            {
                if (WorldGen.InWorld(x, originY))
                {
                    WorldGen.SquareTileFrame(x, originY);
                }
                if (WorldGen.InWorld(x, originY - 20))
                {
                    WorldGen.SquareTileFrame(x, originY - 20);
                }
            }
        }
    }
}
