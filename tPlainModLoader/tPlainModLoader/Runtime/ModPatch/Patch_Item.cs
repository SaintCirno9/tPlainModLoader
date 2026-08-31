using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Items;
using TPML.Content.Engine;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// Item 生命周期补丁（M2 迁移：Harmony → MonoMod）
    /// </summary>
    internal class Patch_Item : ListCopy<PatchItem>
    {
        private static List<PatchItem> mod = new List<PatchItem>();

        public Patch_Item() : base(mod) { }

        /// <summary>集中注册全部补丁（由 ContentPatch_Initialize 调用）</summary>
        public static void RegisterAll()
        {
            var item = typeof(Item);

            // Item.SetDefaults(int, ItemVariant)
            HookRegistry.Add(MethodLookup.Instance(item, "SetDefaults", typeof(int), typeof(ItemVariant)),
                (Action<Action<Item, int, ItemVariant>, Item, int, ItemVariant>)((orig, self, type, variant) =>
                {
                    SetDefaultsPrefix(self, type, variant);
                    orig(self, type, variant);
                    SetDefaultsPostfix(self, type, variant);
                }));

            // Item.NewItem(IEntitySource, Vector2, int, int, int, NewItemOwnership, Vector2?, NewItemModifier, bool)（静态，返回 int）
            // 注意：NewItemOwnership/NewItemModifier 位于 Terraria 命名空间（非 DataStructures）
            var newItem = MethodLookup.Static(item, "NewItem", new[]
            {
                typeof(IEntitySource), typeof(Vector2), typeof(int), typeof(int), typeof(int),
                typeof(Terraria.NewItemOwnership), typeof(Vector2?), typeof(Item.NewItemModifier), typeof(bool)
            });
            if (newItem != null)
            {
                HookRegistry.Add(newItem, (Func<Func<IEntitySource, Vector2, int, int, int, Terraria.NewItemOwnership, Vector2?, Item.NewItemModifier, bool, int>, IEntitySource, Vector2, int, int, int, Terraria.NewItemOwnership, Vector2?, Item.NewItemModifier, bool, int>)((orig, source, center, type, stack, prefix, ownership, velocity, modifier, noBroadcast) =>
                {
                    int result = orig(source, center, type, stack, prefix, ownership, velocity, modifier, noBroadcast);
                    NewItemPostfix(result, source, center, type, stack, prefix, ownership, velocity, modifier, noBroadcast);
                    return result;
                }));
            }
        }

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
