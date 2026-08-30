using CommandHelp;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 无条件队内传送门控（对齐 ImproveGame 语义，基于 HookGen 强类型 On_ 门控）：旁观模式选中队友后传送不再受
    /// "同队/存活/携带虫洞药水"等条件约束，且不消耗虫洞药水。
    /// 原版链路：CanWormholeToSpectating（Player.cs:17267）→ UnityTeleport → TakeUnityPotion（42644）。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class NoConditionTeamTPHooks
    {
        public static GetSetReset<bool> Enable = new GetSetReset<bool>(false, false);

        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_Player.CanWormholeToSpectating += Hook_CanWormholeToSpectating;
            On_Player.TakeUnityPotion += Hook_TakeUnityPotion;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_Player.CanWormholeToSpectating -= Hook_CanWormholeToSpectating;
            On_Player.TakeUnityPotion -= Hook_TakeUnityPotion;
            _registered = false;
        }

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("noConditionTeamTP", Enable)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(Enable, "队内传送无视条件（无需虫洞药水、不消耗、无视队伍/死亡限制）", "Images/Item_2997", "无条件队内传送")
            };
        }

        private static bool Hook_CanWormholeToSpectating(On_Player.orig_CanWormholeToSpectating orig, Player self)
        {
            if (Enable.val)
            {
                return true;
            }

            return orig(self);
        }

        private static void Hook_TakeUnityPotion(On_Player.orig_TakeUnityPotion orig, Player self)
        {
            if (Enable.val)
            {
                return;
            }

            orig(self);
        }
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    internal static class NoConditionTeamTP
    {
        public static GetSetReset<bool> Enable => NoConditionTeamTPHooks.Enable;

        public static List<CommandObject> GetCO() => NoConditionTeamTPHooks.GetCO();
        public static List<UIElement> GetUI() => NoConditionTeamTPHooks.GetUI();
    }
}
