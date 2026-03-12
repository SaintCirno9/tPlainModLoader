using HarmonyLib;
using System.Collections.Generic;
using Terraria;

namespace tContentPatch.ModPatch
{
    [HarmonyPatch(typeof(Terraria.IO.WorldFile))]
    internal class Patch_WorldFile : ListCopy<PatchWorldFile>
    {
        private static List<PatchWorldFile> mod = new List<PatchWorldFile>();

        public Patch_WorldFile() : base(mod) { }

        [HarmonyPatch("_SaveWorld")]
        [HarmonyPrefix]
        public static void SaveWorldPrefix(bool useCloudSaving, bool resetTime, bool useTemps, bool canBeSkipped)
        {
            if (Main.netMode != 0 && Main.dedServ == false) return;

            mod.ForTry(item => item.SaveWorldPrefix(useCloudSaving, resetTime, useTemps, canBeSkipped));
        }

        [HarmonyPatch("_SaveWorld")]
        [HarmonyPostfix]
        public static void SaveWorldPostfix(bool useCloudSaving, bool resetTime, bool useTemps, bool canBeSkipped)
        {
            if (Main.netMode != 0 && Main.dedServ == false) return;

            mod.ForTry(item => item.SaveWorldPostfix(useCloudSaving, resetTime, useTemps, canBeSkipped));
        }
    }
}
