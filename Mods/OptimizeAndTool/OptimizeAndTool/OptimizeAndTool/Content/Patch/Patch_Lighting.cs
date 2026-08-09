using HarmonyLib;
using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace OptimizeAndTool.Content.Patch
{
    [HarmonyPatch(typeof(Lighting))]
    internal static class Patch_Lighting
    {
        [HarmonyPatch(nameof(Lighting.GetColor), new Type[] { typeof(int), typeof(int) })]
        [HarmonyPrefix]
        public static bool GetColorPrefix(ref Color __result)
        {
            return PatchGameViewMatrixZoomLimit.GetColorPrefix(ref __result);
        }
    }
}
