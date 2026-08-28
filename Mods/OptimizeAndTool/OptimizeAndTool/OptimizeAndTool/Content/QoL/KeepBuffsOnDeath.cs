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
    /// 死亡保存增益（对齐 ImproveGame 语义）：玩家死亡后增益不再被清除。
    /// 原版 Player.UpdateDead（Player.cs:17109-17116）每帧清空全部非持久 buff，
    /// 这里 Prefix 快照 buffType/buffTime，Postfix 写回（死亡期间 buff 冻结，复活后保留）。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class KeepBuffsOnDeath
    {
        public static GetSetReset<bool> Enable = new GetSetReset<bool>(false, false);

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("keepBuffsOnDeath", Enable)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(Enable, "死亡后保留全部增益（含战斗增益），复活后继续生效", "Images/Item_316", "死亡保存增益")
            };
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.UpdateDead))]
    internal static class Patch_KeepBuffsOnDeath
    {
        private static int[] savedBuffType = null;
        private static int[] savedBuffTime = null;

        [HarmonyPrefix]
        internal static void Prefix(Player __instance)
        {
            if (!KeepBuffsOnDeath.Enable.val) return;
            savedBuffType = (int[])__instance.buffType.Clone();
            savedBuffTime = (int[])__instance.buffTime.Clone();
        }

        [HarmonyFinalizer]
        internal static void Finalizer(Player __instance)
        {
            if (!KeepBuffsOnDeath.Enable.val || savedBuffType == null) return;
            for (int i = 0; i < __instance.buffType.Length; i++)
            {
                if (savedBuffType[i] > 0)
                {
                    __instance.buffType[i] = savedBuffType[i];
                    __instance.buffTime[i] = savedBuffTime[i];
                }
            }
            savedBuffType = null;
            savedBuffTime = null;
        }
    }
}
