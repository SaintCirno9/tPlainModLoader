using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace RecipeBrowser.TagHandlers
{
    public class ImageTagHandler : ITagHandler
    {
        private class ImageSnippet : TextSnippet
        {
            private Texture2D texture;
            private float scale;

            public ImageSnippet(Texture2D texture, float scale = 1f)
                : base("")
            {
                this.texture = texture;
                this.scale = scale;
            }

            public override bool UniqueDraw(bool justCheckingSize, out Vector2 size, SpriteBatch spriteBatch, Vector2 position = default(Vector2), Color color = default(Color), float scale = 1f)
            {
                if (texture == null)
                {
                    size = Vector2.Zero;
                    return true;
                }

                Vector2 texSize = new Vector2(texture.Width, texture.Height) * this.scale;
                size = texSize * scale;

                if (!justCheckingSize && spriteBatch != null)
                {
                    spriteBatch.Draw(texture, position, null, Color.White, 0f, Vector2.Zero, this.scale * scale, SpriteEffects.None, 0f);
                }
                return true;
            }
        }

        TextSnippet ITagHandler.Parse(string text, Color baseColor, string options)
        {
            float scale = 1f;
            if (!string.IsNullOrEmpty(options))
            {
                float.TryParse(options, out scale);
            }
            Texture2D tex = RBTextures.GetTexture(text) ?? TextureAssets.MagicPixel.Value;
            return new ImageSnippet(tex, scale);
        }

        public static string GenerateTag(string texturePath, float scale = 1f)
        {
            return $"[image/{scale}:{texturePath}]";
        }
    }
}
