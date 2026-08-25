using HarmonyLib;
using System;
using System.Reflection;
using OptimizeAndTool.Content.Storage.ItemContainer;
using tContentPatch;
using tContentPatch.Patch;
using TPML.Content;
using TPML.Content.Engine;

namespace OptimizeAndTool.Content.Patch
{
    internal class PatchInit : tContentPatch.Mod
    {
        private const string patchId = "tPlainModLoader.Mod.OptimizeAndTool.gamePatch"; // 修补的id
        private Harmony harmony = null;
        public static OptimizeAndToolContentMod ContentModInstance { get; private set; }

        public override void Load()
        {
            try
            {
                ContentHookDispatcher.Initialize();
                if (ContentModInstance == null)
                {
                    ContentModInstance = new OptimizeAndToolContentMod();
                    ModContent.RegisterMod(ContentModInstance);
                    ContentModInstance.Load();
                }
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[OptimizeAndTool] Content 注册异常: {ex}");
            }
        }

        public override void Loaded()
        {
            try
            {
                RecipeLoader.SetupRecipes();
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[OptimizeAndTool] 配方注册异常: {ex}");
            }
        }

        public override void AddPatch(IAddPatch addPatch) // 添加修补
        {
            harmony = new Harmony(patchId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public override void Unload()
        {
            harmony?.UnpatchAll(patchId); // 卸载修补
            harmony = null;
        }
    }

    public class OptimizeAndToolContentMod : TPML.Content.Mod
    {
        public override void Load()
        {
            try
            {
                AddContent(new PotionBagItem());
                AddContent(new BannerChestItem());
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[OptimizeAndToolContentMod] 注册内容异常: {ex}");
            }
        }
    }
}
