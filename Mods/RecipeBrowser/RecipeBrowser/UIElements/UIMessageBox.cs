using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.UI;
using Terraria.UI.Chat;

namespace RecipeBrowser.UIElements
{
    public class UIMessageBox : UIPanel
    {
        private string text;
        protected UIScrollbar _scrollbar;
        private float height;
        internal bool heightNeedsRecalculating;
        private List<List<TextSnippet>> drawTextSnippets = new List<List<TextSnippet>>();

        private static readonly List<char> cnPuncs = new List<char>
        {
            '–', '—', '‘', '’', '“', '”', '…', '、', '。', '〈',
            '〉', '《', '》', '「', '」', '『', '』', '【', '】', '〔',
            '〕', '！', '（', '）', '，', '．', '：', '；', '？'
        };

        public UIMessageBox(string text)
        {
            this.text = text;
            if (_scrollbar != null)
            {
                _scrollbar.ViewPosition = 0f;
                heightNeedsRecalculating = true;
            }
            OverflowHidden = true;
        }

        public override void OnActivate()
        {
            base.OnActivate();
            heightNeedsRecalculating = true;
        }

        internal void SetText(string text)
        {
            this.text = text;
            if (_scrollbar != null)
            {
                _scrollbar.ViewPosition = 0f;
                heightNeedsRecalculating = true;
            }
        }

        protected override void DrawChildren(SpriteBatch spriteBatch)
        {
            base.DrawChildren(spriteBatch);
            CalculatedStyle innerDimensions = GetInnerDimensions();
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            float num = 0f;
            if (_scrollbar != null)
            {
                num = -_scrollbar.GetValue();
            }

            foreach (var snippetList in drawTextSnippets)
            {
                TextSnippet[] array = snippetList.ToArray();
                float y = ChatManager.GetStringSize(font, array, Vector2.One, -1f).Y;
                if (num > -y)
                {
                    int hoveredSnippet = -1;
                    ChatManager.ConvertNormalSnippets(snippetList);
                    ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, array, new Vector2(innerDimensions.X, innerDimensions.Y + num), 0f, Vector2.Zero, Vector2.One, out hoveredSnippet, -1f, 2f);
                    if (hoveredSnippet > -1 && IsMouseHovering)
                    {
                        array[hoveredSnippet].OnHover();
                        if (Main.mouseLeft && Main.mouseLeftRelease)
                        {
                            array[hoveredSnippet].OnClick();
                        }
                    }
                }
                num += y;
                if (num > innerDimensions.Height) break;
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            // 注：原版 LockVanillaMouseScroll 为 tML 扩展，TPML 原版 PlayerInput 无此 API
            Recalculate();
        }

        public override void RecalculateChildren()
        {
            base.RecalculateChildren();
            if (!heightNeedsRecalculating) return;

            CalculatedStyle innerDimensions = GetInnerDimensions();
            if (innerDimensions.Width <= 0f || innerDimensions.Height <= 0f) return;

            DynamicSpriteFont font = FontAssets.MouseText.Value;
            drawTextSnippets = WordwrapStringSmart(text, Color.White, font, (int)innerDimensions.Width, -1);
            height = 0f;
            foreach (var snippetList in drawTextSnippets)
            {
                TextSnippet[] array = snippetList.ToArray();
                height += ChatManager.GetStringSize(font, array, Vector2.One, -1f).Y;
            }
            heightNeedsRecalculating = false;
        }

        public override void Recalculate()
        {
            base.Recalculate();
            UpdateScrollbar();
        }

        public override void ScrollWheel(UIScrollWheelEvent evt)
        {
            base.ScrollWheel(evt);
            if (_scrollbar != null)
            {
                _scrollbar.ViewPosition -= evt.ScrollWheelValue;
            }
        }

        public void SetScrollbar(UIScrollbar scrollbar)
        {
            _scrollbar = scrollbar;
            UpdateScrollbar();
            heightNeedsRecalculating = true;
        }

        private void UpdateScrollbar()
        {
            if (_scrollbar != null)
            {
                _scrollbar.SetView(GetInnerDimensions().Height, height);
            }
        }

        public static List<List<TextSnippet>> WordwrapStringSmart(string text, Color c, DynamicSpriteFont font, int maxWidth, int maxLines)
        {
            TextSnippet[] array = ChatManager.ParseMessage(text, c).ToArray();
            List<List<TextSnippet>> list = new List<List<TextSnippet>>();
            List<TextSnippet> currentLine = new List<TextSnippet>();

            foreach (TextSnippet snippet in array)
            {
                string[] parts = snippet.Text.Split('\n');
                for (int j = 0; j < parts.Length - 1; j++)
                {
                    currentLine.Add(snippet.CopyMorph(parts[j]));
                    list.Add(currentLine);
                    currentLine = new List<TextSnippet>();
                }
                currentLine.Add(snippet.CopyMorph(parts[parts.Length - 1]));
            }
            list.Add(currentLine);

            if (maxWidth != -1)
            {
                for (int k = 0; k < list.Count; k++)
                {
                    List<TextSnippet> line = list[k];
                    float currentWidth = 0f;
                    for (int l = 0; l < line.Count; l++)
                    {
                        float snippetWidth = ChatManager.GetStringSize(font, new[] { line[l] }, Vector2.One, -1f).X;
                        if (snippetWidth + currentWidth > maxWidth)
                        {
                            int available = maxWidth - (int)currentWidth;
                            if (currentWidth > 0f) available -= 16;
                            float limit = available;
                            bool exceed = false;
                            int splitIdx = -1;

                            for (int m = 0; m < line[l].Text.Length; m++)
                            {
                                if (exceed) break;
                                if (line[l].Text[m] == ' ' || isChinese(line[l].Text[m]))
                                {
                                    if (ChatManager.GetStringSize(font, line[l].Text.Substring(0, m), Vector2.One, -1f).X < limit)
                                    {
                                        splitIdx = m;
                                    }
                                    else
                                    {
                                        exceed = true;
                                    }
                                }
                            }
                            if (line[l].Text.Length == 0) exceed = true;

                            if (splitIdx == -1)
                            {
                                if (l == 0)
                                {
                                    var nextLine = new List<TextSnippet>();
                                    for (int n = l + 1; n < line.Count; n++) nextLine.Add(line[n]);
                                    list[k] = list[k].Take(1).ToList();
                                    list.Insert(k + 1, nextLine);
                                }
                                else
                                {
                                    var nextLine = new List<TextSnippet>();
                                    for (int n = l; n < line.Count; n++) nextLine.Add(line[n]);
                                    list[k] = list[k].Take(l).ToList();
                                    list.Insert(k + 1, nextLine);
                                }
                            }
                            else
                            {
                                string leftText = line[l].Text.Substring(0, splitIdx);
                                string rightText = line[l].Text.Substring(splitIdx).TrimStart();
                                var nextLine = new List<TextSnippet> { line[l].CopyMorph(rightText) };
                                for (int n = l + 1; n < line.Count; n++) nextLine.Add(line[n]);
                                line[l] = line[l].CopyMorph(leftText);
                                list[k] = list[k].Take(l + 1).ToList();
                                list.Insert(k + 1, nextLine);
                            }
                            break;
                        }
                        currentWidth += snippetWidth;
                    }
                }
            }

            if (maxLines != -1)
            {
                while (list.Count > maxLines)
                {
                    list.RemoveAt(maxLines);
                }
            }
            return list;
        }

        public static bool isChinese(char a)
        {
            if (a < '一' || a > '龥')
            {
                return cnPuncs.Contains(a);
            }
            return true;
        }
    }
}
