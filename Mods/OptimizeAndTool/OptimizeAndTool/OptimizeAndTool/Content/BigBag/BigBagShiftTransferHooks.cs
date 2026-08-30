using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.BigBag
{
    /// <summary>
    /// 大背包 Shift 快捷转移补丁（基于 HookGen 强类型 On_ 门控）：
    /// 当大背包打开时，玩家在个人物品栏按住 Shift 点击物品即可一键存入大背包（光标高亮为转移到箱子图标）；
    /// 作者: SaintCirno9
    /// </summary>
    internal static class BigBagShiftTransferHooks
    {
        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_ItemSlot.GetAlternateClickAction += Hook_GetAlternateClickAction;
            On_ItemSlot.OverrideLeftClick += Hook_OverrideLeftClick;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_ItemSlot.GetAlternateClickAction -= Hook_GetAlternateClickAction;
            On_ItemSlot.OverrideLeftClick -= Hook_OverrideLeftClick;
            _registered = false;
        }

        private static ItemSlot.AlternateClickAction? Hook_GetAlternateClickAction(On_ItemSlot.orig_GetAlternateClickAction orig, Item[] inv, int context, int slot)
        {
            ItemSlot.AlternateClickAction? result = orig(inv, context, slot);

            if (!ModifyInterfaceLayers.BigBagIsOpen || !BigBag.EnableBigBag.val) return result;
            if (Main.player[Main.myPlayer].chest != -1) return result;

            if (context != ItemSlot.Context.InventoryItem && context != ItemSlot.Context.InventoryCoin && context != ItemSlot.Context.InventoryAmmo) return result;

            if (inv == null || slot < 0 || slot >= inv.Length) return result;
            Item item = inv[slot];
            if (item == null || item.IsAir || item.favorited) return result;

            if (ItemSlot.ShiftInUse)
            {
                if (BigBag.TryPlacingInBigBag(inv, slot, justCheck: true))
                {
                    return ItemSlot.AlternateClickAction.TransferToChest;
                }
            }

            return result;
        }

        private static bool Hook_OverrideLeftClick(On_ItemSlot.orig_OverrideLeftClick orig, Item[] inv, int context, int slot)
        {
            if (!ModifyInterfaceLayers.BigBagIsOpen || !BigBag.EnableBigBag.val) return orig(inv, context, slot);
            if (Main.player[Main.myPlayer].chest != -1) return orig(inv, context, slot);

            if (context != ItemSlot.Context.InventoryItem && context != ItemSlot.Context.InventoryCoin && context != ItemSlot.Context.InventoryAmmo) return orig(inv, context, slot);

            if (Main.cursorOverride == 9)
            {
                if (BigBag.TryPlacingInBigBag(inv, slot, justCheck: false))
                {
                    SoundEngine.PlaySound(SoundID.Grab);
                    CoinSlot.ForceSlotState(slot, context, inv[slot]);
                    return true;
                }
            }

            return orig(inv, context, slot);
        }
    }
}
