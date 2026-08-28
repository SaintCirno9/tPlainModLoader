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
    /// 专家 Debuff 时长还原（对齐 ImproveGame 语义）：专家/大师模式下部分 debuff 时长
    /// 被 ×2/×2.5（Player.AddBuff_DetermineBuffTimeToAdd，Player.cs:5426，依据
    /// GameDifficultyData.DebuffTimeMultiplier），开启后一律按经典模式原时长。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class ExpertDebuffTime
    {
        public static GetSetReset<bool> Enable = new GetSetReset<bool>(false, false);

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
    }

    [HarmonyPatch(typeof(Player), nameof(Player.AddBuff_DetermineBuffTimeToAdd))]
    internal static class Patch_ExpertDebuffTime
    {
        [HarmonyPrefix]
        internal static bool Prefix(int type, int time, ref int __result)
        {
            if (!ExpertDebuffTime.Enable.val) return true;
            __result = time;
            return false;
        }
    }
}
