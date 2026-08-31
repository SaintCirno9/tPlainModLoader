using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using tContentPatch.Utils;
using Terraria;
using Terraria.Graphics.Light;
using Terraria.Utilities;
using TPML.Content.Engine;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// 光照扫描补丁（M2 迁移：Harmony → MonoMod，含 ref 参数）
    /// </summary>
    internal class Patch_TileLightScanner : ListCopy<PatchTileLightScanner>
    {
        private static List<PatchTileLightScanner> mod = new List<PatchTileLightScanner>();

        public Patch_TileLightScanner() : base(mod) { }

        private delegate void Orig_ApplyTileLight(TileLightScanner self, Tile tile, int x, int y, ref FastRandom localRandom, ref Vector3 lightColor);
        private delegate void Hook_ApplyTileLight(Orig_ApplyTileLight orig, TileLightScanner self, Tile tile, int x, int y, ref FastRandom localRandom, ref Vector3 lightColor);

        /// <summary>集中注册全部补丁（由 ContentPatch_Initialize 调用）</summary>
        public static void RegisterAll()
        {
            // TileLightScanner.ApplyTileLight(Tile, int, int, ref FastRandom, ref Vector3)（实例，ref 参数自定义委托）
            HookRegistry.Add(MethodLookup.Instance(typeof(TileLightScanner), "ApplyTileLight",
                    typeof(Tile), typeof(int), typeof(int),
                    typeof(FastRandom).MakeByRefType(), typeof(Vector3).MakeByRefType()),
                (Hook_ApplyTileLight)ApplyTileLightHook);
        }

        private static void ApplyTileLightHook(Orig_ApplyTileLight orig, TileLightScanner self, Tile tile, int x, int y, ref FastRandom localRandom, ref Vector3 lightColor)
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
