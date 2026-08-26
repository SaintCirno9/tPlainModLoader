using System;
using Microsoft.Xna.Framework;
using Terraria;
using TPML.Content;
using TPML.Content.Engine;
using TPML.Core.Logging;
using Instavator.Content.Logic;

namespace Instavator
{
    /// <summary>
    /// tPlainModLoader 原生 Mod 加载器入口
    /// </summary>
    public class InstavatorTPMLEntry : tContentPatch.Mod
    {
        private static readonly ILogger Logger = LogManager.GetLogger("Instavator");
        public static InstavatorMod ModInstance { get; private set; }

        public override void Load()
        {
            try
            {
                Logger.Info("===== 开始载入 Instavator Mod =====");

                // 1. 初始化 ContentHookDispatcher（挂钩 SetDefaults, ItemCheck, Tooltips 等）
                ContentHookDispatcher.Initialize();

                // 2. 实例化并注册 Mod 内容
                ModInstance = new InstavatorMod();
                ModContent.RegisterMod(ModInstance);
                ModInstance.Load();

                Logger.Info("===== 模组物品注册完成 =====");
            }
            catch (Exception ex)
            {
                Logger.Error("载入异常", ex);
            }
        }

        public override void Loaded()
        {
            try
            {
                // 在所有内容加载完成后由框架统一触发配方构建与注入
                RecipeLoader.SetupRecipes();
                Logger.Info($"★ 配方注入流程结束，当前全局配方数: {Recipe.numRecipes}");
            }
            catch (Exception ex)
            {
                Logger.Error("配方注入异常", ex);
            }
        }

        public override void Unload()
        {
            Logger.Info("Instavator 模组已卸载");
        }
    }

    /// <summary>
    /// 玩家进入世界通知与材质状态保障
    /// </summary>
    public class InstavatorPlayerNotice : tContentPatch.PatchPlayer
    {
        private bool _announced = false;

        public override void UpdatePrefix(Player This, int playerI)
        {
            if (!_announced && This.whoAmI == Main.myPlayer && !Main.gameMenu)
            {
                _announced = true;
                Main.NewText($"[Instavator] 地狱直通车已就绪！3 款直通车配方已载入 (配方总数: {Recipe.numRecipes})", 255, 180, 80);
                
                // 保证材质在世界加载后完全绑定
                ItemLoader.ReloadTextures();
            }
        }
    }

    /// <summary>
    /// 主循环前置贴图就绪保障
    /// </summary>
    public class InstavatorPatchMain : tContentPatch.PatchMain
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
    public class InstavatorMod : TPML.Content.Mod
    {
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
