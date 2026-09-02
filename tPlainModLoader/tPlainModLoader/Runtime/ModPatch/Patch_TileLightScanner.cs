#pragma warning disable CS0618
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using tContentPatch.Utils;
using Terraria;
using Terraria.Graphics.Light;
using Terraria.Utilities;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// 光照扫描强类型门面调度类
    /// 作者: SaintCirno9
    /// </summary>
    internal class Patch_TileLightScanner : ListCopy<PatchTileLightScanner>
    {
        private static readonly List<PatchTileLightScanner> mod = new List<PatchTileLightScanner>();
        internal static List<PatchTileLightScanner> ModList => mod;
        private static bool _hooksInitialized = false;

        public Patch_TileLightScanner() : base(mod) { }

        /// <summary>集中注册 TileLightScanner 强类型 HookGen 钩子</summary>
        public static void RegisterAll()
        {
            if (_hooksInitialized) return;

            On_TileLightScanner.ApplyTileLight += Hook_ApplyTileLight;

            _hooksInitialized = true;
        }

        private static void Hook_ApplyTileLight(On_TileLightScanner.orig_ApplyTileLight orig, TileLightScanner self, Tile tile, int x, int y, ref FastRandom localRandom, ref Vector3 lightColor)
        {
            ApplyTileLightPrefix(tile, x, y, ref localRandom, ref lightColor);
            orig(self, tile, x, y, ref localRandom, ref lightColor);
        }

        public static void ApplyTileLightPrefix(Tile tile, int x, int y, ref FastRandom localRandom, ref Vector3 lightColor)
        {
            try
            {
                foreach (PatchTileLightScanner item in mod) item.ApplyTileLightPrefix(tile, x, y, ref localRandom, ref lightColor);
            }
            catch (Exception ex)
            {
                OutputDebug.OutputException(ex);
            }
        }
    }
}
