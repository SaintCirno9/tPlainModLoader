using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;

namespace Terraria.ModLoader
{
    /// <summary>
    /// tModLoader 自定义物品基类
    /// </summary>
    public abstract class ModItem : ModType
    {
        public Item Item { get; internal set; }
        public int Type => Item?.type ?? 0;
        public virtual string Texture => (GetType().Namespace + "." + Name).Replace('.', '/');

        public string DisplayName => ItemLoader.GetDisplayName(Type);
        public string Tooltip => ItemLoader.GetTooltip(Type);

        public override void Load(Mod mod)
        {
            Mod = mod;
            ItemLoader.Register(this);
            base.Load(mod);
        }

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

        public virtual bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            return true;
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

        public ModRecipe CreateRecipe(int amount = 1)
        {
            var recipe = new ModRecipe(Mod);
            recipe.Create(Type, amount);
            return recipe;
        }
    }
}
