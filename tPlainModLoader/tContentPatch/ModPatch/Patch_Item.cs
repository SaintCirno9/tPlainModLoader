using HarmonyLib;
using Microsoft.Xna.Framework;
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
            typeof(Vector2), typeof(int), typeof(int), typeof(int),
            typeof(NewItemOwnership),
            typeof(Vector2?), typeof(Item.NewItemModifier), typeof(bool)
        })]
        [HarmonyPostfix]
        public static void NewItemPostfix(int __result,
            IEntitySource source,
            Vector2 center, int type, int stack, int prefix,
            NewItemOwnership ownership,
            Vector2? velocity, Item.NewItemModifier modifier, bool noBroadcast)
        {
            mod.ForTry(item => item.NewItemPostfix(__result, source,
                center, type, stack, prefix,
                ownership,
                velocity, modifier, noBroadcast));
        }
    }
}
