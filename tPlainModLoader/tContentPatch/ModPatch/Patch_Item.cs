using HarmonyLib;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Items;

namespace tContentPatch.ModPatch
{
    [HarmonyPatch(typeof(Item))]
    internal class Patch_Item : ListCopy<PatchItem>
    {
        private static List<PatchItem> mod = new List<PatchItem>();

        public Patch_Item() : base(mod) { }

        [HarmonyPatch("SetDefaults")]
        [HarmonyPrefix]
        public static void SetDefaultsPrefix(Item __instance, int Type, ItemVariant variant)
        {
            mod.ForTry(item => item.SetDefaultsPrefix(__instance, Type, variant));
        }

        [HarmonyPatch("SetDefaults")]
        [HarmonyPostfix]
        public static void SetDefaultsPostfix(Item __instance, int Type, ItemVariant variant)
        {
            mod.ForTry(item => item.SetDefaultsPostfix(__instance, Type, variant));
        }

        [HarmonyPatch("NewItem", new Type[]
        {
            typeof(IEntitySource),
            typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int),
            typeof(bool), typeof(int), typeof(bool)
        })]
        [HarmonyPostfix]
        public static void NewItemPostfix(int __result,
            IEntitySource source,
            int X, int Y, int Width, int Height, int Type, int Stack,
            bool noBroadcast, int pfix, bool noGrabDelay)
        {
            mod.ForTry(item => item.NewItemPostfix(__result, source,
                X, Y, Width, Height, Type, Stack, noBroadcast, pfix, noGrabDelay));
        }
    }
}
