using HarmonyLib;
using System.Collections.Generic;
using Terraria;
using Terraria.IO;

namespace tContentPatch.ModPatch
{
    [HarmonyPatch(typeof(Player))]
    internal class Patch_Player : ListCopy<PatchPlayer>
    {
        private static List<PatchPlayer> mod = new List<PatchPlayer>();

        public Patch_Player() : base(mod) { }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static void UpdatePrefix(Player __instance, int i)
        {
            mod.ForTry(item => item.UpdatePrefix(__instance, i));
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix(Player __instance, int i)
        {
            mod.ForTry(item => item.UpdatePostfix(__instance, i));
        }

        [HarmonyPatch("UpdateArmorSets")]
        [HarmonyPostfix]
        public static void UpdateArmorSetsPostfix(Player __instance, int i)
        {
            mod.ForTry(item => item.UpdateArmorSetsPostfix(__instance, i));
        }

        [HarmonyPatch("SavePlayer")]
        [HarmonyPrefix]
        public static void SavePlayerPrefix(PlayerFileData playerFile, bool skipMapSave)
        {
            if (Main.netMode != 0 && Main.netMode != 1) return;

            mod.ForTry(item => item.SavePlayerPrefix(playerFile, skipMapSave));
        }

        [HarmonyPatch("SavePlayer")]
        [HarmonyPostfix]
        public static void SavePlayerPostfix(PlayerFileData playerFile, bool skipMapSave)
        {
            if (Main.netMode != 0 && Main.netMode != 1) return;

            mod.ForTry(item => item.SavePlayerPostfix(playerFile, skipMapSave));
        }
    }
}
