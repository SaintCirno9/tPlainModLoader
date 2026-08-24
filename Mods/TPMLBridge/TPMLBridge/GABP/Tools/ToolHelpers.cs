using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TPMLBridge.GABP.Tools
{
    /// <summary>
    /// GABP 工具辅助函数（物品名称解析、槽位查找等）
    /// 作者: SaintCirno9
    /// </summary>
    public static class ToolHelpers
    {
        public static string GetItemDisplayName(int itemId)
        {
            if (itemId <= ItemID.None) return string.Empty;
            string name = Lang.GetItemNameValue(itemId);
            if (!string.IsNullOrEmpty(name)) return name;

            if (itemId >= ItemID.Count)
            {
                ModItem modItem = ItemLoader.GetItem(itemId);
                if (modItem != null) return modItem.DisplayName;
            }
            return $"Item_{itemId}";
        }

        public static int FindInventorySlot(int itemId)
        {
            if (Main.LocalPlayer?.inventory == null)
                return -1;

            for (int i = 0; i < Main.LocalPlayer.inventory.Length; i++)
            {
                if (Main.LocalPlayer.inventory[i]?.type == itemId && !Main.LocalPlayer.inventory[i].IsAir)
                    return i;
            }
            return -1;
        }
    }
}
