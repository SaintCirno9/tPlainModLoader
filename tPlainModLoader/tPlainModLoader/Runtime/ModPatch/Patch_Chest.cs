using System.Collections.Generic;
using Terraria;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// Chest 商店强类型门面调度类
    /// 作者: SaintCirno9
    /// </summary>
    internal class Patch_Chest : ListCopy<PatchChest>
    {
        private static readonly List<PatchChest> mod = new List<PatchChest>();
        internal static List<PatchChest> ModList => mod;
        private static bool _hooksInitialized = false;

        public Patch_Chest() : base(mod) { }

        /// <summary>集中注册 Chest 强类型 HookGen 钩子</summary>
        public static void RegisterAll()
        {
            if (_hooksInitialized) return;

            On_Chest.SetupShop += Hook_SetupShop;

            _hooksInitialized = true;
        }

        private static void Hook_SetupShop(On_Chest.orig_SetupShop orig, Chest self, int type)
        {
            orig(self, type);
            UpdateNPCPostfix(self, type);
        }

        public static void UpdateNPCPostfix(Chest __instance, int type)
        {
            mod.ForTry(item => item.SetupShop(__instance, type));
        }
    }
}
