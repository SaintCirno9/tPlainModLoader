using Terraria;
using Terraria.ID;
using TPML.Content;
using Terraria.DataStructures;

namespace Fargowiltas.Projectiles
{
    public class LumberJaxe : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("LumberJaxe");
        }

        public override void SetDefaults()
        {
            Projectile.width = 42;
            Projectile.height = 40;
            Projectile.friendly = true;
            // Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 150;
            AIType = ProjectileID.CrystalBullet;
        }

        public override void AI()
        {
            Projectile.rotation += 0.3f;
        }

        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (target.type == NPCID.MourningWood || target.type == NPCID.Everscream || target.type == NPCID.Splinterling)
            {
                damage *= 10;
            }
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y, Projectile.velocity.X * 0, Projectile.velocity.Y * 0, ModContent.ProjectileType<Explosion>(), (int)(Projectile.damage * 1f), Projectile.knockBack, Projectile.owner);
        }
    }
}
