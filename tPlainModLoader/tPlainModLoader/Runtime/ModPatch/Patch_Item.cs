using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Items;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// Item 生命周期补丁列表持有类（已收敛至 ItemLoader 统一分发）
    /// 作者: SaintCirno9
    /// </summary>
    internal class Patch_Item : ListCopy<PatchItem>
    {
        private static readonly List<PatchItem> mod = new List<PatchItem>();
        internal static List<PatchItem> ModList => mod;

        public Patch_Item() : base(mod) { }

        public static void SetDefaultsPrefix(Item __instance, int Type, ItemVariant variant)
        {
            mod.ForTry(item => item.SetDefaultsPrefix(__instance, Type, variant));
        }

        public static void SetDefaultsPostfix(Item __instance, int Type, ItemVariant variant)
        {
            mod.ForTry(item => item.SetDefaultsPostfix(__instance, Type, variant));
        }

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
