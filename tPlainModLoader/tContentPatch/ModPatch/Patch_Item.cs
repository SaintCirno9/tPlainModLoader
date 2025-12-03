using HarmonyLib;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;

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

        [HarmonyPatch("NewItem", new Type[]
        {
            typeof(IEntitySource),
            typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int),
            typeof(bool), typeof(int), typeof(bool), typeof(bool)
        })]
        [HarmonyPostfix]
        public static void NewItemPostfix(int __result,
            IEntitySource source,
            int X, int Y, int Width, int Height, int Type, int Stack,
            bool noBroadcast, int pfix, bool noGrabDelay, bool reverseLookup)
        {
            mod.ForTry(item => item.NewItemPostfix(__result, source,
                X, Y, Width, Height, Type, Stack, noBroadcast, pfix, noGrabDelay, reverseLookup));
        }
    }
}
