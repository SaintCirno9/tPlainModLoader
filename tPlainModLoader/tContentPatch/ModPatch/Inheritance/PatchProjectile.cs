using Terraria;
using Terraria.DataStructures;

namespace tContentPatch
{
    /// <summary/>
    public abstract class PatchProjectile
    {
        /// <summary>
        /// <see cref="Mod.Loaded"/>后调用
        /// </summary>
        public virtual void Initialize() { }
        /// <summary>
        /// <see cref="Projectile.Update(int)"/>前调用
        /// </summary>
        public virtual void UpdatePrefix(Projectile This, int i) { }
        /// <summary>
        /// <see cref="Projectile.Update(int)"/>后调用
        /// </summary>
        public virtual void UpdatePostfix(Projectile This, int i) { }
        /// <summary>
        /// <see cref="Projectile.Kill"/>前调用
        /// </summary>
        public virtual void KillPrefix(Projectile This) { }
        /// <summary>
        /// <see cref="Projectile.NewProjectile(IEntitySource, float, float, float, float, int, int, float, int, float, float, float)"/>后调用
        /// </summary>
        public virtual void NewProjectilePostfix(int result, IEntitySource spawnSource,
            float X, float Y, float SpeedX, float SpeedY,
            int Type, int Damage, float KnockBack, int Owner, float ai0, float ai1, float ai2)
        { }
    }
}
