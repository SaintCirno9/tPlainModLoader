using CommandHelp;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 专家 Debuff 时长还原门控（对齐 ImproveGame 语义，基于 HookGen 强类型 On_ 门控）：专家/大师模式下部分 debuff 时长
    /// 被 ×2/×2.5（Player.AddBuff_DetermineBuffTimeToAdd，Player.cs:5426，依据
    /// GameDifficultyData.DebuffTimeMultiplier），开启后一律按经典模式原时长。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class ExpertDebuffTimeHooks
    {
        public static GetSetReset<bool> Enable = new GetSetReset<bool>(false, false);

        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_Player.AddBuff_DetermineBuffTimeToAdd += Hook_AddBuff_DetermineBuffTimeToAdd;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_Player.AddBuff_DetermineBuffTimeToAdd -= Hook_AddBuff_DetermineBuffTimeToAdd;
            _registered = false;
        }

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("classicDebuffTime", Enable)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(Enable, "专家/大师模式下 Debuff 时长与经典模式一致（不再 ×2/×2.5）", "Images/Item_25", "专家 Debuff 时长还原经典")
            };
        }

        private static int Hook_AddBuff_DetermineBuffTimeToAdd(On_Player.orig_AddBuff_DetermineBuffTimeToAdd orig, Player self, int type, int time)
        {
            if (Enable.val)
            {
                return time;
            }

            return orig(self, type, time);
        }
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    internal static class ExpertDebuffTime
    {
        public static GetSetReset<bool> Enable => ExpertDebuffTimeHooks.Enable;

        public static List<CommandObject> GetCO() => ExpertDebuffTimeHooks.GetCO();
        public static List<UIElement> GetUI() => ExpertDebuffTimeHooks.GetUI();
    }
}
