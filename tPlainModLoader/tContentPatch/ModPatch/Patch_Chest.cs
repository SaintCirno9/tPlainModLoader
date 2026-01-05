using HarmonyLib;
using System.Collections.Generic;
using Terraria;

namespace tContentPatch.ModPatch
{
    [HarmonyPatch(typeof(Chest))]
    internal class Patch_Chest : ListCopy<PatchChest>
    {
        private static List<PatchChest> mod = new List<PatchChest>();

        public Patch_Chest() : base(mod) { }

        [HarmonyPatch("SetupShop")]
        [HarmonyPostfix]
        public static void UpdateNPCPostfix(Chest __instance, int type)
        {
            mod.ForTry(item => item.SetupShop(__instance, type));
        }
    }
}
