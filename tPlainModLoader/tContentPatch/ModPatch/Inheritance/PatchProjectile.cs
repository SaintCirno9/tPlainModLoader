using Terraria;

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
    }
}
