using HarmonyLib;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;

namespace AccessoryBox.Common
{
    /// <summary>
    /// 饰品箱 Shift 快捷转移补丁：
    /// 当饰品箱打开时，玩家在个人物品栏按住 Shift 点击物品即可一键存入饰品箱（光标高亮为转移到箱子图标）；
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch]
    internal static class Patch_BoxShiftTransfer
    {
        [HarmonyPatch(typeof(ItemSlot), nameof(ItemSlot.GetAlternateClickAction))]
        [HarmonyPostfix]
        public static void GetAlternateClickActionPostfix(Item[] inv, int context, int slot, ref ItemSlot.AlternateClickAction? __result)
        {
            if (!ModifyInterfaceLayers.BoxIsOpen || !AccessoryBox.EnableMod) return;
            if (Main.player[Main.myPlayer].chest != -1) return;

            if (context != ItemSlot.Context.InventoryItem && context != ItemSlot.Context.InventoryCoin && context != ItemSlot.Context.InventoryAmmo) return;

            if (inv == null || slot < 0 || slot >= inv.Length) return;
            Item item = inv[slot];
            if (item == null || item.IsAir || item.favorited) return;

            if (ItemSlot.ShiftInUse)
            {
                if (AccessoryBox.TryPlacingInAccessoryBox(inv, slot, justCheck: true))
                {
                    __result = ItemSlot.AlternateClickAction.TransferToChest;
                }
            }
        }

        [HarmonyPatch(typeof(ItemSlot), nameof(ItemSlot.OverrideLeftClick))]
        [HarmonyPrefix]
        public static bool OverrideLeftClickPrefix(Item[] inv, int context, int slot, ref bool __result)
        {
            if (!ModifyInterfaceLayers.BoxIsOpen || !AccessoryBox.EnableMod) return true;
            if (Main.player[Main.myPlayer].chest != -1) return true;

            if (context != ItemSlot.Context.InventoryItem && context != ItemSlot.Context.InventoryCoin && context != ItemSlot.Context.InventoryAmmo) return true;

            if (Main.cursorOverride == 9)
            {
                if (AccessoryBox.TryPlacingInAccessoryBox(inv, slot, justCheck: false))
                {
                    SoundEngine.PlaySound(SoundID.Grab);
                    CoinSlot.ForceSlotState(slot, context, inv[slot]);
                    __result = true;
                    return false;
                }
            }

            return true;
        }
    }
}
