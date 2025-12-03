using HarmonyLib;
using System.Collections.Generic;
using Terraria;

namespace tContentPatch.ModPatch
{
    [HarmonyPatch(typeof(Item))]
    internal class Patch_Item : ListCopy<PatchItem>
    {
        private static List<PatchItem> mod = new List<PatchItem>();

        public Patch_Item() : base(mod) { }

        [HarmonyPatch("UpdateItem")]
        [HarmonyPrefix]
        public static void UpdateItemPrefix(Item __instance, int i)
        {
            mod.ForTry(item => item.UpdateItemPrefix(__instance, i));
        }

        [HarmonyPatch("UpdateItem")]
        [HarmonyPostfix]
        public static void UpdateItemPostfix(Item __instance, int i)
        {
            mod.ForTry(item => item.UpdateItemPostfix(__instance, i));
        }
    }
}
