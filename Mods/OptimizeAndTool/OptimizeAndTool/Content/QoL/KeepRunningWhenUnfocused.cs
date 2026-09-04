using CommandHelp;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 失焦时保持游戏运行（对齐 ImproveGame 语义，基于 HookGen 强类型 On_ 门控）：单人游戏窗口失焦时不再暂停。
    /// 原版 FocusHelper.UpdateFocus 在失焦 && 单机 && 未开启"失焦运行"时置 wantsToPause=true，主循环随即 gamePaused。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class KeepRunningWhenUnfocused
    {
        public static GetSetReset<bool> Enable = new GetSetReset<bool>(false, false);
        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_FocusHelper.UpdateFocus += Hook_UpdateFocus;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_FocusHelper.UpdateFocus -= Hook_UpdateFocus;
            _registered = false;
        }

        private static void Hook_UpdateFocus(On_FocusHelper.orig_UpdateFocus orig, out bool wantsToPause)
        {
            orig(out wantsToPause);
            if (Enable.val)
            {
                wantsToPause = false;
            }
        }

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("keepRunningWhenUnfocused", Enable)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(Enable, "单人游戏窗口失焦（后台运行）时不再暂停游戏", "Images/Item_1621", "失焦时保持游戏运行")
            };
        }
    }
}
