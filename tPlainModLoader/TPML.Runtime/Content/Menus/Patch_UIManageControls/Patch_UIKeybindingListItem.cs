using System;
using tContentPatch.Input;
using Terraria.GameContent.UI.Elements;
using TPML.Content.Engine;

namespace tContentPatch.Content.Menus.Patch_UIManageControls
{
    /// <summary>
    /// 拦截 UIKeybindingListItem.GetFriendlyName，为模组快捷键返回自定义中文友好名称
    /// </summary>
    internal static class Patch_UIKeybindingListItem
    {
        /// <summary>集中注册全部补丁（由 ContentPatch_Initialize 调用）</summary>
        public static void RegisterAll()
        {
            // UIKeybindingListItem.GetFriendlyName()（实例，返回 string，prefix 返回 false 跳过）
            HookRegistry.Add(MethodLookup.Instance(typeof(UIKeybindingListItem), "GetFriendlyName"),
                (Func<Func<UIKeybindingListItem, string>, UIKeybindingListItem, string>)((orig, self) =>
                {
                    string result = null;
                    if (Prefix(self, ref result)) result = orig(self);
                    return result;
                }));
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
