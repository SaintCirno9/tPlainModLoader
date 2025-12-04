using HarmonyLib;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;

namespace tContentPatch.ModPatch
{
    [HarmonyPatch(typeof(Projectile))]
    internal class Patch_Projectile : ListCopy<PatchProjectile>
    {
        private static List<PatchProjectile> mod = new List<PatchProjectile>();

        public Patch_Projectile() : base(mod) { }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static void UpdatePrefix(Projectile __instance, int i)
        {
            mod.ForTry(item => item.UpdatePrefix(__instance, i));
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix(Projectile __instance, int i)
        {
            mod.ForTry(item => item.UpdatePostfix(__instance, i));
        }

        [HarmonyPatch("Kill")]
        [HarmonyPrefix]
        public static void KillPrefix(Projectile __instance)
        {
            mod.ForTry(item => item.KillPrefix(__instance));
        }

        [HarmonyPatch("Kill")]
        [HarmonyPostfix]
        public static void KillPostfix(Projectile __instance)
        {
            mod.ForTry(item => item.KillPostfix(__instance));
        }

        [HarmonyPatch("NewProjectile", new Type[]
        {
            typeof(IEntitySource),
            typeof(float), typeof(float), typeof(float), typeof(float),
            typeof(int), typeof(int), typeof(float), typeof(int),
            typeof(float),typeof(float),typeof(float)
        })]
        [HarmonyPostfix]
        internal static void NewProjectilePostfix(int __result,
            IEntitySource spawnSource,
            float X, float Y, float SpeedX, float SpeedY,
            int Type, int Damage, float KnockBack, int Owner,
            float ai0, float ai1, float ai2)
        {
            mod.ForTry(item => item.NewProjectilePostfix(
                __result, spawnSource, X, Y, SpeedX, SpeedY, Type, Damage, KnockBack, Owner, ai0, ai1, ai2));
        }
    }
}
