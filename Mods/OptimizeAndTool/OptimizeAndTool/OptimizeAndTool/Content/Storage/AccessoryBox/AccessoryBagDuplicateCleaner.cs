using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
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
            ScanAndCleanArray(player.inventory, seen);
            ScanAndCleanArray(player.bank?.item, seen);
            ScanAndCleanArray(player.bank2?.item, seen);
            ScanAndCleanArray(player.bank3?.item, seen);
            ScanAndCleanArray(player.bank4?.item, seen);
        }

        private static void ScanAndCleanArray(Item[] items, HashSet<Guid> seen)
        {
            if (items == null) return;
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
                            it.TurnToAir();
                            Main.NewText($"[AccessoryBag] 检测到重复饰品袋 {bag.ShortID}，已自动安全清理。", Color.OrangeRed);
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
