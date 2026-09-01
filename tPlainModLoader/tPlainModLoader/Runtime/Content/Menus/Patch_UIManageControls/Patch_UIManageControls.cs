using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.GameInput;
using Terraria.UI;
using TPML.Content;
using TPML.Content.Engine;

namespace tContentPatch.Content.Menus.Patch_UIManageControls
{
    /// <summary>
    /// 拦截 UIManageControls.OnActivate (进入控件设置菜单)，确保每次打开时动态刷新并注入模组快捷键分组
    /// </summary>
    internal static class Patch_UIManageControls
    {
        /// <summary>集中注册全部补丁（由 ContentPatch_Initialize 调用）</summary>
        public static void RegisterAll()
        {
            // UIManageControls.OnActivate()（实例，postfix）
            HookRegistry.Add(typeof(UIManageControls).GetMethod("OnActivate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic),
                (System.Action<System.Action<UIManageControls>, UIManageControls>)((orig, self) =>
                {
                    orig(self);
                    Postfix(self);
                }));
        }

        private static void Postfix(UIManageControls __instance)
        {
            var keybinds = KeybindLoader.Keybinds.ToList();
            if (keybinds.Count == 0) return;

            // 借助 Publicizer 直接强类型访问私有列表字段，移除旧的 Mod 分组防止重复
            RemoveOldModGroups(__instance._bindsKeyboard);
            RemoveOldModGroups(__instance._bindsGamepad);
            RemoveOldModGroups(__instance._bindsKeyboardUI);
            RemoveOldModGroups(__instance._bindsGamepadUI);

            int groupIndex = (__instance._bindsKeyboard?.Count ?? 5);

            __instance._bindsKeyboard?.Add(CreateModBindingGroup(groupIndex, keybinds, InputMode.Keyboard));
            __instance._bindsGamepad?.Add(CreateModBindingGroup(groupIndex, keybinds, InputMode.XBoxGamepad));
            __instance._bindsKeyboardUI?.Add(CreateModBindingGroup(groupIndex, keybinds, InputMode.KeyboardUI));
            __instance._bindsGamepadUI?.Add(CreateModBindingGroup(groupIndex, keybinds, InputMode.XBoxGamepadUI));

            // 强类型直接调用私有 FillList() 刷新当前正在展示的 UIList
            __instance.FillList();
            __instance.Recalculate();
        }

        private static void RemoveOldModGroups(List<UIElement> list)
        {
            if (list == null) return;
            list.RemoveAll(elem => elem is ModKeybindingGroupElement);
        }

        private class ModKeybindingGroupElement : UISortableElement
        {
            public ModKeybindingGroupElement(int order) : base(order) { }
        }

        private static UISortableElement CreateModBindingGroup(int elementIndex, List<ModKeybind> keybinds, InputMode currentInputMode)
        {
            var sortableElement = new ModKeybindingGroupElement(elementIndex);
            sortableElement.HAlign = 0.5f;
            sortableElement.Width.Set(0f, 1f);
            sortableElement.Height.Set(2000f, 0f);

            UIPanel groupPanel = new UIPanel();
            groupPanel.Width.Set(0f, 1f);
            groupPanel.Height.Set(-16f, 1f);
            groupPanel.VAlign = 1f;
            groupPanel.BackgroundColor = new Color(33, 43, 79) * 0.8f;
            groupPanel.BackgroundColor = Color.Lerp(groupPanel.BackgroundColor, Color.Goldenrod, 0.18f);

            sortableElement.Append(groupPanel);

            UIList uIList = new UIList();
            uIList.OverflowHidden = false;
            uIList.Width.Set(0f, 1f);
            uIList.Height.Set(-8f, 1f);
            uIList.VAlign = 1f;
            uIList.ListPadding = 5f;
            groupPanel.Append(uIList);

            Color itemColor = groupPanel.BackgroundColor.MultiplyRGBA(new Color(111, 111, 111));

            // 按模组名称分组
            var modGroups = keybinds.GroupBy(k => k.ModName);
            int itemIndex = 0;

            foreach (var group in modGroups)
            {
                // 1. 添加模组名称小标题
                UISortableElement headerElement = new UISortableElement(itemIndex++);
                headerElement.Width.Set(0f, 1f);
                headerElement.Height.Set(26f, 0f);
                headerElement.HAlign = 0.5f;

                UIText headerText = new UIText($"[ {group.Key} ]", 0.8f, true)
                {
                    HAlign = 0.5f,
                    VAlign = 0.5f,
                    TextColor = Color.Gold
                };
                headerElement.Append(headerText);
                uIList.Add(headerElement);

                // 2. 依次添加该模组下的快捷键项
                foreach (var keybind in group)
                {
                    UISortableElement rowElement = new UISortableElement(itemIndex++);
                    rowElement.Width.Set(0f, 1f);
                    rowElement.Height.Set(30f, 0f);
                    rowElement.HAlign = 0.5f;

                    UIKeybindingListItem bindItem = new UIKeybindingListItem(keybind.FullName, currentInputMode, itemColor);
                    bindItem.Width.Set(0f, 1f);
                    bindItem.Height.Set(0f, 1f);
                    bindItem.SetSnapPoint("Wide", itemIndex);

                    rowElement.Append(bindItem);
                    uIList.Add(rowElement);
                }
            }

            // 顶部分组大标题
            string groupTitle = "模组按键 (Mod Controls)";
            UITextPanel<string> titlePanel = new UITextPanel<string>(groupTitle, 0.7f)
            {
                VAlign = 0f,
                HAlign = 0.5f
            };
            sortableElement.Append(titlePanel);

            sortableElement.Recalculate();
            float totalHeight = uIList.GetTotalHeight();
            sortableElement.Width.Set(0f, 1f);
            sortableElement.Height.Set(totalHeight + 30f + 16f, 0f);
            sortableElement.Recalculate();

            return sortableElement;
        }
    }
}
