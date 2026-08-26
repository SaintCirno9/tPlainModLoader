using CommandHelp;
using HarmonyLib;
using Microsoft.Xna.Framework;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL.Fishing
{
    /// <summary>
    /// 渔获处理系统
    /// 包含：自动开宝匣、自动开牡蛎、垃圾渔获自动售卖、全部渔获自动变现、钓起敌怪自动处决与战利品统计
    /// 作者: SaintCirno9
    /// </summary>
    internal class FishingCatchProcessor
    {
        public static GetSetReset<bool> EnableAutoOpenCrates = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> EnableAutoOpenOysters = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableAutoSellJunk = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> EnableAutoSellAllCatches = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> EnableAutoKillFishingNPC = new GetSetReset<bool>(false, false);

        // 统计数据
        public static int TotalCatchesCount { get; private set; } = 0;
        public static long TotalCoinsEarned { get; private set; } = 0;
        public static int LastCatchItemType { get; private set; } = 0;
        public static int LastCatchStack { get; private set; } = 0;

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("autoOpenCrates", EnableAutoOpenCrates),
                CommandBuild.get2("autoOpenOysters", EnableAutoOpenOysters),
                CommandBuild.get2("autoSellJunkCatches", EnableAutoSellJunk),
                CommandBuild.get2("autoSellAllCatches", EnableAutoSellAllCatches),
                CommandBuild.get2("autoKillFishingNPC", EnableAutoKillFishingNPC)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(EnableAutoOpenCrates, "钓起宝匣时自动开箱并掉落内部所有物资（免去手动开启与背包占满困扰）", "Images/Item_2334", "自动开启宝匣"),
                UIBuild.get2(EnableAutoOpenOysters, "钓起牡蛎时自动开壳，直接获得白/黑/粉珍珠或牡蛎肉", "Images/Item_4403", "自动开启牡蛎"),
                UIBuild.get2(EnableAutoSellJunk, "钓起破旧鞋子、海藻、锡罐等垃圾时直接将其折算为钱币", "Images/Item_2337", "垃圾渔获自动变现"),
                UIBuild.get2(EnableAutoSellAllCatches, "钓起任何鱼类与物品时全部直接折算为钱币（挂机极速刷钱专用）", "Images/Item_73", "全部渔获自动变现"),
                UIBuild.get2(EnableAutoKillFishingNPC, "钓起血月敌怪或熔岩怪物时瞬间将其处决并掉落战利品与统计击杀", "Images/Item_4342", "自动处决钓起敌怪")
            };
        }

        public static void ResetStats()
        {
            TotalCatchesCount = 0;
            TotalCoinsEarned = 0;
            LastCatchItemType = 0;
            LastCatchStack = 0;
        }

        /// <summary>
        /// 处理钓起的物品渔获，并将成堆武器/弹药按原版规则结算
        /// </summary>
        public static void ProcessCatch(Projectile bobber, Player player, int itemType, int stack, FishingAttempt fisher)
        {
            if (itemType <= 0)
                return;

            Item item = new Item();
            item.SetDefaults(itemType);
            item.stack = stack > 1 ? stack : 1;

            // 原版 AI_061_FishingBobber_GiveItemToPlayer 对成堆投掷物有渔力决定的数量
            int finalFishingLevel = fisher.playerFishingConditions.FinalFishingLevel;
            if (itemType == ItemID.BombFish)
            {
                int minValue = (finalFishingLevel / 20 + 3) / 2;
                int maxValue = (finalFishingLevel / 10 + 6) / 2;
                if (Main.rand.Next(50) < finalFishingLevel) maxValue++;
                if (Main.rand.Next(100) < finalFishingLevel) maxValue++;
                if (Main.rand.Next(150) < finalFishingLevel) maxValue++;
                if (Main.rand.Next(200) < finalFishingLevel) maxValue++;
                item.stack = Main.rand.Next(minValue, maxValue + 1);
            }
            else if (itemType == ItemID.FrostDaggerfish)
            {
                int minValue = (finalFishingLevel / 4 + 15) / 2;
                int maxValue = (finalFishingLevel / 2 + 40) / 2;
                if (Main.rand.Next(50) < finalFishingLevel) maxValue += 6;
                if (Main.rand.Next(100) < finalFishingLevel) maxValue += 6;
                if (Main.rand.Next(150) < finalFishingLevel) maxValue += 6;
                if (Main.rand.Next(200) < finalFishingLevel) maxValue += 6;
                item.stack = Main.rand.Next(minValue, maxValue + 1);
            }

            AutoFishingSupplies.TryConsumeBait(player, fisher.playerFishingConditions.BaitItemType);
            bobber.ReduceRemainingChumsInPool();

            TotalCatchesCount += item.stack;
            LastCatchItemType = itemType;
            LastCatchStack = item.stack;

            // 全部售卖变现
            if (EnableAutoSellAllCatches.val)
            {
                SellItem(player, item);
                return;
            }

            // 垃圾渔获自动变现
            if (EnableAutoSellJunk.val && IsJunkCatch(itemType))
            {
                SellItem(player, item);
                return;
            }

            // 自动开宝匣
            if (EnableAutoOpenCrates.val &&
                (ItemID.Sets.IsFishingCrate[itemType] || ItemID.Sets.IsFishingCrateHardmode[itemType]))
            {
                for (int i = 0; i < item.stack; i++)
                {
                    player.OpenFishingCrate(itemType);
                }
                return;
            }

            // 自动开牡蛎
            if (EnableAutoOpenOysters.val && itemType == ItemID.Oyster)
            {
                for (int i = 0; i < item.stack; i++)
                {
                    player.OpenOyster(itemType);
                }
                return;
            }

            GiveOrDropItem(bobber, player, item);
        }

        /// <summary>
        /// 处理钓起的怪物/敌怪
        /// </summary>
        public static void ProcessEnemyCatch(Projectile bobber, Player player, int npcType, FishingAttempt fisher)
        {
            if (npcType <= 0)
                return;

            AutoFishingSupplies.TryConsumeBait(player, fisher.playerFishingConditions.BaitItemType);
            bobber.ReduceRemainingChumsInPool();

            TotalCatchesCount++;
            LastCatchItemType = -npcType;
            LastCatchStack = 1;

            Point point = new Point((int)bobber.position.X, (int)bobber.position.Y);
            if (npcType == NPCID.BloodNautilus)
                point.Y += 64;

            if (Main.netMode == 1)
            {
                // 与原版收竿钓怪一致，由服务器生成并同步
                NetMessage.SendData(130, -1, -1, null, point.X / 16, point.Y / 16, npcType);
            }
            else
            {
                if (npcType == NPCID.TownSlimeRed)
                    NPC.unlockedSlimeRedSpawn = true;

                IEntitySource source = new EntitySource_FishedOut(bobber);
                int npcIndex = NPC.NewNPC(source, point.X, point.Y, npcType);
                if (npcIndex >= 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex].active)
                {
                    NPC npc = Main.npc[npcIndex];
                    if (EnableAutoKillFishingNPC.val)
                    {
                        npc.playerInteraction[player.whoAmI] = true;
                        int banner = Terraria.GameContent.BannerSystem.NPCtoBanner(npc.BannerID());
                        if (banner > 0)
                        {
                            player.lastCreatureHit = banner;
                        }
                        npc.StrikeNPCNoInteraction(99999, 0f, 0);
                    }
                }

                if (npcType == NPCID.TownSlimeRed)
                    WorldGen.CheckAchievement_RealEstateAndTownSlimes();
            }
        }

        /// <summary>
        /// 优先放入背包，背包放不下时从浮标位置掉落
        /// </summary>
        private static void GiveOrDropItem(Projectile bobber, Player player, Item item)
        {
            item.newAndShiny = true;
            Item overflow = player.GetItem(item, GetItemSettings.PickupItemFromWorld);
            if (overflow != null && overflow.stack > 0)
            {
                player.QuickSpawnItem(new EntitySource_FishedOut(bobber), overflow.type, overflow.stack);
            }
        }

        private static void SellItem(Player player, Item item)
        {
            long value = (long)item.value * item.stack / 5;
            if (value <= 0)
                value = 1;

            TotalCoinsEarned += value;

            long[] coins = new long[4];
            long coinValue = Item.platinum;
            long sum = 0;
            for (int i = 3; i >= 0; i--)
            {
                coins[i] = (value - sum) / coinValue;
                sum += coins[i] * coinValue;
                coinValue /= 100;
            }

            IEntitySource source = player.GetItemSource_OpenItem(item.type);
            for (int i = 0; i < 4; i++)
            {
                if (coins[i] > 0)
                {
                    player.QuickSpawnItem(source, ItemID.CopperCoin + i, (int)coins[i]);
                }
            }
        }

        /// <summary>
        /// 判定是否为垃圾渔获（鞋子、海藻、锡罐、乔乔可乐等）
        /// </summary>
        public static bool IsJunkCatch(int type)
        {
            return type == ItemID.OldShoe ||
                   type == ItemID.TinCan ||
                   type == ItemID.FishingSeaweed ||
                   type == ItemID.Seaweed ||
                   type == ItemID.JojaCola;
        }
    }
}