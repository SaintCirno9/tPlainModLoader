using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace AccessoryBox.Common.UI
{
    internal class BoxItem : UIPanel
    {
        public Action<Item, Item> OnSetItem = null;
        public Action<Item> OnDel = null;
        public Item item { get; protected set; } = null;

        public BoxItem(Item item)
        {
            this.item = item;

            Width.Set(30, 0);
            Height.Set(30, 0);
            SetPadding(6);
            BackgroundColor = BorderColor = new Color(43, 60, 120);
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            SoundEngine.PlaySound(SoundID.MenuTick);

            OnSetItem?.Invoke(item, Main.LocalPlayer.HeldItem);
        }

        public override void RightClick(UIMouseEvent evt)
        {
            SoundEngine.PlaySound(SoundID.Grab);

            OnDel?.Invoke(item);
        }

        public void SetItem(Item item)
        {
            this.item = item;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            if (item == null) return;

            if (IsMouseHovering && item != null && item.type > ItemID.None)
            {
                ItemSlot.MouseHover(new Item[] { item });
            }

            CalculatedStyle rect = GetInnerDimensions();
            Vector2 center = new Vector2(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);

            ItemSlot.DrawItemIcon(item, ItemSlot.Context.CreativeInfinite, spriteBatch,
                center, 1, rect.Width, Color.White);
        }
    }
}
