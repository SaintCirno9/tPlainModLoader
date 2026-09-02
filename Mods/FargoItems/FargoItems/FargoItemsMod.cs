using System;
using Microsoft.Xna.Framework;
using Terraria;
using TPML.Content;
using TPML.Core.Logging;
using FargoItems.Content.Logic;

namespace FargoItems
{
    /// <summary>
    /// 玩家进入世界通知与材质状态保障
    /// </summary>
    public class FargoItemsPlayerNotice : TPML.Content.ModPlayer
    {
        private bool _announced = false;

        public override void UpdatePrefix(Player This, int playerI)
        {
            if (!_announced && This.whoAmI == Main.myPlayer && !Main.gameMenu)
            {
                _announced = true;
                Main.NewText($"[FargoItems] Fargo 实用物品与建筑矩阵已就绪 (配方总数: {Recipe.numRecipes})", 255, 180, 80);
                
                // 保证材质在世界加载后完全绑定
                ItemLoader.ReloadTextures();
            }
        }
    }

    /// <summary>
    /// 主循环前置贴图就绪保障
    /// </summary>
    public class FargoItemsPatchMain : TPML.Content.ModSystem
    {
        private static bool _texturesReady = false;

        public override void UpdatePrefix(Microsoft.Xna.Framework.GameTime gameTime)
        {
            InstavatorShaftBuilder.Update();

            if (!_texturesReady)
            {
                if (Main.instance?.GraphicsDevice != null || Main.spriteBatch?.GraphicsDevice != null)
                {
                    _texturesReady = true;
                    ItemLoader.ReloadTextures();
                }
            }
        }
    }

    /// <summary>
    /// TPML 原生 Mod 内容定义
    /// </summary>
    public class FargoItemsMod : TPML.Content.Mod
    {
        public override string Name => "FargoItems";
        public override string DisplayName => "Fargo's Items";

        public override void Load()
        {
            Logger.Info("[FargoItems] 模组已由统一 ContentHost 载入，自动扫描并注册模组内容矩阵");
        }

        public override void PostSetupContent()
        {
            // 由框架统一 RecipeLoader 管理，无需手动调用
        }
    }
}
