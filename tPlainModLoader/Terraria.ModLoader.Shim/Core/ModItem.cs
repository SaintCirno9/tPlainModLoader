using System.Collections.Generic;

namespace Terraria.ModLoader
{
    /// <summary>
    /// tModLoader 自定义物品基类
    /// </summary>
    public abstract class ModItem : ModType
    {
        public Item Item { get; internal set; }
        public int Type => Item?.type ?? 0;

        public virtual void SetDefaults()
        {
        }

        public virtual bool? CanUseItem(Player player)
        {
            return null;
        }

        public virtual bool? UseItem(Player player)
        {
            return null;
        }

        public virtual void HoldItem(Player player)
        {
        }

        public virtual void UpdateInventory(Player player)
        {
        }

        public virtual void UpdateEquip(Player player)
        {
        }

        public virtual void ModifyTooltips(List<TooltipLine> tooltips)
        {
        }

        public virtual void AddRecipes()
        {
        }
    }
}
