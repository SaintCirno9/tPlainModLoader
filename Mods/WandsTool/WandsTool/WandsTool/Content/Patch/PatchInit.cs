using HarmonyLib;
using tContentPatch;
using tContentPatch.Patch;

namespace WandsTool.Content.Patch
{
    /// <summary>
    /// 魔杖工具 Harmony 补丁初始化与生命周期管理
    /// </summary>
    internal class PatchInit : Mod
    {
        private const string patchId = "tPlainModLoader.Mod.WandsTool.ActionIsolation";
        private Harmony harmony = null;

        public override void AddPatch(IAddPatch addPatch)
        {
            harmony = new Harmony(patchId);
            harmony.PatchAll();
        }

        public override void Unload()
        {
            harmony?.UnpatchSelf();
            harmony = null;
        }
    }
}
