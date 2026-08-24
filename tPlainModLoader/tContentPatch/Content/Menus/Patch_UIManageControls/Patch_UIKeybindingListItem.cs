using HarmonyLib;
using tContentPatch.Input;
using Terraria.GameContent.UI.Elements;

namespace tContentPatch.Content.Menus.Patch_UIManageControls
{
    /// <summary>
    /// 拦截 UIKeybindingListItem.GetFriendlyName，为模组快捷键返回自定义中文友好名称
    /// </summary>
    [HarmonyPatch(typeof(UIKeybindingListItem), "GetFriendlyName")]
    internal static class Patch_UIKeybindingListItem
    {
        private static bool Prefix(UIKeybindingListItem __instance, ref string __result)
        {
            // 通过 Publicizer 直接强类型访问私有字段 _keybind
            string bind = __instance._keybind;
            if (!string.IsNullOrEmpty(bind) && KeybindLoader.TryGetKeybind(bind, out var keybind))
            {
                __result = !string.IsNullOrEmpty(keybind.DisplayName) ? keybind.DisplayName : keybind.Name;
                return false;
            }

            return true;
        }
    }
}
