using CommandHelp;
using HarmonyLib;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL.Fishing
{
    /// <summary>
    /// 钓鱼宝匣掉落几率修改与倍率计算补丁
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch]
    internal class FishingCrateModifier
    {
        public static GetSetReset<bool> EnableGuaranteedCrate = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> EnableCrateMultiplier = new GetSetReset<bool>(false, false);
        public static GetSetReset<int> CrateChanceMultiplier = new GetSetReset<int>(2, 2, GetSetReset.GetIntFunc(1, 10));

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("guaranteedCrate", EnableGuaranteedCrate),
                CommandBuild.get1("crateChanceMult", EnableCrateMultiplier, CrateChanceMultiplier, new CommandInt())
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(EnableGuaranteedCrate, "钓鱼时 100% 必出宝匣（无论何种水域均优先钓起生物群落/通用宝匣）", "Images/Item_2334", "必出宝匣模式"),
                UIBuild.get1(EnableCrateMultiplier, CrateChanceMultiplier, int.Parse, "开启后将基础宝匣概率乘以该倍率 (1~10x)", "Images/Item_2356", "宝匣掉落倍率")
            };
        }

        /// <summary>
        /// 在原版 FishingCheck_RollDropLevels 结算后调整 crate 结果
        /// </summary>
        [HarmonyPatch(typeof(Projectile), "FishingCheck_RollDropLevels")]
        [HarmonyPostfix]
        public static void FishingCheck_RollDropLevelsPostfix(
            int fishingLevel,
            ref bool common,
            ref bool uncommon,
            ref bool rare,
            ref bool veryrare,
            ref bool legendary,
            ref bool crate)
        {
            if (EnableGuaranteedCrate.val)
            {
                crate = true;
                return;
            }

            if (!EnableCrateMultiplier.val || CrateChanceMultiplier.val <= 1 || crate)
                return;

            Player player = Main.myPlayer >= 0 && Main.myPlayer < Main.maxPlayers ? Main.player[Main.myPlayer] : null;
            int baseChance = 10;
            if (player != null && player.active && player.cratePotion)
            {
                baseChance += 15;
            }

            float targetChance = Math.Min(100f, baseChance * CrateChanceMultiplier.val);
            if (targetChance >= 100f)
            {
                crate = true;
                return;
            }

            float p = (targetChance - baseChance) / (100f - baseChance);
            if (Main.rand.NextDouble() < p)
            {
                crate = true;
            }
        }
    }
}