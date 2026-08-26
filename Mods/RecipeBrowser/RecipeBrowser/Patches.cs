using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;

namespace RecipeBrowser
{
    [HarmonyPatch]
    public static class Patches
    {
        private static bool AdjTilesActive;
        private static bool[] oldAdjTile;
        private static bool oldAdjWater;
        private static bool oldAdjHoney;
        private static bool oldAdjLava;

        /// <summary>
        /// 全局保护 ScissorRectangle 范围，杜绝 XNA set_ScissorRectangle 抛出 ArgumentException (The scissor rectangle is invalid)
        /// </summary>
        [HarmonyPatch(typeof(UIElement), nameof(UIElement.GetClippingRectangle))]
        [HarmonyPostfix]
        public static void UIElement_GetClippingRectangle_Postfix(SpriteBatch spriteBatch, ref Rectangle __result)
        {
            try
            {
                if (spriteBatch?.GraphicsDevice == null) return;
                var gd = spriteBatch.GraphicsDevice;
                int maxW = gd.Viewport.Width;
                int maxH = gd.Viewport.Height;
                var renderTargets = gd.GetRenderTargets();
                if (renderTargets != null && renderTargets.Length > 0 && renderTargets[0].RenderTarget is Texture2D rt)
                {
                    maxW = rt.Width;
                    maxH = rt.Height;
                }

                if (maxW <= 0) maxW = 1;
                if (maxH <= 0) maxH = 1;

                __result.X = Utils.Clamp(__result.X, 0, maxW - 1);
                __result.Y = Utils.Clamp(__result.Y, 0, maxH - 1);
                __result.Width = Utils.Clamp(__result.Width, 1, maxW - __result.X);
                __result.Height = Utils.Clamp(__result.Height, 1, maxH - __result.Y);
            }
            catch { }
        }

        [HarmonyPatch(typeof(Recipe), nameof(Recipe.UpdateRecipeList))]
        [HarmonyPostfix]
        public static void Recipe_UpdateRecipeList_Postfix()
        {
            if (!AdjTilesActive && RecipeCatalogueUI.instance != null && RecipePath.extendedCraft)
            {
                RecipeCatalogueUI.instance.InvalidateExtendedCraft();
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.AdjTiles))]
        [HarmonyPrefix]
        public static void Player_AdjTiles_Prefix(Player __instance)
        {
            AdjTilesActive = true;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.AdjTiles))]
        [HarmonyPostfix]
        public static void Player_AdjTiles_Postfix(Player __instance)
        {
            try
            {
                if (__instance == Main.LocalPlayer && !Main.playerInventory && RecipeBrowserUI.instance != null && RecipeBrowserUI.instance.ShowRecipeBrowser)
                {
                    if (oldAdjTile == null || oldAdjTile.Length != Main.LocalPlayer.adjTile.Length)
                    {
                        oldAdjTile = new bool[Main.LocalPlayer.adjTile.Length];
                        for (int i = 0; i < oldAdjTile.Length; i++) oldAdjTile[i] = Main.LocalPlayer.adjTile[i];
                        oldAdjWater = Main.LocalPlayer.adjWaterSource;
                        oldAdjHoney = Main.LocalPlayer.adjHoney;
                        oldAdjLava = Main.LocalPlayer.adjLava;
                        return;
                    }

                    bool changed = false;
                    for (int i = 0; i < oldAdjTile.Length; i++)
                    {
                        if (oldAdjTile[i] != Main.LocalPlayer.adjTile[i])
                        {
                            changed = true;
                            oldAdjTile[i] = Main.LocalPlayer.adjTile[i];
                        }
                    }
                    if (oldAdjWater != Main.LocalPlayer.adjWaterSource) { changed = true; oldAdjWater = Main.LocalPlayer.adjWaterSource; }
                    if (oldAdjHoney != Main.LocalPlayer.adjHoney) { changed = true; oldAdjHoney = Main.LocalPlayer.adjHoney; }
                    if (oldAdjLava != Main.LocalPlayer.adjLava) { changed = true; oldAdjLava = Main.LocalPlayer.adjLava; }

                    if (changed)
                    {
                        Recipe.UpdateRecipeList();
                    }
                }
            }
            finally
            {
                AdjTilesActive = false;
            }
        }
    }
}
