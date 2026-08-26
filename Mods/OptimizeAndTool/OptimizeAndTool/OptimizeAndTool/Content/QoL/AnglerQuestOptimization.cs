using CommandHelp;
using HarmonyLib;
using OptimizeAndTool.Content.Storage.ItemContainer;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Achievements;
using Terraria.ID;
using Terraria.UI;
using TPML.Content;
using BigBagSystem = OptimizeAndTool.Content.BigBag.BigBag;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 渔夫任务与钓鱼机制优化补丁
    /// 包含：
    /// 1. 任务鱼提交全量接入通用随身背包系统（主背包、虚空袋、猪猪、保险箱、熔炉、大背包与实体容器）；
    /// 2. 提交任务鱼立即刷新下一个任务（仅在成功交付后才刷新，杜绝未交付时误切任务）；
    /// 3. 任务鱼解除唯一限制支持 9999 堆叠与重复钓取（世界中无渔夫/当天已交付/背包持有均可钓取）；
    /// 4. 水池无渔力大小惩罚（强制按 300 格满水域计算）。
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch]
    internal class AnglerQuestOptimization
    {
        public static GetSetReset<bool> EnableNoAnglerCooldown = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableQuestFishStack = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableNoFishingPenalty = new GetSetReset<bool>(true, true);

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
                UIBuild.get2(EnableQuestFishStack, "任务鱼解除唯一限制并可堆叠至 9999，渔夫不在/任务已交付/背包持有均可重复钓取", "Images/Item_2450", "任务鱼堆叠与无唯一限制"),
                UIBuild.get2(EnableNoFishingPenalty, "消除水池过小对渔力的惩罚，任何水体均强制按满 300 格计算渔力", "Images/Item_2294", "水池无渔力惩罚")
            };
        }

        #region 1. 通用随身背包检索与提交无冷却

        /// <summary>
        /// 接管渔夫任务对话结算：支持全随身背包检索、无冷却刷新与精准结算
        /// </summary>
        [HarmonyPatch(typeof(Main), nameof(Main.NPCChatText_DoAnglerQuest))]
        [HarmonyPrefix]
        public static bool NPCChatText_DoAnglerQuestPrefix()
        {
            Player player = Main.LocalPlayer;
            if (player == null || !player.active)
                return true;

            Main.npcChatCornerItem = 0;
            SoundEngine.PlaySound(12);

            bool rewarded = false;
            int questFishId = (Main.anglerQuest >= 0 && Main.anglerQuest < Main.anglerQuestItemNetIDs.Length)
                ? Main.anglerQuestItemNetIDs[Main.anglerQuest]
                : -1;

            bool canTurnIn = EnableNoAnglerCooldown.val ||
                (!Main.anglerQuestFinished && !Main.anglerWhoFinishedToday.Contains(player.name));

            if (canTurnIn && questFishId > 0)
            {
                // 通用随身背包系统检索并扣除 1 条任务鱼
                if (TryConsumeQuestFish(player, questFishId))
                {
                    rewarded = true;
                    SoundEngine.PlaySound(24);
                    player.anglerQuestsFinished++;
                    player.GetAnglerReward(Main.npc[player.talkNPC], questFishId);
                }
            }

            Main.npcChatText = Lang.AnglerQuestChat(rewarded);

            if (rewarded)
            {
                Main.anglerQuestFinished = true;
                if (Main.netMode == 1)
                {
                    NetMessage.SendData(75);
                }
                else
                {
                    Main.anglerWhoFinishedToday.Add(player.name);
                }
                AchievementsHelper.HandleAnglerService();

                // 开启无冷却：立即重置状态并刷新下一个任务
                if (EnableNoAnglerCooldown.val)
                {
                    Main.anglerQuestFinished = false;
                    Main.anglerWhoFinishedToday?.Clear();
                    Main.AnglerQuestSwap();
                    Main.NewText("[c/00FFDD:渔夫任务已成功提交并立即刷新！可再次对话接取新的钓鱼任务。]");
                }
            }

            Main.DoNPCPortraitHop();
            return false; // 拦截原版单背包查找逻辑
        }

        /// <summary>
        /// 在玩家全随身容器（主背包、虚空袋、猪猪、保险箱、熔炉、大背包、独立实体容器）中寻找并消耗 1 条任务鱼
        /// </summary>
        public static bool TryConsumeQuestFish(Player player, int questFishId)
        {
            if (player == null || !player.active || questFishId <= 0)
                return false;

            // 1. 主背包 (58 格)
            if (TryConsumeFromItemArray(player.inventory, questFishId))
                return true;

            // 2. 虚空袋 (40 格)
            if (player.bank4?.item != null && TryConsumeFromItemArray(player.bank4.item, questFishId))
                return true;

            // 3. 猪猪储蓄罐 (40 格)
            if (player.bank?.item != null && TryConsumeFromItemArray(player.bank.item, questFishId))
                return true;

            // 4. 保险箱 (40 格)
            if (player.bank2?.item != null && TryConsumeFromItemArray(player.bank2.item, questFishId))
                return true;

            // 5. 护卫熔炉 (40 格)
            if (player.bank3?.item != null && TryConsumeFromItemArray(player.bank3.item, questFishId))
                return true;

            // 6. 巨大额外背包 (BigBag)
            if (BigBagSystem.EnableBigBag.val && BigBagSystem.Slots != null && TryConsumeFromItemArray(BigBagSystem.Slots, questFishId))
            {
                BigBagSystem.NotifySlotsChanged();
                return true;
            }

            return false;
        }

        private static bool TryConsumeFromItemArray(Item[] items, int questFishId)
        {
            if (items == null) return false;

            for (int i = 0; i < items.Length; i++)
            {
                Item item = items[i];
                if (item == null || item.IsAir || item.stack <= 0)
                    continue;

                if (item.type == questFishId)
                {
                    item.stack--;
                    if (item.stack <= 0)
                    {
                        item.TurnToAir();
                    }
                    return true;
                }

                // 递归检查随身独立收纳容器
                if (item.type >= ItemID.Count)
                {
                    var container = ItemLoader.GetModItem(item) as IItemContainer;
                    if (container?.Slots != null)
                    {
                        if (TryConsumeFromItemArray(container.Slots, questFishId))
                            return true;
                    }
                }
            }

            return false;
        }

        #endregion

        #region 2. 任务鱼解除唯一限制与重复钓取

        /// <summary>
        /// 任务鱼生成时解除 uniqueStack 并设最大堆叠为 9999
        /// </summary>
        [HarmonyPatch(typeof(Item), "DefaultToQuestFish")]
        [HarmonyPostfix]
        public static void DefaultToQuestFishPostfix(Item __instance)
        {
            if (EnableQuestFishStack.val)
            {
                __instance.maxStack = 9999;
                __instance.uniqueStack = false;
            }
        }

        /// <summary>
        /// 探测任务鱼：消除背包持有/渔夫不在/任务已交付等拦截限制
        /// </summary>
        [HarmonyPatch(typeof(Projectile), "FishingCheck_ProbeForQuestFish")]
        [HarmonyPostfix]
        public static void FishingCheck_ProbeForQuestFishPostfix(ref FishingAttempt fisher)
        {
            if (!EnableQuestFishStack.val)
                return;

            if (Main.anglerQuest >= 0 && Main.anglerQuest < Main.anglerQuestItemNetIDs.Length)
            {
                fisher.questFish = Main.anglerQuestItemNetIDs[Main.anglerQuest];
            }
        }

        /// <summary>
        /// 任务鱼拾取时临时解除 uniqueStack，使重复任务鱼可以正常进入背包
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.GetItem))]
        [HarmonyPrefix]
        public static void GetItemPrefix(Item newItem, out bool __state)
        {
            __state = false;
            if (EnableQuestFishStack.val && newItem != null && newItem.questItem && newItem.uniqueStack)
            {
                __state = true;
                newItem.uniqueStack = false;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GetItem))]
        [HarmonyPostfix]
        public static void GetItemPostfix(Item newItem, bool __state)
        {
            if (__state)
                newItem.uniqueStack = true;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.ItemSpace), typeof(Item))]
        [HarmonyPrefix]
        public static void ItemSpacePrefix(Item newItem, out bool __state)
        {
            __state = false;
            if (EnableQuestFishStack.val && newItem != null && newItem.questItem && newItem.uniqueStack)
            {
                __state = true;
                newItem.uniqueStack = false;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.ItemSpace), typeof(Item))]
        [HarmonyPostfix]
        public static void ItemSpacePostfix(Item newItem, bool __state)
        {
            if (__state)
                newItem.uniqueStack = true;
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
