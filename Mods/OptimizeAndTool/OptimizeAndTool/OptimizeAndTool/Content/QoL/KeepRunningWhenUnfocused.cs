using CommandHelp;
using HarmonyLib;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 失焦时保持游戏运行（对齐 ImproveGame 语义）：单人游戏窗口失焦时不再暂停。
    /// 原版 FocusHelper.UpdateFocus（FocusHelper.cs:133）在失焦 && 单机 && 未开启"失焦运行"时
    /// 置 wantsToPause=true，主循环随即 gamePaused。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class KeepRunningWhenUnfocused
    {
        public static GetSetReset<bool> Enable = new GetSetReset<bool>(false, false);

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

    /// <summary>
    /// 失焦保持运行：Postfix 覆写 wantsToPause=false（原方法仍正常更新 IsSelectedApplication 与鼠标可见性，
    /// 只是不让主循环进入 gamePaused）。
    /// </summary>
    [HarmonyPatch(typeof(FocusHelper), nameof(FocusHelper.UpdateFocus))]
    internal static class Patch_KeepRunningWhenUnfocused
    {
        [HarmonyPostfix]
        internal static void Postfix(ref bool wantsToPause)
        {
            if (!KeepRunningWhenUnfocused.Enable.val) return;
            wantsToPause = false;
        }
    }
}
