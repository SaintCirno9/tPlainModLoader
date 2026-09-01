using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Terraria;

namespace TPML.Content
{
    /// <summary>
    /// 对齐 tML 的 Projectile 便捷方法
    /// 作者: SaintCirno9
    /// </summary>
    public static class ProjectileExtensions
    {
        private static readonly ConditionalWeakTable<Projectile, ConcurrentDictionary<Type, object>> _globalInstances = new ConditionalWeakTable<Projectile, ConcurrentDictionary<Type, object>>();

        /// <summary>
        /// 对齐 tML Projectile.GetGlobalProjectile<T>
        /// </summary>
        public static T GetGlobalProjectile<T>(this Projectile projectile) where T : class, new()
        {
            if (projectile == null) return null;
            var dict = _globalInstances.GetOrCreateValue(projectile);
            return (T)dict.GetOrAdd(typeof(T), _ => new T());
        }

        /// <summary>
        /// 对齐 tML Projectile.TryGetGlobalProjectile<T>
        /// </summary>
        public static bool TryGetGlobalProjectile<T>(this Projectile projectile, out T result) where T : class, new()
        {
            if (projectile == null)
            {
                result = null;
                return false;
            }
            result = projectile.GetGlobalProjectile<T>();
            return result != null;
        }

        /// <summary>
        /// 对齐 tML Projectile.TryGetOwner
        /// </summary>
        public static bool TryGetOwner(this Projectile projectile, out Player player)
        {
            if (projectile != null && projectile.owner >= 0 && projectile.owner < Main.maxPlayers && Main.player[projectile.owner] != null && Main.player[projectile.owner].active)
            {
                player = Main.player[projectile.owner];
                return true;
            }
            player = null;
            return false;
        }

        /// <summary>
        /// 对齐 tML <c>Projectile.GetSource_FromThis()</c>
        /// </summary>
        public static Terraria.DataStructures.IEntitySource GetSource_FromThis(this Projectile proj, string context = null)
        {
            return new EntitySource_Misc(context ?? "Projectile");
        }

        /// <summary>
        /// 对齐 tML <c>Projectile.ModProjectile</c> 的方法形式
        /// </summary>
        public static ModProjectile ModProjectile(this Projectile proj) => proj.GetModProjectile();

        /// <summary>
        /// 获取绑定在此 Projectile 实例上的 ModProjectile
        /// </summary>
        public static ModProjectile GetModProjectile(this Projectile proj) => ProjectileLoader.GetModProjectile(proj);

        /// <summary>
        /// 获取绑定在此 Projectile 实例上的泛型 ModProjectile
        /// </summary>
        public static T GetModProjectile<T>(this Projectile proj) where T : ModProjectile => ProjectileLoader.GetModProjectile<T>(proj);

        /// <summary>
        /// 对齐 tML <c>Projectile.CloneDefaults</c>
        /// </summary>
        public static void CloneDefaults(this Projectile proj, int typeToClone)
        {
            if (proj == null) return;
            int originalType = proj.type;
            proj.SetDefaults(typeToClone);
            proj.type = originalType;
        }
    }
}