using HarmonyLib;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;

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

        [HarmonyPatch("SetDefaults")]
        [HarmonyPrefix]
        public static void SetDefaultsPrefix(NPC __instance, int Type, NPCSpawnParams spawnparams)
        {
            mod.ForTry(item => item.SetDefaultsPrefix(__instance, Type, spawnparams));
        }

        [HarmonyPatch("SetDefaults")]
        [HarmonyPostfix]
        public static void SetDefaultsPostfix(NPC __instance, int Type, NPCSpawnParams spawnparams)
        {
            mod.ForTry(item => item.SetDefaultsPostfix(__instance, Type, spawnparams));
        }

        [HarmonyPatch("NewNPC")]
        [HarmonyPostfix]
        public static void NewNPCPostfix(int __result, IEntitySource source,
            int X, int Y, int Type, int Start, float ai0, float ai1, float ai2, float ai3, int Target)
        {
            mod.ForTry(item => item.NewNPCPostfix(__result, source,
                X, Y, Type, Start, ai0, ai1, ai2, ai3, Target));
        }
    }
}
