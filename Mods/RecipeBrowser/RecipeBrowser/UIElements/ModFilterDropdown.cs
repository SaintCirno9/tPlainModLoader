using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria.UI.Chat;

namespace RecipeBrowser.UIElements
{
    public class ModFilterDropdownRow : UIElement
    {
        private readonly int _modIndex;
        private readonly string _displayText;
        private readonly string _fullText;
        private readonly bool _selected;
        private readonly Action<int> _onSelect;

        public ModFilterDropdownRow(int modIndex, string displayText, bool selected, Action<int> onSelect)
        {
            _modIndex = modIndex;
            _displayText = displayText;
            _fullText = displayText;
            _selected = selected;
            _onSelect = onSelect;

            Height.Set(24f, 0f);
            Width.Set(0f, 1f);

            // 超长模组名二分截断为省略号（对齐原版 ComputeTruncationOnce），悬停显示全文
            string truncated = displayText;
            try
            {
                const float maxTextWidth = 196f;
                DynamicSpriteFont font = FontAssets.MouseText.Value;
                float fullWidth = ChatManager.GetStringSize(font, displayText, Vector2.One, -1f).X * 0.85f;
                if (fullWidth > maxTextWidth)
                {
                    int cut = displayText.Length;
                    while (cut > 1 && ChatManager.GetStringSize(font, displayText.Substring(0, cut) + "…", Vector2.One, -1f).X * 0.85f > maxTextWidth)
                    {
                        cut--;
                    }
                    truncated = displayText.Substring(0, cut) + "…";
                }
            }
            catch { }

            UIText text = new UIText(truncated, 0.85f);
            text.Left.Set(8f, 0f);
            text.VAlign = 0.5f;
            Append(text);
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            _onSelect?.Invoke(_modIndex);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            CalculatedStyle dimensions = GetDimensions();
            if (_selected)
            {
                spriteBatch.Draw(Terraria.GameContent.TextureAssets.MagicPixel.Value, dimensions.ToRectangle(), Color.LightSeaGreen * 0.4f);
            }
            if (IsMouseHovering)
            {
                spriteBatch.Draw(Terraria.GameContent.TextureAssets.MagicPixel.Value, dimensions.ToRectangle(), Color.White * 0.2f);
                if (_displayText != _fullText || _displayText.Length > 0)
                {
                    UICommon.TooltipMouseText(_fullText);
                }
            }
        }
    }

    public class ModFilterDropdown : UIPanel
    {
        private readonly string[] _mods;
        private readonly Func<int, string> _getDisplayName;
        private readonly Action<int> _onSelect;
        private readonly List<ModFilterDropdownRow> _rows = new List<ModFilterDropdownRow>();

        public ModFilterDropdown(string[] mods, Func<int, string> getDisplayName, int selectedIndex, Action<int> onSelect)
        {
            _mods = mods;
            _getDisplayName = getDisplayName;
            _onSelect = onSelect;

            SetPadding(4f);
            BackgroundColor = new Color(23, 25, 48, 245);
            BorderColor = new Color(73, 94, 171);

            BuildContent(selectedIndex);
        }

        private void BuildContent(int selectedIndex)
        {
            UIList list = new UIList();
            list.Width.Set(0f, 1f);
            list.Height.Set(0f, 1f);
            list.ListPadding = 2f;
            Append(list);

            if (_mods.Length > 8)
            {
                InvisibleFixedUIScrollbar scrollbar = new InvisibleFixedUIScrollbar(RecipeBrowserUI.instance.userInterface);
                scrollbar.Height.Set(-8f, 1f);
                scrollbar.Top.Set(4f, 0f);
                scrollbar.HAlign = 1f;
                list.SetScrollbar(scrollbar);
                Append(scrollbar);
            }

            for (int i = 0; i < _mods.Length; i++)
            {
                string displayText = _getDisplayName(i);
                ModFilterDropdownRow row = new ModFilterDropdownRow(i, displayText, selectedIndex == i, _onSelect);
                _rows.Add(row);
                list.Add(row);
            }
        }
    }
}
