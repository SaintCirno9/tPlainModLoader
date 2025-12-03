using HarmonyLib;
using System.Collections.Generic;
using Terraria;

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
    }
}
