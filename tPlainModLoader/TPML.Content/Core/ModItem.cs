using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using TPML.Content.UI;

namespace TPML.Content
{
    /// <summary>
    /// TPML 自定义物品基类
    /// </summary>
    public abstract class ModItem : ModType
    {
        public Item Item { get; internal set; }
        private int _type;
        public int Type => Item != null && Item.type > 0 ? Item.type : _type;
        internal void SetType(int type) => _type = type;

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

        public virtual void SetStaticDefaults()
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

        public virtual void SaveData(TPML.Content.IO.TagCompound tag)
        {
        }

        public virtual void LoadData(TPML.Content.IO.TagCompound tag)
        {
        }

        public virtual ModItem Clone(Item newEntity)
        {
            ModItem clone = (ModItem)System.Activator.CreateInstance(GetType());
            clone.Mod = Mod;
            clone.Item = newEntity;
            clone.SetType(Type);
            return clone;
        }

        public ModRecipe CreateRecipe(int amount = 1)
        {
            return RecipeLoader.CreateRecipe(this, amount);
        }
    }
}
