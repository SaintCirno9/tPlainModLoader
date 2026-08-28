using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace RecipeBrowser.TagHandlers
{
    /// <summary>
    /// [image] 标签处理器 —— 恢复原版 options 语法（t tooltip / s scale / v vOffset）+ 资源容错
    /// 作者: SaintCirno9
    /// </summary>
    public class ImageTagHandler : ITagHandler
    {
        private class ImageSnippet : TextSnippet
        {
            private Texture2D texture;
            private string tooltip;
            private string texturePath;
            public int vOffset;
            private float otherScale;

            public ImageSnippet(string texturePath, string tooltip, float scale, float otherScale)
                : base("", Color.White)
            {
                this.texturePath = texturePath;
                this.tooltip = tooltip;
                this.otherScale = otherScale;
                DeleteWhole = true;
            }

            private Texture2D Texture
            {
                get
                {
                    if (texture == null || texture.IsDisposed)
                    {
                        texture = RBTextures.GetTexture(texturePath);
                    }
                    return texture;
                }
            }

            public override void OnHover()
            {
                if (!string.IsNullOrEmpty(tooltip))
                {
                    UICommon.TooltipMouseText(tooltip);
                }
            }

            public override bool UniqueDraw(bool justCheckingSize, out Vector2 size, SpriteBatch spriteBatch, Vector2 position = default(Vector2), Color color = default(Color), float scale = 1f)
            {
                Texture2D tex = Texture;
                if (tex == null)
                {
                    size = Vector2.Zero;
                    return true;
                }
                size = new Vector2(tex.Width * otherScale, tex.Height * otherScale) + new Vector2(0f, vOffset);
                if (!justCheckingSize && color != Color.Black)
                {
                    spriteBatch.Draw(tex, position + new Vector2(0f, vOffset), null, color, 0f, Vector2.Zero, otherScale, SpriteEffects.None, 0f);
                }
                return true;
            }
        }

        TextSnippet ITagHandler.Parse(string text, Color baseColor, string options)
        {
            string tooltip = null;
            float result = 1f;
            int vOffset = 0;

            if (!string.IsNullOrEmpty(options))
            {
                string[] opts = options.Split(',');
                foreach (string o in opts)
                {
                    if (o.Length == 0) continue;
                    if (o[0] == 't') tooltip = o.Substring(1).Replace(';', ':');
                    if (o[0] == 's') float.TryParse(o.Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
                    if (o[0] == 'v') int.TryParse(o.Substring(1), out vOffset);
                }
            }

            // 资源存在性容错：无法加载时回退纯文本（对齐原版 HasAsset 语义）
            if (RBTextures.GetTexture(text) != null)
            {
                return new ImageSnippet(text, tooltip, 1f, result)
                {
                    vOffset = vOffset
                };
            }
            return new TextSnippet(text);
        }

        public static string GenerateTag(string texturePath, float scale = 1f)
        {
            return $"[image/s{scale.ToString(CultureInfo.InvariantCulture)}:{texturePath}]";
        }
    }
}
