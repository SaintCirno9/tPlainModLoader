using HarmonyLib;
using System.Collections.Generic;
using Terraria;

namespace tContentPatch.ModPatch
{
    [HarmonyPatch(typeof(NPC))]
    internal class Patch_NPC : ListCopy<PatchNPC>
    {
        private static List<PatchNPC> mod = new List<PatchNPC>();

        public Patch_NPC() : base(mod) { }

        [HarmonyPatch("UpdateNPC")]
        [HarmonyPrefix]
        public static void UpdateNPCPrefix(NPC __instance, int i)
        {
            mod.ForTry(item => item.UpdateNPCPrefix(__instance, i));
        }

        [HarmonyPatch("UpdateNPC")]
        [HarmonyPostfix]
        public static void UpdateNPCPostfix(NPC __instance, int i)
        {
            mod.ForTry(item => item.UpdateNPCPostfix(__instance, i));
        }
    }
}
