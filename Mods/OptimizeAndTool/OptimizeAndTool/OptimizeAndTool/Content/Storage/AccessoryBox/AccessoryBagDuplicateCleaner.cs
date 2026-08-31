using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using TPML.Content;

namespace OptimizeAndTool.Content.Storage.AccessoryBox
{
    /// <summary>
    /// 饰品袋相同 GUID 重复清理安全系统
    /// 作者: SaintCirno9
    /// </summary>
    public static class AccessoryBagDuplicateCleaner
    {
        public static void CheckAndCleanDuplicates(Player player)
        {
            if (Main.gameMenu || player == null || !player.active || player.whoAmI != Main.myPlayer) return;
            if (Main.GameUpdateCount % 30 != 0) return;

            HashSet<Guid> seen = new HashSet<Guid>();
            ScanAndCleanArray(player, player.inventory, seen);
            ScanAndCleanArray(player, player.bank?.item, seen);
            ScanAndCleanArray(player, player.bank2?.item, seen);
            ScanAndCleanArray(player, player.bank3?.item, seen);
            ScanAndCleanArray(player, player.bank4?.item, seen);
        }

        private static void ScanAndCleanArray(Player player, Item[] items, HashSet<Guid> seen)
        {
            if (player == null || items == null) return;
            for (int i = 0; i < items.Length; i++)
            {
                Item it = items[i];
                if (it != null && !it.IsAir)
                {
                    AccessoryBagItem bag = ItemLoader.GetModItem(it) as AccessoryBagItem;
                    if (bag != null && bag.BagID != Guid.Empty)
                    {
                        if (seen.Contains(bag.BagID))
                        {
                            if (bag.personalInventory != null)
                            {
                                for (int s = 0; s < bag.personalInventory.Length; s++)
                                {
                                    Item inner = bag.personalInventory[s];
                                    if (inner != null && !inner.IsAir)
                                    {
                                        player.QuickSpawnItem(new TPML.Content.EntitySource_Misc("AccessoryBagDuplicate"), inner.type, inner.stack);
                                        inner.TurnToAir();
                                    }
                                }
                            }
                            it.TurnToAir();
                            Main.NewText($"[AccessoryBag] 检测到重复饰品袋 {bag.ShortID}，袋内物品已掉落并清理空袋。", Color.OrangeRed);
                        }
                        else
                        {
                            seen.Add(bag.BagID);
                        }
                    }
                }
            }
        }
    }
}
