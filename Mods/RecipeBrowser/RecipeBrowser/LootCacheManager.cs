using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using TPML.Core.Logging;

namespace RecipeBrowser
{
    internal static class LootCacheManager
    {
        private static readonly ILogger Logger = LogManager.GetLogger("RecipeBrowser");
        /// <summary>
        /// 物品 → 掉率信息缓存（含全局掉落；替代 tML 的 ItemDropDatabase.GetRulesForItemID，供掉落查看器使用）
        /// </summary>
        internal static Dictionary<int, List<DropRateInfo>> itemDrops;

        internal static void EnsureItemDropRates()
        {
            if (itemDrops != null) return;
            itemDrops = new Dictionary<int, List<DropRateInfo>>();
            try
            {
                if (Main.ItemDropsDB == null) return;

                for (int i = -65; i < NPCID.Count; i++)
                {
                    if (i == 0) continue;
                    try
                    {
                        List<IItemDropRule> rules = Main.ItemDropsDB.GetRulesForNPCID(i, true);
                        if (rules == null || rules.Count == 0) continue;

                        List<DropRateInfo> list = new List<DropRateInfo>();
                        DropRateInfoChainFeed feed = new DropRateInfoChainFeed(1f);
                        foreach (IItemDropRule rule in rules)
                        {
                            try { rule?.ReportDroprates(list, feed); } catch { }
                        }
                        foreach (DropRateInfo d in list)
                        {
                            if (d.itemId <= 0) continue;
                            if (!itemDrops.TryGetValue(d.itemId, out var l))
                            {
                                itemDrops[d.itemId] = l = new List<DropRateInfo>();
                            }
                            l.Add(d);
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("LootCacheManager.EnsureItemDropRates 异常", ex);
            }
        }

        internal static void Setup()
        {
            try
            {
                if (LootCache.instance == null)
                {
                    LootCache.instance = new LootCache();
                }
                LootCache.instance.lootInfos.Clear();

                if (Main.ItemDropsDB == null)
                    return;

                DropRateInfoChainFeed feed = new DropRateInfoChainFeed(1f);
                for (int i = -65; i < NPCID.Count; i++)
                {
                    if (i == 0) continue;

                    try
                    {
                        List<IItemDropRule> rulesForNPCID = Main.ItemDropsDB.GetRulesForNPCID(i, false);
                        if (rulesForNPCID == null || rulesForNPCID.Count == 0) continue;

                        List<DropRateInfo> list = new List<DropRateInfo>();
                        feed = new DropRateInfoChainFeed(1f);
                        foreach (IItemDropRule item in rulesForNPCID)
                        {
                            try
                            {
                                item?.ReportDroprates(list, feed);
                            }
                            catch { }
                        }
                        foreach (DropRateInfo item2 in list)
                        {
                            if (item2.itemId <= 0) continue;

                            if (!LootCache.instance.lootInfos.TryGetValue(item2.itemId, out var value))
                            {
                                LootCache.instance.lootInfos.Add(item2.itemId, value = new List<int>());
                            }
                            if (!value.Contains(i))
                            {
                                value.Add(i);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("LootCacheManager.Setup 异常", ex);
            }
        }
    }
}
