using HarmonyLib;
using System;
using System.Collections.Generic;

namespace tContentPatch.ModPatch
{
    [HarmonyPatch(typeof(Terraria.IO.WorldFile))]
    internal class Patch_WorldFile : ListCopy<PatchWorldFile>
    {
        private static List<PatchWorldFile> mod = new List<PatchWorldFile>();

        public Patch_WorldFile() : base(mod) { }

        [HarmonyPatch("SaveWorld", new Type[] { typeof(bool), typeof(bool)})]
        [HarmonyPostfix]
        public static void SaveWorldPostfix(bool useCloudSaving, bool resetTime = false)
        {
            mod.ForTry(item => item.SaveWorldPostfix(useCloudSaving, resetTime));
        }
    }
}
