using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace TPML.Content
{
    /// <summary>
    /// TPML 自定义弹幕 (ModProjectile) 基类
    /// 遵循 tModLoader 经典 API 范式与强类型生命周期分发
    /// 作者: SaintCirno9
    /// </summary>
    public abstract class ModProjectile : ModType
    {
        public Projectile Projectile { get; internal set; }
        private int _type;
        public int Type => Projectile != null && Projectile.type > 0 ? Projectile.type : _type;
        internal void SetType(int type) => _type = type;

        public virtual string Texture => (GetType().Namespace + "." + Name).Replace('.', '/');
        public int AIType { get; set; }

        public string DisplayName => ProjectileLoader.GetDisplayName(Type);

        public override void Load(Mod mod)
        {
            Mod = mod;
            ProjectileLoader.Register(this);
            base.Load(mod);
        }

        public virtual void SetStaticDefaults()
        {
        }

        public virtual void SetDefaults()
        {
        }

        public virtual void AI()
        {
        }

        public virtual bool PreAI()
        {
            return true;
        }

        public virtual void PostAI()
        {
        }

        public virtual void OnKill(int timeLeft)
        {
        }

        public virtual bool OnTileCollide(Vector2 oldVelocity)
        {
            return true;
        }

        public virtual bool? CanDamage()
        {
            return null;
        }

        public virtual Color? GetAlpha(Color lightColor)
        {
            return null;
        }

        public virtual bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            return true;
        }

        public virtual bool PreDraw(ref Color lightColor)
        {
            return true;
        }

        public virtual void PostDraw(Color lightColor)
        {
        }

        public virtual bool ShouldUpdatePosition()
        {
            return true;
        }

        public virtual void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
        }

        public virtual void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
        }

        public virtual void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
        }

        public virtual void OnHitPlayer(Player target, int damage, bool crit)
        {
        }

        public virtual bool? CanCutTiles()
        {
            return null;
        }

        public virtual void CutTiles()
        {
        }

        public virtual ModProjectile Clone(Projectile newEntity)
        {
            ModProjectile clone = (ModProjectile)Activator.CreateInstance(GetType());
            clone.Mod = Mod;
            clone.Projectile = newEntity;
            clone.SetType(Type);
            clone.AIType = AIType;
            return clone;
        }
    }
}
