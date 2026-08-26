using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI.Chat;

namespace RecipeBrowser.Common
{
    /// <summary>
    /// UI 通用扩展与绘图辅助垫片
    /// 作者: SaintCirno9
    /// </summary>
    public static class UICommon
    {
        public static readonly Color DefaultUIBlue = new Color(73, 94, 171);
        public static readonly Color DefaultUIBlueMouseOver = new Color(63, 82, 151) * 0.7f;
        public static readonly Color MainPanelBackground = new Color(63, 82, 151) * 0.7f;

        public static void DrawHoverStringInBounds(SpriteBatch spriteBatch, string text, Rectangle? bounds = null)
        {
            if (string.IsNullOrEmpty(text)) return;

            Vector2 stringSize = ChatManager.GetStringSize(FontAssets.MouseText.Value, text, Vector2.One);
            Vector2 position = new Vector2(Main.mouseX + 14, Main.mouseY + 14);

            Rectangle area = bounds ?? new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);

            if (position.X + stringSize.X > area.Right)
            {
                position.X = area.Right - stringSize.X;
            }
            if (position.Y + stringSize.Y > area.Bottom)
            {
                position.Y = area.Bottom - stringSize.Y;
            }
            if (position.X < area.Left)
            {
                position.X = area.Left;
            }
            if (position.Y < area.Top)
            {
                position.Y = area.Top;
            }

            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, text, position, Color.White, 0f, Vector2.Zero, Vector2.One);
        }

        public static void PlaySound(int soundType, int style = 1)
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        public static void TooltipMouseText(string text)
        {
            Main.hoverItemName = text;
        }
    }
}
