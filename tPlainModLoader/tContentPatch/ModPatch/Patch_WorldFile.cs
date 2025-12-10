using HarmonyLib;
using System;
using System.Collections.Generic;
using Terraria;

namespace tContentPatch.ModPatch
{
    [HarmonyPatch(typeof(Terraria.IO.WorldFile))]
    internal class Patch_WorldFile : ListCopy<PatchWorldFile>
    {
        private static List<PatchWorldFile> mod = new List<PatchWorldFile>();

        public Patch_WorldFile() : base(mod) { }

        [HarmonyPatch("SaveWorld", new Type[] { typeof(bool), typeof(bool) })]
        [HarmonyPrefix]
        public static void SaveWorldPrefix(bool useCloudSaving, bool resetTime)
        {
            if (Main.netMode != 0 && Main.netMode != 2) return;

            mod.ForTry(item => item.SaveWorldPrefix(useCloudSaving, resetTime));
        }

        [HarmonyPatch("SaveWorld", new Type[] { typeof(bool), typeof(bool)})]
        [HarmonyPostfix]
        public static void SaveWorldPostfix(bool useCloudSaving, bool resetTime)
        {
            if (Main.netMode != 0 && Main.netMode != 2) return;

            mod.ForTry(item => item.SaveWorldPostfix(useCloudSaving, resetTime));
        }
    }
}
