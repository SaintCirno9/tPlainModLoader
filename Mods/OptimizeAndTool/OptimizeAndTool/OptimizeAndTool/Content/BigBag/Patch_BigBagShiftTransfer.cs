using HarmonyLib;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.BigBag
{
    /// <summary>
    /// 大背包 Shift 快捷转移补丁：
    /// 当大背包打开时，玩家在个人物品栏按住 Shift 点击物品即可一键存入大背包（光标高亮为转移到箱子图标）；
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch]
    internal static class Patch_BigBagShiftTransfer
    {
        [HarmonyPatch(typeof(ItemSlot), nameof(ItemSlot.GetAlternateClickAction))]
        [HarmonyPostfix]
        public static void GetAlternateClickActionPostfix(Item[] inv, int context, int slot, ref ItemSlot.AlternateClickAction? __result)
        {
            if (!ModifyInterfaceLayers.BigBagIsOpen || !BigBag.EnableBigBag.val) return;
            if (Main.player[Main.myPlayer].chest != -1) return;

            if (context != ItemSlot.Context.InventoryItem && context != ItemSlot.Context.InventoryCoin && context != ItemSlot.Context.InventoryAmmo) return;

            if (inv == null || slot < 0 || slot >= inv.Length) return;
            Item item = inv[slot];
            if (item == null || item.IsAir || item.favorited) return;

            if (ItemSlot.ShiftInUse)
            {
                if (BigBag.TryPlacingInBigBag(inv, slot, justCheck: true))
                {
                    __result = ItemSlot.AlternateClickAction.TransferToChest;
                }
            }
        }

        [HarmonyPatch(typeof(ItemSlot), nameof(ItemSlot.OverrideLeftClick))]
        [HarmonyPrefix]
        public static bool OverrideLeftClickPrefix(Item[] inv, int context, int slot, ref bool __result)
        {
            if (!ModifyInterfaceLayers.BigBagIsOpen || !BigBag.EnableBigBag.val) return true;
            if (Main.player[Main.myPlayer].chest != -1) return true;

            if (context != ItemSlot.Context.InventoryItem && context != ItemSlot.Context.InventoryCoin && context != ItemSlot.Context.InventoryAmmo) return true;

            if (Main.cursorOverride == 9)
            {
                if (BigBag.TryPlacingInBigBag(inv, slot, justCheck: false))
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
