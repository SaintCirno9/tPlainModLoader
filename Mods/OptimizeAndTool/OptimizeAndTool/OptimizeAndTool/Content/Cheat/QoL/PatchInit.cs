using HarmonyLib;
using System.Reflection;
using tContentPatch;
using tContentPatch.Patch;

namespace OptimizeAndTool.Content.Cheat.QoL
{
    /// <summary>
    /// Harmony 补丁初始化与生命周期管理
    /// 作者: SaintCirno9
    /// </summary>
    internal class PatchInit : Mod
    {
        private const string patchId = "tPlainModLoader.Mod.OptimizeAndTool.Patch";
        private Harmony harmony = null;

        public override void AddPatch(IAddPatch addPatch)
        {
            harmony = new Harmony(patchId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public override void Unload()
        {
            harmony?.UnpatchAll(patchId);
            harmony = null;
        }
    }
}
