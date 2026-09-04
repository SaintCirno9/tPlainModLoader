using System;
using System.Collections.Generic;
using CommandHelp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using ReLogic.OS;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Chat;
using Terraria.UI;
using Terraria.UI.Chat;
using TPML.Content;

namespace OptimizeAndTool.Content
{
    /// <summary>
    /// 聊天文本复制功能（基于 TPML ModSystem 与 HookGen 强类型门面）
    /// 作者: SaintCirno9
    /// </summary>
    internal class CopyChat : TPML.Content.ModSystem
    {
        public static GetSetReset<bool> Enable = new GetSetReset<bool>(true, true);

        public static List<CommandObject> GetCO()
        {
            List<CommandObject> cos = new List<CommandObject>
            {
                CommandBuild.get2("copyChat", Enable),
            };

            return cos;
        }

        public static List<UIElement> GetUI()
        {
            List<UIElement> uis = new List<UIElement>
            {
                UIBuild.get2(Enable, text: "复制聊天文本"),
            };

            return uis;
        }

        public override void Load()
        {
            On_RemadeChatMonitor.DrawChat += (orig, self, drawingPlayerChat) =>
            {
                orig(self, drawingPlayerChat);
                DrawChatPostfix(self, drawingPlayerChat);
            };
        }

        private static void DrawChatPostfix(RemadeChatMonitor self, bool drawingPlayerChat)
        {
            if (Enable.val == false || self == null) return;

            int showCount = self._showCount;
            int startChatLine = self._startChatLine;
            List<ChatMessageContainer> messages = self._messages;

            int num = startChatLine;
            int i2 = 0;
            int num3 = 0;
            while (num > 0 && i2 < messages.Count)
            {
                int num4 = Math.Min(num, messages[i2].LineCount);
                num -= num4;
                num3 += num4;
                if (num3 == messages[i2].LineCount)
                {
                    num3 = 0;
                    i2++;
                }
            }

            int i = 0;
            while (i < showCount && i2 < messages.Count)
            {
                ChatMessageContainer chatMessageContainer = messages[i2];
                if (!chatMessageContainer.Prepared || !(drawingPlayerChat | chatMessageContainer.CanBeShownWhenChatIsClosed))
                {
                    break;
                }

                int size = 22;
                Vector2 pos = new Vector2(88f, (float)(Main.screenHeight - 30 - 28 - i * size));
                ++i;

                Texture2D texture = Main.Assets.Request<Texture2D>("Images/UI/Cursor_7").Value;
                Rectangle rect = new Rectangle((int)pos.X, (int)pos.Y, texture.Width, texture.Height);
                rect.Width = size - 4;
                rect.Height = size - 4;
                rect.X -= rect.Width + 4;

                bool isMouse = false;

                if (Main.mouseX >= rect.X && Main.mouseX <= rect.X + rect.Width)
                {
                    if (Main.mouseY >= rect.Y && Main.mouseY <= rect.Y + rect.Height)
                    {
                        isMouse = true;
                        string text = chatMessageContainer.OriginalText ?? string.Empty;

                        if (Main.mouseLeft && Main.mouseLeftRelease)
                        {
                            SoundEngine.PlaySound(12);
                            Platform.Get<IClipboard>().Value = text;
                        }
                        else
                        {
                            TPML.Content.DrawTip.Draw(Main.spriteBatch, new string[] { $"复制[{text}]" });
                        }
                    }
                }

                Main.spriteBatch.Draw(texture, rect, isMouse ? Color.White : Color.White * 0.5f);

                num3++;
                if (num3 >= chatMessageContainer.LineCount)
                {
                    num3 = 0;
                    i2++;
                }
            }
        }
    }
}
