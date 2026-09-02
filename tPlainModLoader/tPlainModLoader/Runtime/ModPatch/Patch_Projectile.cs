using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// Projectile 生命周期补丁列表持有类（已收敛至 ProjectileLoader 统一分发）
    /// 作者: SaintCirno9
    /// </summary>
    internal class Patch_Projectile : ListCopy<PatchProjectile>
    {
        private static readonly List<PatchProjectile> mod = new List<PatchProjectile>();
        internal static List<PatchProjectile> ModList => mod;

        public Patch_Projectile() : base(mod) { }

        public static void UpdatePrefix(Projectile __instance, int i)
        {
            mod.ForTry(item => item.UpdatePrefix(__instance, i));
        }

        public static void UpdatePostfix(Projectile __instance, int i)
        {
            mod.ForTry(item => item.UpdatePostfix(__instance, i));
        }

        public static void KillPrefix(Projectile __instance)
        {
            mod.ForTry(item => item.KillPrefix(__instance));
        }

        public static void KillPostfix(Projectile __instance)
        {
            mod.ForTry(item => item.KillPostfix(__instance));
        }

        public static void SetDefaultsPrefix(Projectile __instance, int Type)
        {
            mod.ForTry(item => item.SetDefaultsPrefix(__instance, Type));
        }

        public static void SetDefaultsPostfix(Projectile __instance, int Type)
        {
            mod.ForTry(item => item.SetDefaultsPostfix(__instance, Type));
        }

        public static void NewProjectilePostfix(int __result,
            IEntitySource spawnSource,
            float X, float Y, float SpeedX, float SpeedY,
            int Type, int Damage, float KnockBack, int Owner,
            float ai0, float ai1, float ai2,
            NewProjectileModifier modifer)
        {
            mod.ForTry(item => item.NewProjectilePostfix(
                __result, spawnSource, X, Y, SpeedX, SpeedY, Type, Damage, KnockBack, Owner, ai0, ai1, ai2, modifer));
        }

        public static void AI_203_GetLightningColor(Projectile __instance, ref Color __result)
        {
            Color color = __result;
            mod.ForTry(item =>
            {
                color = item.AI_203_GetLightningColor(__instance, color);
            });
            __result = color;
        }
    }
}
