using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;
using TPML.Content;
using TPML.Core.Logging;

namespace RecipeBrowser
{
    /// <summary>
    /// RecipeBrowser 核心底层 Hook 门控矩阵（基于 HookGen 强类型 On_ 门控，零反射，100% 对齐规范）
    /// 作者: SaintCirno9
    /// </summary>
    public static class RecipeBrowserHooks
    {
        private static readonly ILogger Logger = LogManager.GetLogger("RecipeBrowser");
        private static bool _registered = false;

        private static bool AdjTilesActive;
        private static bool[] oldAdjTile;
        private static bool oldAdjWater;
        private static bool oldAdjHoney;
        private static bool oldAdjLava;

        /// <summary>
        /// 集中注册全部 MonoMod 门控
        /// </summary>
        public static void RegisterAll()
        {
            if (_registered) return;

            On_UIElement.GetClippingRectangle += Hook_GetClippingRectangle;
            On_Recipe.UpdateRecipeList += Hook_UpdateRecipeList;
            On_Player.AdjTiles += Hook_AdjTiles;

            _registered = true;
            Logger.Info("★ RecipeBrowser MonoMod On_ 门控已成功注册");
        }

        /// <summary>
        /// 集中注销全部 MonoMod 门控
        /// </summary>
        public static void UnregisterAll()
        {
            if (!_registered) return;

            On_UIElement.GetClippingRectangle -= Hook_GetClippingRectangle;
            On_Recipe.UpdateRecipeList -= Hook_UpdateRecipeList;
            On_Player.AdjTiles -= Hook_AdjTiles;

            _registered = false;
        }

        /// <summary>
        /// 全局保护 ScissorRectangle 范围，杜绝 XNA set_ScissorRectangle 抛出 ArgumentException (The scissor rectangle is invalid)
        /// </summary>
        private static Rectangle Hook_GetClippingRectangle(On_UIElement.orig_GetClippingRectangle orig, UIElement self, SpriteBatch spriteBatch)
        {
            Rectangle result = orig(self, spriteBatch);
            try
            {
                if (spriteBatch?.GraphicsDevice == null) return result;
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

                result.X = Utils.Clamp(result.X, 0, maxW - 1);
                result.Y = Utils.Clamp(result.Y, 0, maxH - 1);
                result.Width = Utils.Clamp(result.Width, 1, maxW - result.X);
                result.Height = Utils.Clamp(result.Height, 1, maxH - result.Y);
            }
            catch { }
            return result;
        }

        private static void Hook_UpdateRecipeList(On_Recipe.orig_UpdateRecipeList orig)
        {
            orig();

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

        private static void Hook_AdjTiles(On_Player.orig_AdjTiles orig, Player self)
        {
            AdjTilesActive = true;
            try
            {
                orig(self);

                Player localPlayer = Main.LocalPlayer;
                if (self != null && self == localPlayer && localPlayer?.adjTile != null && !Main.playerInventory && RecipeBrowserUI.instance != null && RecipeBrowserUI.instance.ShowRecipeBrowser)
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
