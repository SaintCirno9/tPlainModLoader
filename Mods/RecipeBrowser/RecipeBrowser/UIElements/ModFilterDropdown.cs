using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace RecipeBrowser.UIElements
{
    public class ModFilterDropdownRow : UIElement
    {
        private readonly int _modIndex;
        private readonly string _displayText;
        private readonly bool _selected;
        private readonly Action<int> _onSelect;

        public ModFilterDropdownRow(int modIndex, string displayText, bool selected, Action<int> onSelect)
        {
            _modIndex = modIndex;
            _displayText = displayText;
            _selected = selected;
            _onSelect = onSelect;

            Height.Set(24f, 0f);
            Width.Set(0f, 1f);

            UIText text = new UIText(displayText, 0.85f);
            text.Left.Set(6f, 0f);
            text.Top.Set(4f, 0f);
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
            if (IsMouseHovering)
            {
                CalculatedStyle dimensions = GetDimensions();
                spriteBatch.Draw(Terraria.GameContent.TextureAssets.MagicPixel.Value, dimensions.ToRectangle(), Color.White * 0.15f);
            }
            if (_selected)
            {
                CalculatedStyle dimensions = GetDimensions();
                spriteBatch.Draw(Terraria.GameContent.TextureAssets.MagicPixel.Value, dimensions.ToRectangle(), Color.LightSeaGreen * 0.35f);
            }
        }
    }

    public class ModFilterDropdown : UIElement
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

            Width.Set(200f, 0f);
            Height.Set(Math.Min(240f, mods.Length * 28f + 16f), 0f);

            BuildContent(selectedIndex);
        }

        private void BuildContent(int selectedIndex)
        {
            UIPanel innerPanel = new UIPanel();
            innerPanel.Width.Set(0f, 1f);
            innerPanel.Height.Set(0f, 1f);
            innerPanel.Top.Set(0f, 0f);
            innerPanel.BackgroundColor = new Color(20, 30, 60, 240);
            innerPanel.SetPadding(6f);
            Append(innerPanel);

            UIList list = new UIList();
            list.Width.Set(0f, 1f);
            list.Height.Set(0f, 1f);
            list.ListPadding = 4f;
            innerPanel.Append(list);

            InvisibleFixedUIScrollbar scrollbar = new InvisibleFixedUIScrollbar(RecipeBrowserUI.instance.userInterface);
            scrollbar.Height.Set(-12f, 1f);
            scrollbar.Top.Set(6f, 0f);
            scrollbar.HAlign = 1f;
            list.SetScrollbar(scrollbar);
            Append(scrollbar);

            for (int i = 0; i < _mods.Length; i++)
            {
                string displayText = _getDisplayName(i);
                ModFilterDropdownRow row = new ModFilterDropdownRow(i, displayText, selectedIndex == i, OnRowSelected);
                _rows.Add(row);
                list.Add(row);
            }
        }

        private void OnRowSelected(int index)
        {
            _onSelect?.Invoke(index);
            if (Parent != null)
            {
                Parent.RemoveChild(this);
            }
        }
    }
}
