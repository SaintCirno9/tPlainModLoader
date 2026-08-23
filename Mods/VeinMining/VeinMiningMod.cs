using HarmonyLib;
using tContentPatch;
using tContentPatch.Patch;

namespace VeinMining
{
    /// <summary>
    /// 简单连锁挖矿模组入口类
    /// </summary>
    public class VeinMiningMod : Mod
    {
        private const string HarmonyId = "tPlainModLoader.Mod.VeinMining.gamePatch";
        private Harmony harmony;

        /// <summary>
        /// 注册 Harmony 补丁
        /// </summary>
        /// <param name="addPatch">补丁接口</param>
        public override void AddPatch(IAddPatch addPatch)
        {
            harmony = new Harmony(HarmonyId);
            harmony.PatchAll();
        }

        /// <summary>
        /// 模组卸载时清理补丁
        /// </summary>
        public override void Unload()
        {
            harmony?.UnpatchAll(HarmonyId);
            harmony = null;
        }
    }
}
