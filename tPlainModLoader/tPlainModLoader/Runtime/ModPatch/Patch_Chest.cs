using System;
using System.Collections.Generic;
using Terraria;
using TPML.Content.Engine;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// Chest 商店补丁（M2 迁移：Harmony → MonoMod）
    /// </summary>
    internal class Patch_Chest : ListCopy<PatchChest>
    {
        private static List<PatchChest> mod = new List<PatchChest>();

        public Patch_Chest() : base(mod) { }

        /// <summary>集中注册全部补丁（由 ContentPatch_Initialize 调用）</summary>
        public static void RegisterAll()
        {
            // Chest.SetupShop(int)
            HookRegistry.Add(MethodLookup.Instance(typeof(Chest), "SetupShop", typeof(int)),
                (Action<Action<Chest, int>, Chest, int>)((orig, self, type) =>
                {
                    orig(self, type);
                    UpdateNPCPostfix(self, type);
                }));
        }

        public static void UpdateNPCPostfix(Chest __instance, int type)
        {
            mod.ForTry(item => item.SetupShop(__instance, type));
        }
    }
}
