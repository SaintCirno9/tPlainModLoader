using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Chat;

namespace RecipeBrowser.UIElements
{
    public class UIItemSlot : UIElement
    {
        public static Texture2D DefaultBackgroundTexture => TextureAssets.InventoryBack9?.Value ?? TextureAssets.InventoryBack.Value;
        public Texture2D BackgroundTexture;

        internal float scale = 0.75f;
        public int itemType;
        public Item item;
        public bool hideSlot;
        internal static Item hoveredItem;
        internal int frameCounter;
        internal int frameTimer;
        private const int frameDelay = 7;

        public UIItemSlot(Item item, float scale = 0.75f)
        {
            this.scale = scale;
            this.item = item ?? new Item();
            itemType = this.item.type;
            BackgroundTexture = DefaultBackgroundTexture;

            float w = (BackgroundTexture != null ? BackgroundTexture.Width : 52) * scale;
            float h = (BackgroundTexture != null ? BackgroundTexture.Height : 52) * scale;
            Width.Set(w, 0f);
            Height.Set(h, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (item == null) return;
            CalculatedStyle innerDimensions = GetInnerDimensions();
            Vector2 pos = innerDimensions.Position();
            var bg = BackgroundTexture ?? DefaultBackgroundTexture;

            if (!hideSlot && bg != null)
            {
                spriteBatch.Draw(bg, pos, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                DrawAdditionalOverlays(spriteBatch, pos, scale);
            }

            if (item.IsAir || item.type <= 0) return;

            Utilities.LoadItem(item.type);
            Texture2D tex = (item.type < TextureAssets.Item.Length) ? TextureAssets.Item[item.type]?.Value : null;
            if (tex == null) return;

            DrawAnimation anim = (item.type < Main.itemAnimations.Length) ? Main.itemAnimations[item.type] : null;
            Rectangle frame = anim != null ? anim.GetFrame(tex, -1) : tex.Bounds;

            Color lightColor = Color.White;
            float lightScale = 1f;
            ItemSlot.GetItemLight(ref lightColor, ref lightScale, item, false);

            int width = frame.Width;
            int height = frame.Height;
            float drawScale = 1f;
            float bgWidth = (bg != null ? bg.Width : 52) * scale;

            if (width > bgWidth || height > bgWidth)
            {
                drawScale = (width <= height) ? (bgWidth / height) : (bgWidth / width);
            }
            drawScale *= scale;

            Vector2 slotSize = (bg != null ? new Vector2(bg.Width, bg.Height) : new Vector2(52, 52)) * scale;
            Vector2 drawPos = pos + slotSize / 2f;
            Vector2 origin = new Vector2(frame.Width / 2f, frame.Height / 2f);

            spriteBatch.Draw(tex, drawPos, frame, item.GetAlpha(lightColor), 0f, origin, drawScale * lightScale, SpriteEffects.None, 0f);
            if (item.color != Color.Transparent)
            {
                spriteBatch.Draw(tex, drawPos, frame, item.GetColor(Color.White), 0f, origin, drawScale * lightScale, SpriteEffects.None, 0f);
            }

            if (item.type < ItemID.Sets.TrapSigned.Length && ItemID.Sets.TrapSigned[item.type])
            {
                if (TextureAssets.Wire != null && TextureAssets.Wire.IsLoaded && TextureAssets.Wire.Value != null)
                {
                    spriteBatch.Draw(TextureAssets.Wire.Value, pos + new Vector2(40f, 40f) * scale, new Rectangle(4, 58, 8, 8), Color.White, 0f, new Vector2(4f), 1f, SpriteEffects.None, 0f);
                }
            }

            DrawAdditionalBadges(spriteBatch, pos, scale);

            if (item.stack > 1)
            {
                ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, item.stack.ToString(), pos + new Vector2(10f, 26f) * scale, Color.White, 0f, Vector2.Zero, new Vector2(scale), -1f, scale);
            }

            if (IsMouseHovering)
            {
                Main.HoverItem = item.Clone();
                Main.hoverItemName = Main.HoverItem.Name;
                hoveredItem = Main.HoverItem;
            }
        }

        internal virtual void DrawAdditionalOverlays(SpriteBatch spriteBatch, Vector2 position, float scale)
        {
        }

        internal virtual void DrawAdditionalBadges(SpriteBatch spriteBatch, Vector2 position, float scale)
        {
        }

        public static void DrawItem(SpriteBatch spriteBatch, Item item, Vector2 pos, float scale)
        {
            if (item == null || item.IsAir || item.type <= 0) return;
            Utilities.LoadItem(item.type);
            Texture2D tex = (item.type < TextureAssets.Item.Length) ? TextureAssets.Item[item.type]?.Value : null;
            if (tex == null) return;

            DrawAnimation anim = (item.type < Main.itemAnimations.Length) ? Main.itemAnimations[item.type] : null;
            Rectangle frame = anim != null ? anim.GetFrame(tex, -1) : tex.Bounds;

            Color lightColor = Color.White;
            float lightScale = 1f;
            ItemSlot.GetItemLight(ref lightColor, ref lightScale, item, false);

            int width = frame.Width;
            int height = frame.Height;
            float drawScale = 1f;
            float bgWidth = 52f * scale;

            if (width > bgWidth || height > bgWidth)
            {
                drawScale = (width <= height) ? (bgWidth / height) : (bgWidth / width);
            }
            drawScale *= scale;

            Vector2 slotSize = new Vector2(52f, 52f) * scale;
            Vector2 drawPos = pos + slotSize / 2f;
            Vector2 origin = new Vector2(frame.Width / 2f, frame.Height / 2f);

            spriteBatch.Draw(tex, drawPos, frame, item.GetAlpha(lightColor), 0f, origin, drawScale * lightScale, SpriteEffects.None, 0f);
            if (item.color != Color.Transparent)
            {
                spriteBatch.Draw(tex, drawPos, frame, item.GetColor(Color.White), 0f, origin, drawScale * lightScale, SpriteEffects.None, 0f);
            }
        }
    }
}
