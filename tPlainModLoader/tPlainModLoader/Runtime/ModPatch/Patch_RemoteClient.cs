using System.Collections.Generic;
using Terraria;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// RemoteClient 强类型门面调度类
    /// 作者: SaintCirno9
    /// </summary>
    internal class Patch_RemoteClient : ListCopy<PatchRemoteClient>
    {
        private static readonly List<PatchRemoteClient> mod = new List<PatchRemoteClient>();
        internal static List<PatchRemoteClient> ModList => mod;
        private static bool _hooksInitialized = false;

        public Patch_RemoteClient() : base(mod) { }

        /// <summary>集中注册 RemoteClient 强类型 HookGen 钩子</summary>
        public static void RegisterAll()
        {
            if (_hooksInitialized) return;

            On_RemoteClient.Reset += Hook_Reset;

            _hooksInitialized = true;
        }

        private static void Hook_Reset(On_RemoteClient.orig_Reset orig, RemoteClient self)
        {
            UpdateNPCPostfix(self);
            orig(self);
        }

        public static void UpdateNPCPostfix(RemoteClient __instance)
        {
            mod.ForTry(item => item.ResetPrefix(__instance));
        }
    }
}
