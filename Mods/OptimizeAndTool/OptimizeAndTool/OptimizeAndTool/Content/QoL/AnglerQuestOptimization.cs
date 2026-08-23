using CommandHelp;
using HarmonyLib;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 渔夫任务与钓鱼机制优化补丁
    /// 包含：提交任务鱼立即刷新无冷却、任务鱼解除唯一限制支持堆叠与重复钓取、水池无渔力大小惩罚（强制按 300 格满水域计算）
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch]
    internal class AnglerQuestOptimization
    {
        public static GetSetReset<bool> EnableNoAnglerCooldown = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableQuestFishStack = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableNoFishingPenalty = new GetSetReset<bool>(true, true);

        /// <summary>
        /// 钓鱼探测任务鱼时的标记上下文
        /// </summary>
        private static bool isProbingQuestFish = false;

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("noAnglerCooldown", EnableNoAnglerCooldown),
                CommandBuild.get2("questFishStack", EnableQuestFishStack),
                CommandBuild.get2("noFishingPenalty", EnableNoFishingPenalty)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(EnableNoAnglerCooldown, "提交渔夫任务后立即刷新下一个任务，无需等待至次日凌晨", "Images/Item_2422", "渔夫任务无冷却"),
                UIBuild.get2(EnableQuestFishStack, "任务鱼解除唯一限制，支持堆叠至 9999 且背包持有任务鱼时仍可继续钓取", "Images/Item_2450", "任务鱼堆叠与无唯一限制"),
                UIBuild.get2(EnableNoFishingPenalty, "消除水池过小对渔力的惩罚，任何水体均强制按满 300 格计算渔力", "Images/Item_2294", "水池无渔力惩罚")
            };
        }

        #region 1. 渔夫任务立即刷新无冷却

        /// <summary>
        /// 提交任务鱼后立即重置任务完成状态并刷新下一个任务
        /// </summary>
        [HarmonyPatch(typeof(Main), nameof(Main.NPCChatText_DoAnglerQuest))]
        [HarmonyPostfix]
        public static void NPCChatText_DoAnglerQuestPostfix()
        {
            if (!EnableNoAnglerCooldown.val) return;

            Main.anglerQuestFinished = false;
            Main.anglerWhoFinishedToday?.Clear();
            Main.AnglerQuestSwap();
            Main.NewText("[c/00FFDD:渔夫任务已立即刷新！可再次对话接取新的钓鱼任务。]");
        }

        #endregion

        #region 2. 任务鱼解除唯一限制与重复钓取

        /// <summary>
        /// 拦截钓鱼射弹探测任务鱼的上下文
        /// </summary>
        [HarmonyPatch(typeof(Projectile), "FishingCheck_ProbeForQuestFish")]
        [HarmonyPrefix]
        public static void FishingCheck_ProbeForQuestFishPrefix()
        {
            isProbingQuestFish = true;
        }

        [HarmonyPatch(typeof(Projectile), "FishingCheck_ProbeForQuestFish")]
        [HarmonyPostfix]
        public static void FishingCheck_ProbeForQuestFishPostfix()
        {
            isProbingQuestFish = false;
        }

        /// <summary>
        /// 在探测任务鱼时让 HasItem 返回 false，使游戏允许继续钓取多条相同的任务鱼
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.HasItem), typeof(int))]
        [HarmonyPrefix]
        public static bool HasItemPrefix(int type, ref bool __result)
        {
            if (isProbingQuestFish && EnableQuestFishStack.val)
            {
                __result = false;
                return false; // 拦截并返回未持有，允许钓取任务鱼
            }
            return true;
        }

        #endregion

        #region 3. 水池无渔力大小惩罚

        /// <summary>
        /// 强制水域格数至少为 300 格，完全消除湖泊大小渔力惩罚
        /// </summary>
        [HarmonyPatch(typeof(Projectile), "GetFishingPondState")]
        [HarmonyPostfix]
        public static void GetFishingPondStatePostfix(int x, int y, ref bool lava, ref bool honey, ref int numWaters, ref int chumCount)
        {
            if (EnableNoFishingPenalty.val && numWaters > 0)
            {
                numWaters = Math.Max(numWaters, 300);
            }
        }

        #endregion
    }
}
