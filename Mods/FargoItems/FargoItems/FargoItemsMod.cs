using System;
using Microsoft.Xna.Framework;
using Terraria;
using TPML.Content;
using TPML.Core.Logging;
using FargoItems.Content.Logic;

namespace FargoItems
{
    /// <summary>
    /// tPlainModLoader 原生 Mod 加载器入口
    /// </summary>
    public class FargoItemsTPMLEntry : tContentPatch.Mod
    {
        private static readonly ILogger Logger = LogManager.GetLogger("FargoItems");
        public static FargoItemsMod ModInstance { get; private set; }

        public override void Load()
        {
            try
            {
                Logger.Info("===== 开始载入 FargoItems Mod =====");

                // 内容模组由统一 ContentHost 自动注册并触发 Load，入口只保留旧引擎钩子职责
                ModInstance = TPML.Content.ContentHost.Find<FargoItemsMod>();

                Logger.Info("===== 模组物品注册完成 =====");
            }
            catch (Exception ex)
            {
                Logger.Error("载入异常", ex);
            }
        }

        public override void Unload()
        {
            Logger.Info("FargoItems 模组已卸载");
        }
    }

    /// <summary>
    /// 玩家进入世界通知与材质状态保障
    /// </summary>
    public class FargoItemsPlayerNotice : tContentPatch.PatchPlayer
    {
        private bool _announced = false;

        public override void UpdatePrefix(Player This, int playerI)
        {
            if (!_announced && This.whoAmI == Main.myPlayer && !Main.gameMenu)
            {
                _announced = true;
                Main.NewText($"[FargoItems] 地狱直通车已就绪！3 款直通车配方已载入 (配方总数: {Recipe.numRecipes})", 255, 180, 80);
                
                // 保证材质在世界加载后完全绑定
                ItemLoader.ReloadTextures();
            }
        }
    }

    /// <summary>
    /// 主循环前置贴图就绪保障
    /// </summary>
    public class FargoItemsPatchMain : tContentPatch.PatchMain
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
            try
            {
                Logger.Info("开始注册内容...");
                AddContent(new Content.Items.Instavator());
                Logger.Info("注册 Instavator 完成");
                AddContent(new Content.Items.HalfInstavator());
                Logger.Info("注册 HalfInstavator 完成");
                AddContent(new Content.Items.DoubleObsidianInstavator());
                Logger.Info("注册 DoubleObsidianInstavator 完成");
                AddContent(new Content.Systems.InstaVisualSystem());
                Logger.Info("注册 InstaVisualSystem 完成");
            }
            catch (Exception ex)
            {
                Logger.Error("注册内容异常", ex);
            }
        }

        public override void PostSetupContent()
        {
            // 由框架统一 RecipeLoader 管理，无需手动调用
        }
    }
}
