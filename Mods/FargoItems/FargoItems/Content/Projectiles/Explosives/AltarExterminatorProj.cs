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
    public class AltarExterminatorProj : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Altar Exterminator");
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            Player player = Main.player[Projectile.owner];
            ExterminateAltars(player);
        }

        public static void ExterminateAltars(Player player)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            for (int i = 0; i < Main.maxTilesX; i++)
            {
                for (int j = 0; j < Main.maxTilesY; j++)
                {
                    if (WorldGen.InWorld(i, j))
                    {
                        Tile tile = Framing.GetTileSafely(i, j);
                        if (tile.active() && tile.type == TileID.DemonAltar)
                        {
                            WorldGen.KillTile(i, j, noItem: true);
                            tile.ClearEverything();
                        }
                    }
                }
            }

            Main.refreshMap = true;
        }
    }
}
