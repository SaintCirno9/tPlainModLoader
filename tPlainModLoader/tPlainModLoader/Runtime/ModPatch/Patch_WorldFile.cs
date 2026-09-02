#pragma warning disable CS0618
using System.Collections.Generic;
using Terraria;
using Terraria.IO;
using TPML.Content.IO;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// WorldFile 存档生命周期强类型门面调度类
    /// 作者: SaintCirno9
    /// </summary>
    internal class Patch_WorldFile : ListCopy<PatchWorldFile>
    {
        private static readonly List<PatchWorldFile> mod = new List<PatchWorldFile>();
        internal static List<PatchWorldFile> ModList => mod;
        private static bool _hooksInitialized = false;

        public Patch_WorldFile() : base(mod) { }

        /// <summary>集中注册 WorldFile 强类型 HookGen 钩子</summary>
        public static void RegisterAll()
        {
            if (_hooksInitialized) return;

            On_WorldFile._SaveWorld += Hook_SaveWorld;
            On_WorldFile.LoadWorld += Hook_LoadWorld;

            _hooksInitialized = true;
        }

        private static void Hook_SaveWorld(On_WorldFile.orig__SaveWorld orig, bool useCloudSaving, bool resetTime, bool useTemps, bool canBeSkipped)
        {
            SaveWorldPrefix(useCloudSaving, resetTime, useTemps, canBeSkipped);
            orig(useCloudSaving, resetTime, useTemps, canBeSkipped);
            SaveWorldPostfix(useCloudSaving, resetTime, useTemps, canBeSkipped);
        }

        private static void Hook_LoadWorld(On_WorldFile.orig_LoadWorld orig)
        {
            orig();
            LoadWorldPostfix();
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
