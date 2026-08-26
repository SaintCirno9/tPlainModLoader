using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;

namespace RecipeBrowser
{
    internal static class LootCacheManager
    {
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
                Console.WriteLine($"[RecipeBrowser] LootCacheManager.Setup Error: {ex}");
            }
        }
    }
}
