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

        public virtual bool CanUseItem(Item item, Player player)
        {
            return true;
        }

        public virtual bool? UseItem(Item item, Player player)
        {
            return null;
        }

        public virtual void PickAmmo(Item weapon, Item ammo, Player player, ref int type, ref float speed, ref StatModifier damage, ref float knockback)
        {
        }

        public virtual void HoldItem(Item item, Player player)
        {
        }

        public virtual void UpdateInventory(Item item, Player player)
        {
        }

        public virtual bool InstancePerEntity => false;

        public virtual GlobalItem Clone(Item item, Item itemClone) => this;

        public virtual void UpdateEquip(Item item, Player player)
        {
        }

        public virtual void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
        }

        public virtual void PostUpdate(Item item)
        {
        }

        public virtual bool CanBeConsumedAsAmmo(Item ammo, Item weapon, Player player) => true;

        public virtual bool? CanConsumeBait(Player player, Item bait) => null;

        public virtual bool ConsumeItem(Item item, Player player) => true;

        public virtual bool OnPickup(Item item, Player player) => true;

        public virtual bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player) => true;

        public virtual void VerticalWingSpeeds(Item item, Player player, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
        }

        public virtual void HorizontalWingSpeeds(Item item, Player player, ref float speed, ref float acceleration)
        {
        }

        public virtual void GrabRange(Item item, Player player, ref int grabRange)
        {
        }

        public virtual bool GrabStyle(Item item, Player player) => false;

        public virtual bool PreDrawInInventory(Item item, Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Microsoft.Xna.Framework.Vector2 position, Microsoft.Xna.Framework.Rectangle frame, Microsoft.Xna.Framework.Color drawColor, Microsoft.Xna.Framework.Color itemColor, Microsoft.Xna.Framework.Vector2 origin, float scale) => true;

        public virtual void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
        }

        public virtual void ModifyItemLoot(Item item, ItemLoot itemLoot)
        {
        }
    }
}
