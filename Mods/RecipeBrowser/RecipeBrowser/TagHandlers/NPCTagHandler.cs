using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace RecipeBrowser.TagHandlers
{
    public class NPCTagHandler : ITagHandler
    {
        private class NPCSnippet : TextSnippet
        {
            private int npcType;

            public NPCSnippet(int npcType)
                : base("")
            {
                this.npcType = npcType;
                CheckForHover = true;
            }

            public override void OnHover()
            {
                Main.hoverItemName = Lang.GetNPCNameValue(npcType);
            }

            public override bool UniqueDraw(bool justCheckingSize, out Vector2 size, SpriteBatch spriteBatch, Vector2 position = default(Vector2), Color color = default(Color), float scale = 1f)
            {
                if (npcType <= 0 || npcType >= TextureAssets.NpcHead.Length)
                {
                    size = new Vector2(32f * scale);
                    return true;
                }

                int headIndex = NPC.TypeToDefaultHeadIndex(npcType);
                Texture2D texture = (headIndex >= 0 && headIndex < TextureAssets.NpcHead.Length) ? TextureAssets.NpcHead[headIndex]?.Value : null;
                if (texture == null)
                {
                    texture = TextureAssets.MagicPixel.Value;
                }

                size = new Vector2(texture.Width, texture.Height) * scale;
                if (!justCheckingSize && spriteBatch != null)
                {
                    spriteBatch.Draw(texture, position, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
                return true;
            }
        }

        TextSnippet ITagHandler.Parse(string text, Color baseColor, string options)
        {
            if (int.TryParse(text, out int npcType))
            {
                return new NPCSnippet(npcType);
            }
            return new TextSnippet(text);
        }

        public static string GenerateTag(int npcType)
        {
            return $"[npc:{npcType}]";
        }
    }
}
