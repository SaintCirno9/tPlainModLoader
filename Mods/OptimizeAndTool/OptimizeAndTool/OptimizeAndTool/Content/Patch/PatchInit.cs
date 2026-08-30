using HarmonyLib;
using System;
using System.Reflection;
using OptimizeAndTool.Content.Storage.ItemContainer;
using tContentPatch;
using tContentPatch.Patch;
using TPML.Content;
using OptimizeAndTool.Content.Storage.AccessoryBox;

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
                // 内容模组由统一 ContentHost 自动注册并触发 Load，入口只保留旧引擎钩子职责
                ContentModInstance = ContentHost.Find<OptimizeAndToolContentMod>();
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[OptimizeAndTool] Content 注册异常: {ex}");
            }
        }

        public override void AddPatch(IAddPatch addPatch) // 添加修补
        {
            harmony = new Harmony(patchId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public override void Unload()
        {
            harmony?.UnpatchSelf(); // 卸载修补
            harmony = null;
        }
    }

    public class OptimizeAndToolContentMod : TPML.Content.Mod
    {
        public override string Name => "OptimizeAndTool";
        public override string DisplayName => "优化与实用工具 (OptimizeAndTool)";

        public override void Load()
        {
            try
            {
                AddContent(new PotionBagItem());
                AddContent(new BannerChestItem());
                AddContent(new AccessoryBagItem());
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[OptimizeAndToolContentMod] 注册内容异常: {ex}");
            }
        }
    }
}
