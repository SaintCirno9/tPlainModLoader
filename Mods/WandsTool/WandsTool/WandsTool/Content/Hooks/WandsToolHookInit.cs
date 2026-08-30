using tContentPatch;
using tContentPatch.Patch;

namespace WandsTool.Content.Hooks
{
    /// <summary>
    /// 魔杖工具 MonoMod 门控初始化与生命周期管理
    /// </summary>
    internal class WandsToolHookInit : Mod
    {
        public override void AddPatch(IAddPatch addPatch)
        {
            HookPlayerAction.RegisterAll();
        }

        public override void Unload()
        {
            HookPlayerAction.UnregisterAll();
        }
    }
}
