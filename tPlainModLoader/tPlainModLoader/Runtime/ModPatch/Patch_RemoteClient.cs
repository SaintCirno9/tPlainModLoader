using System;
using System.Collections.Generic;
using Terraria;
using TPML.Content.Engine;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// RemoteClient 补丁（M2 迁移：Harmony → MonoMod）
    /// </summary>
    internal class Patch_RemoteClient : ListCopy<PatchRemoteClient>
    {
        private static List<PatchRemoteClient> mod = new List<PatchRemoteClient>();

        public Patch_RemoteClient() : base(mod) { }

        /// <summary>集中注册全部补丁（由 ContentPatch_Initialize 调用）</summary>
        public static void RegisterAll()
        {
            // RemoteClient.Reset()（实例）
            HookRegistry.Add(typeof(RemoteClient).GetMethod("Reset", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public),
                (Action<Action<RemoteClient>, RemoteClient>)((orig, self) =>
                {
                    UpdateNPCPostfix(self);
                    orig(self);
                }));
        }

        public static void UpdateNPCPostfix(RemoteClient __instance)
        {
            mod.ForTry(item => item.ResetPrefix(__instance));
        }
    }
}
