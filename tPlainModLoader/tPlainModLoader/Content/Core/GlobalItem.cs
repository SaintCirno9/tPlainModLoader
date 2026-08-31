using System.Collections.Generic;
using Terraria;
using TPML.Content.UI;

namespace TPML.Content
{
    /// <summary>
    /// TPML 全局物品行为修饰基类
    /// </summary>
    public abstract class GlobalItem : ModType
    {
        public virtual void SetDefaults(Item item)
        {
        }

        public virtual bool? CanUseItem(Item item, Player player)
        {
            return null;
        }

        public virtual bool? UseItem(Item item, Player player)
        {
            return null;
        }

        public virtual void HoldItem(Item item, Player player)
        {
        }

        public virtual void UpdateInventory(Item item, Player player)
        {
        }

        public virtual void UpdateEquip(Item item, Player player)
        {
        }

        public virtual void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
        }
    }
}
