using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace RecipeBrowser.TagHandlers
{
    public class ItemHoverFixTagHandler : ITagHandler
    {
        private class ItemHoverFixSnippet : TextSnippet
        {
            private Item item;
            private bool check;

            public ItemHoverFixSnippet(Item item, bool check)
                : base("")
            {
                this.item = item;
                this.check = check;
                CheckForHover = true;
            }

            public override void OnHover()
            {
                Main.HoverItem = item.Clone();
                Main.hoverItemName = item.HoverName;
            }

            public override bool UniqueDraw(bool justCheckingSize, out Vector2 size, SpriteBatch spriteBatch, Vector2 position = default(Vector2), Color color = default(Color), float scale = 1f)
            {
                float baseSize = 24f;
                size = new Vector2(baseSize * scale);

                if (!justCheckingSize && spriteBatch != null && item != null)
                {
                    Main.instance.LoadItem(item.type);
                    Texture2D tex = TextureAssets.Item[item.type].Value;
                    Rectangle frame = (Main.itemAnimations[item.type] != null) ? Main.itemAnimations[item.type].GetFrame(tex) : tex.Frame();

                    float maxDim = Math.Max(frame.Width, frame.Height);
                    float itemScale = (baseSize - 2f) / maxDim * scale;
                    Vector2 drawPos = position + new Vector2(baseSize * scale * 0.5f);
                    Vector2 origin = frame.Size() * 0.5f;

                    spriteBatch.Draw(tex, drawPos, frame, Color.White, 0f, origin, itemScale, SpriteEffects.None, 0f);

                    if (item.stack > 1)
                    {
                        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, item.stack.ToString(), position + new Vector2(0f, 10f * scale), Color.White, 0f, Vector2.Zero, new Vector2(0.75f * scale));
                    }

                    if (check)
                    {
                        Texture2D checkTex = RBTextures.Checkmark ?? TextureAssets.MagicPixel.Value;
                        spriteBatch.Draw(checkTex, position + new Vector2(10f * scale, 10f * scale), null, Color.White, 0f, Vector2.Zero, 0.8f * scale, SpriteEffects.None, 0f);
                    }
                }
                return true;
            }
        }

        TextSnippet ITagHandler.Parse(string text, Color baseColor, string options)
        {
            int itemType = 0;
            int stack = 1;
            bool check = false;
            string nameOverride = null;

            string[] parts = text.Split(':');
            if (parts.Length > 0) int.TryParse(parts[0], out itemType);
            if (parts.Length > 1) int.TryParse(parts[1], out stack);
            if (parts.Length > 2) bool.TryParse(parts[2], out check);
            if (parts.Length > 3) nameOverride = parts[3];

            Item item = new Item();
            item.SetDefaults(itemType);
            item.stack = stack;
            if (!string.IsNullOrEmpty(nameOverride))
            {
                item.SetNameOverride(nameOverride);
            }

            return new ItemHoverFixSnippet(item, check);
        }

        public static string GenerateTag(int itemType, int stack = 1, string nameOverride = null, bool check = false)
        {
            string overridePart = string.IsNullOrEmpty(nameOverride) ? "" : $":{nameOverride}";
            return $"[itemhover:{itemType}:{stack}:{check}{overridePart}]";
        }
    }
}
