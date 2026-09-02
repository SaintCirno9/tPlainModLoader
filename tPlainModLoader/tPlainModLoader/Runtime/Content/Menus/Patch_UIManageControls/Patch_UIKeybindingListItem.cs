using Terraria.GameContent.UI.Elements;
using TPML.Content;

namespace TPML.UI.Menus.Patch_UIManageControls
{
    /// <summary>
    /// 拦截 UIKeybindingListItem.GetFriendlyName，为模组快捷键返回自定义中文友好名称
    /// 作者: SaintCirno9
    /// </summary>
    internal static class Patch_UIKeybindingListItem
    {
        private static bool _hooksInitialized = false;

        /// <summary>集中注册 UIKeybindingListItem 强类型 HookGen 钩子</summary>
        public static void RegisterAll()
        {
            if (_hooksInitialized) return;

            On_UIKeybindingListItem.GetFriendlyName += Hook_GetFriendlyName;

            _hooksInitialized = true;
        }

        private static string Hook_GetFriendlyName(On_UIKeybindingListItem.orig_GetFriendlyName orig, UIKeybindingListItem self)
        {
            string result = null;
            if (Prefix(self, ref result))
            {
                result = orig(self);
            }
            return result;
        }

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
