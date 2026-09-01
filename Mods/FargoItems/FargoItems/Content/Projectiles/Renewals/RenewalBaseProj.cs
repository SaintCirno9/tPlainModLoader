using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TPML.Content;

namespace FargoItems.Content.Projectiles.Renewals
{
    public abstract class RenewalBaseProj : ModProjectile
    {
        private readonly String name;
        private readonly int projType;
        private readonly int convertType;
        private readonly bool supreme;

        protected RenewalBaseProj(String name, int projType, int convertType, bool supreme)
        {
            this.name = name;
            this.projType = projType;
            this.convertType = convertType;
            this.supreme = supreme;
        }

        public override string Texture => "FargoItems/Content/Items/Renewals/" + name;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault(name);
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.aiStyle = 2;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 170;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Shatter, Projectile.Center);

            int radius = 150;
            float[] speedX = [0, 0, 5, 5, 5, -5, -5, -5];
            float[] speedY = [5, -5, 0, 5, -5, 0, 5, -5];

            //because these projs may apparently delete blocks if spawned in unloaded chunks far away from players in mp
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                for (int i = 0; i < 8; i++)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y, speedX[i], speedY[i], projType, 0, 0, Main.myPlayer);
                }
            }

            if (supreme)
            {
                for (int x = 5; x < Main.maxTilesX - 5; x++)
                {
                    for (int y = 5; y < Main.maxTilesY - 5; y++)
                    {
                        WorldGen.Convert(x, y, convertType, 1);
                    }
                }
            }
            else
            {
                int centerX = (int)(Projectile.Center.X / 16.0f);
                int centerY = (int)(Projectile.Center.Y / 16.0f);
                int startX = Math.Max(5, centerX - radius);
                int endX = Math.Min(Main.maxTilesX - 5, centerX + radius);
                int startY = Math.Max(5, centerY - radius);
                int endY = Math.Min(Main.maxTilesY - 5, centerY + radius);
                int radiusSq = radius * radius;

                for (int x = startX; x <= endX; x++)
                {
                    int dx = x - centerX;
                    for (int y = startY; y <= endY; y++)
                    {
                        int dy = y - centerY;
                        if (dx * dx + dy * dy <= radiusSq)
                        {
                            WorldGen.Convert(x, y, convertType, 1);
                        }
                    }
                }
            }
        }
    }
}
