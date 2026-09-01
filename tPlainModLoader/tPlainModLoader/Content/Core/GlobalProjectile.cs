using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;

namespace TPML.Content
{
    /// <summary>
    /// TPML 全局弹幕行为修饰基类
    /// 作者: SaintCirno9
    /// </summary>
    public abstract class GlobalProjectile : ModType
    {
        public virtual bool InstancePerEntity => false;

        public virtual void SetDefaults(Projectile projectile)
        {
        }

        public virtual void OnSpawn(Projectile projectile, IEntitySource source)
        {
        }

        public virtual bool PreAI(Projectile projectile)
        {
            return true;
        }

        public virtual void AI(Projectile projectile)
        {
        }

        public virtual void PostAI(Projectile projectile)
        {
        }

        public virtual void OnKill(Projectile projectile, int timeLeft)
        {
        }

        public virtual bool? CanDamage(Projectile projectile)
        {
            return null;
        }

        public virtual void ModifyHitNPC(Projectile projectile, NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
        }

        public virtual void OnHitNPC(Projectile projectile, NPC target, int damage, float knockback, bool crit)
        {
        }

        public virtual void ModifyHitPlayer(Projectile projectile, Player target, ref int damage, ref bool crit)
        {
        }

        public virtual void OnHitPlayer(Projectile projectile, Player target, int damage, bool crit)
        {
        }

        public virtual Color? GetAlpha(Projectile projectile, Color lightColor)
        {
            return null;
        }

        public virtual bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            return true;
        }

        public virtual void PostDraw(Projectile projectile, Color lightColor)
        {
        }

        public virtual bool ShouldUpdatePosition(Projectile projectile)
        {
            return true;
        }
    }
}