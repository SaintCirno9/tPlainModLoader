using System;
using System.Collections.Generic;
using Terraria;
using TPML.Content.Engine;
using TPML.Content.IO;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// WorldFile 存档生命周期补丁（M2 迁移：Harmony → MonoMod）
    /// </summary>
    internal class Patch_WorldFile : ListCopy<PatchWorldFile>
    {
        private static List<PatchWorldFile> mod = new List<PatchWorldFile>();

        public Patch_WorldFile() : base(mod) { }

        /// <summary>集中注册全部补丁（由 ContentPatch_Initialize 调用）</summary>
        public static void RegisterAll()
        {
            var worldFile = typeof(Terraria.IO.WorldFile);

            // WorldFile._SaveWorld(bool, bool, bool, bool)（静态）
            HookRegistry.Add(MethodLookup.Static(worldFile, "_SaveWorld", typeof(bool), typeof(bool), typeof(bool), typeof(bool)),
                (Action<Action<bool, bool, bool, bool>, bool, bool, bool, bool>)((orig, useCloudSaving, resetTime, useTemps, canBeSkipped) =>
                {
                    SaveWorldPrefix(useCloudSaving, resetTime, useTemps, canBeSkipped);
                    orig(useCloudSaving, resetTime, useTemps, canBeSkipped);
                    SaveWorldPostfix(useCloudSaving, resetTime, useTemps, canBeSkipped);
                }));

            // WorldFile.LoadWorld()（静态）
            HookRegistry.Add(worldFile.GetMethod("LoadWorld", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic),
                (Action<Action>)(orig =>
                {
                    orig();
                    LoadWorldPostfix();
                }));
        }

        public static void SaveWorldPrefix(bool useCloudSaving, bool resetTime, bool useTemps, bool canBeSkipped)
        {
            if (Main.netMode != 0 && Main.dedServ == false) return;

            ModItemSidecarEngine.OnWorldSavePrefix();
            mod.ForTry(item => item.SaveWorldPrefix(useCloudSaving, resetTime, useTemps, canBeSkipped));
        }

        public static void SaveWorldPostfix(bool useCloudSaving, bool resetTime, bool useTemps, bool canBeSkipped)
        {
            if (Main.netMode != 0 && Main.dedServ == false) return;

            ModItemSidecarEngine.OnWorldSavePostfix();
            mod.ForTry(item => item.SaveWorldPostfix(useCloudSaving, resetTime, useTemps, canBeSkipped));
        }

        public static void LoadWorldPostfix()
        {
            mod.ForTry(item => item.LoadWorldPostfix());
            ModItemSidecarEngine.OnWorldLoaded();
        }
    }
}
