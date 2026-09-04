using CommandHelp;
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
    /// 渔夫任务与钓鱼机制优化门控（基于 HookGen 强类型 On_ 门控）
    /// 包含：
    /// 1. 任务鱼提交全量接入通用随身背包系统（主背包、虚空袋、猪猪、保险箱、熔炉、大背包与实体容器）；
    /// 2. 提交任务鱼立即刷新下一个任务（仅在成功交付后才刷新，杜绝未交付时误切任务）；
    /// 3. 任务鱼解除唯一限制支持 9999 堆叠与重复钓取（世界中无渔夫/当天已交付/背包持有均可钓取）；
    /// 4. 水池无渔力大小惩罚（强制按 300 格满水域计算）。
    /// 作者: SaintCirno9
    /// </summary>
    internal class AnglerQuestOptimizationHooks
    {
        // 默认关闭：无冷却 + 任务鱼可堆叠组合默认开启会形成无限交付刷奖励
        public static GetSetReset<bool> EnableNoAnglerCooldown = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> EnableQuestFishStack = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableNoFishingPenalty = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableCatchQuestFishAnywhere = new GetSetReset<bool>(true, true);
        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_Main.NPCChatText_DoAnglerQuest += Hook_NPCChatText_DoAnglerQuest;
            On_Item.DefaultToQuestFish += Hook_DefaultToQuestFish;
            On_Projectile.FishingCheck_ProbeForQuestFish += Hook_FishingCheck_ProbeForQuestFish;
            On_Projectile.FishingCheck_RollItemDrop += Hook_FishingCheck_RollItemDrop;
            On_Player.GetItem += Hook_GetItem;
            On_Player.ItemSpace_Item += Hook_ItemSpace;
            On_Projectile.GetFishingPondState += Hook_GetFishingPondState;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_Main.NPCChatText_DoAnglerQuest -= Hook_NPCChatText_DoAnglerQuest;
            On_Item.DefaultToQuestFish -= Hook_DefaultToQuestFish;
            On_Projectile.FishingCheck_ProbeForQuestFish -= Hook_FishingCheck_ProbeForQuestFish;
            On_Projectile.FishingCheck_RollItemDrop -= Hook_FishingCheck_RollItemDrop;
            On_Player.GetItem -= Hook_GetItem;
            On_Player.ItemSpace_Item -= Hook_ItemSpace;
            On_Projectile.GetFishingPondState -= Hook_GetFishingPondState;
            _registered = false;
        }

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("noAnglerCooldown", EnableNoAnglerCooldown),
                CommandBuild.get2("questFishStack", EnableQuestFishStack),
                CommandBuild.get2("noFishingPenalty", EnableNoFishingPenalty),
                CommandBuild.get2("catchQuestFishAnywhere", EnableCatchQuestFishAnywhere)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(EnableNoAnglerCooldown, "提交渔夫任务后立即刷新下一个任务，无需等待至次日凌晨", "Images/Item_2422", "渔夫任务无冷却"),
                UIBuild.get2(EnableQuestFishStack, "任务鱼解除唯一限制并可堆叠至 9999，渔夫不在/任务已交付/背包持有均可重复钓取", "Images/Item_2450", "任务鱼堆叠与无唯一限制"),
                UIBuild.get2(EnableNoFishingPenalty, "消除水池过小对渔力的惩罚，任何水体均强制按满 300 格计算渔力", "Images/Item_2294", "水池无渔力惩罚"),
                UIBuild.get2(EnableCatchQuestFishAnywhere, "无视生物群落、水体深度与环境限制，在任何水体垂钓均可正常钓取今日渔夫任务鱼", "Images/Item_2451", "全环境钓取任务鱼")
            };
        }

        #region 1. 通用随身背包检索与提交无冷却

        /// <summary>
        /// 接管渔夫任务对话结算：支持全随身背包检索、无冷却刷新与精准结算
        /// </summary>
        private static void Hook_NPCChatText_DoAnglerQuest(On_Main.orig_NPCChatText_DoAnglerQuest orig)
        {
            Player player = Main.LocalPlayer;
            if (player == null || !player.active)
            {
                orig();
                return;
            }

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
        private static void Hook_DefaultToQuestFish(On_Item.orig_DefaultToQuestFish orig, Item self)
        {
            orig(self);

            if (EnableQuestFishStack.val)
            {
                self.maxStack = 9999;
                self.uniqueStack = false;
            }
        }

        /// <summary>
        /// 探测任务鱼：消除背包持有/渔夫不在/任务已交付等拦截限制
        /// </summary>
        private static void Hook_FishingCheck_ProbeForQuestFish(On_Projectile.orig_FishingCheck_ProbeForQuestFish orig, Projectile self, ref FishingAttempt fisher)
        {
            orig(self, ref fisher);

            if (!EnableQuestFishStack.val && !EnableCatchQuestFishAnywhere.val)
                return;

            if (Main.anglerQuest >= 0 && Main.anglerQuest < Main.anglerQuestItemNetIDs.Length)
            {
                fisher.questFish = Main.anglerQuestItemNetIDs[Main.anglerQuest];
            }
        }

        /// <summary>
        /// 全环境钓取任务鱼：在任何水体垂钓命中 Uncommon 时，若无更高级别的敌怪/宝匣/稀有战利品，直接钓出今日渔夫任务鱼
        /// </summary>
        private static void Hook_FishingCheck_RollItemDrop(On_Projectile.orig_FishingCheck_RollItemDrop orig, Projectile self, ref FishingAttempt fisher)
        {
            orig(self, ref fisher);

            if (!EnableCatchQuestFishAnywhere.val)
                return;

            // 如果已有敌怪生成，不覆盖
            if (fisher.rolledEnemySpawn > 0)
                return;

            // 如果已有宝匣产出，不覆盖
            if (fisher.crate && fisher.rolledItemDrop > 0 &&
                (ItemID.Sets.IsFishingCrate[fisher.rolledItemDrop] || ItemID.Sets.IsFishingCrateHardmode[fisher.rolledItemDrop]))
                return;

            // 如果 roll 出了更高级别的传奇或非常稀有道具（如和风鱼、青蛙腿、高级武器等），不覆盖
            if (fisher.legendary || fisher.veryrare)
                return;

            int questFishId = fisher.questFish;
            if (questFishId <= 0 && Main.anglerQuest >= 0 && Main.anglerQuest < Main.anglerQuestItemNetIDs.Length)
            {
                questFishId = Main.anglerQuestItemNetIDs[Main.anglerQuest];
            }

            if (fisher.uncommon && questFishId > 0)
            {
                fisher.rolledItemDrop = questFishId;
            }
        }

        /// <summary>
        /// 任务鱼拾取时临时解除 uniqueStack，使重复任务鱼可以正常进入背包
        /// </summary>
        private static Item Hook_GetItem(On_Player.orig_GetItem orig, Player self, Item newItem, GetItemSettings settings)
        {
            bool modified = false;
            if (EnableQuestFishStack.val && newItem != null && newItem.questItem && newItem.uniqueStack)
            {
                modified = true;
                newItem.uniqueStack = false;
            }

            try
            {
                return orig(self, newItem, settings);
            }
            finally
            {
                if (modified && newItem != null)
                {
                    newItem.uniqueStack = true;
                }
            }
        }

        private static Player.ItemSpaceStatus Hook_ItemSpace(On_Player.orig_ItemSpace_Item orig, Player self, Item newItem)
        {
            bool modified = false;
            if (EnableQuestFishStack.val && newItem != null && newItem.questItem && newItem.uniqueStack)
            {
                modified = true;
                newItem.uniqueStack = false;
            }

            try
            {
                return orig(self, newItem);
            }
            finally
            {
                if (modified && newItem != null)
                {
                    newItem.uniqueStack = true;
                }
            }
        }

        #endregion

        #region 3. 水池无渔力大小惩罚

        /// <summary>
        /// 强制水域格数至少为 300 格，完全消除湖泊大小渔力惩罚
        /// </summary>
        private static void Hook_GetFishingPondState(On_Projectile.orig_GetFishingPondState orig, int x, int y, out bool lava, out bool honey, out int numWaters, out int chumCount)
        {
            orig(x, y, out lava, out honey, out numWaters, out chumCount);

            if (EnableNoFishingPenalty.val && numWaters > 0)
            {
                numWaters = Math.Max(numWaters, 300);
            }
        }

        #endregion
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    internal class AnglerQuestOptimization : AnglerQuestOptimizationHooks
    {
    }
}
