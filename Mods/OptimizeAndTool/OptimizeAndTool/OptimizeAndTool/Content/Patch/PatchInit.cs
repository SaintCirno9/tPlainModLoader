using HarmonyLib;
using tContentPatch;
using tContentPatch.Patch;

namespace OptimizeAndTool.Content.Patch
{
    internal class PatchInit : Mod
    {
        private const string patchId = "tPlainModLoader.Mod.OptimizeAndTool.gamePatch";//修补的id
        private Harmony harmony = null;

        public override void AddPatch(IAddPatch addPatch)//添加修补
        {
            harmony = new Harmony(patchId);
            harmony.PatchAll();//修补全部
        }

        public override void Unload()
        {
            harmony?.UnpatchAll(patchId);//卸载修补
            harmony = null;
        }
    }
}
