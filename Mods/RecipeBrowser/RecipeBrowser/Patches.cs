using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;
using TPML.Content;

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
                try
                {
                    RecipeCatalogueUI.instance.InvalidateExtendedCraft();
                }
                catch
                {
                    // 防御性保护
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.AdjTiles))]
        [HarmonyPriority(Priority.First)]
        [HarmonyPrefix]
        public static bool Player_AdjTiles_Prefix(Player __instance)
        {
            AdjTilesActive = true;
            if (__instance != null)
            {
                __instance.SafeScanAdjTiles();
            }
            return false; // 阻断原版易抛 NRE 的 IL 执行，由 SafeScanAdjTiles 全量安全接管
        }

        [HarmonyPatch(typeof(Player), nameof(Player.AdjTiles))]
        [HarmonyPostfix]
        public static void Player_AdjTiles_Postfix(Player __instance)
        {
            try
            {
                Player localPlayer = Main.LocalPlayer;
                if (__instance != null && __instance == localPlayer && localPlayer?.adjTile != null && !Main.playerInventory && RecipeBrowserUI.instance != null && RecipeBrowserUI.instance.ShowRecipeBrowser)
                {
                    if (oldAdjTile == null || oldAdjTile.Length != localPlayer.adjTile.Length)
                    {
                        oldAdjTile = new bool[localPlayer.adjTile.Length];
                        for (int i = 0; i < oldAdjTile.Length; i++) oldAdjTile[i] = localPlayer.adjTile[i];
                        oldAdjWater = localPlayer.adjWaterSource;
                        oldAdjHoney = localPlayer.adjHoney;
                        oldAdjLava = localPlayer.adjLava;
                        return;
                    }

                    bool changed = false;
                    for (int i = 0; i < oldAdjTile.Length; i++)
                    {
                        if (oldAdjTile[i] != localPlayer.adjTile[i])
                        {
                            changed = true;
                            oldAdjTile[i] = localPlayer.adjTile[i];
                        }
                    }
                    if (oldAdjWater != localPlayer.adjWaterSource) { changed = true; oldAdjWater = localPlayer.adjWaterSource; }
                    if (oldAdjHoney != localPlayer.adjHoney) { changed = true; oldAdjHoney = localPlayer.adjHoney; }
                    if (oldAdjLava != localPlayer.adjLava) { changed = true; oldAdjLava = localPlayer.adjLava; }

                    if (changed)
                    {
                        Recipe.UpdateRecipeList();
                    }
                }
            }
            catch
            {
                // 防御性保护：避免配方列表刷新异常影响游戏主循环
            }
            finally
            {
                AdjTilesActive = false;
            }
        }
    }
}
