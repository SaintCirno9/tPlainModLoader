using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using ReLogic.Graphics;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace RecipeBrowser.UIElements
{
    public sealed class ModFilterDropdown : UIPanel
    {
        private sealed class ModFilterDropdownRow : UIPanel
        {
            private const float TextScale = 0.85f;
            private readonly string _fullText;
            private readonly UIText _label;
            private bool _selected;
            private bool _hasComputedTruncation;
            private bool _isTruncated;
            internal int Index { get; }

            internal ModFilterDropdownRow(int index, string displayText, bool selected, Action<int> onSelect)
            {
                _fullText = displayText;
                _selected = selected;
                Index = index;

                Width.Set(0f, 1f);
                Height.Set(30f, 0f);

                _label = new UIText(displayText, TextScale, false)
                {
                    VAlign = 0.5f
                };
                Append(_label);

                OnLeftClick += (evt, el) =>
                {
                    onSelect?.Invoke(Index);
                };

                OnMouseOver += (evt, el) =>
                {
                    if (!_selected)
                    {
                        BackgroundColor = Color.DarkRed * 0.3f;
                        BorderColor = Color.DarkRed * 0.3f;
                    }
                };

                OnMouseOut += (evt, el) =>
                {
                    Refresh();
                };

                Refresh();
            }

            internal void SetSelected(bool selected)
            {
                _selected = selected;
                Refresh();
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                base.DrawSelf(spriteBatch);
                if (!_hasComputedTruncation)
                {
                    ComputeTruncationOnce();
                    _hasComputedTruncation = true;
                }
                if (_isTruncated && IsMouseHovering)
                {
                    UICommon.TooltipMouseText(_fullText);
                }
                if (IsMouseHovering)
                {
                    RecipeBrowserUI.modHoverIndex = Index;
                    RecipeBrowserUI.instance?.UpdateModHoverImage();
                }
            }

            private void Refresh()
            {
                BackgroundColor = _selected ? Color.DarkRed : Color.Transparent;
                BorderColor = _selected ? Color.DarkRed : Color.Transparent;
            }

            private void ComputeTruncationOnce()
            {
                float width = GetInnerDimensions().Width;
                if (width <= 0f)
                {
                    _label.SetText(string.Empty);
                    _isTruncated = true;
                    return;
                }
                DynamicSpriteFont font = FontAssets.MouseText.Value;
                float targetWidth = width / TextScale;
                if (font.MeasureString(_fullText).X <= targetWidth)
                {
                    _label.SetText(_fullText);
                    _isTruncated = false;
                    return;
                }
                float ellipsisWidth = font.MeasureString("...").X;
                if (ellipsisWidth > targetWidth)
                {
                    _label.SetText(string.Empty);
                    _isTruncated = true;
                    return;
                }
                int low = 0;
                int high = _fullText.Length;
                while (low < high)
                {
                    int mid = (low + high + 1) >> 1;
                    if (font.MeasureString(_fullText.Substring(0, mid)).X + ellipsisWidth <= targetWidth)
                    {
                        low = mid;
                    }
                    else
                    {
                        high = mid - 1;
                    }
                }
                _label.SetText(_fullText.Substring(0, low) + "...");
                _isTruncated = true;
            }
        }

        private readonly string[] _mods;
        private readonly Func<int, string> _getDisplayName;
        private readonly List<ModFilterDropdownRow> _rows = new List<ModFilterDropdownRow>();

        public event EventHandler<int> SelectedIndexChanged;

        public ModFilterDropdown(string[] mods, int selectedIndex, Func<int, string> getDisplayName)
        {
            _mods = mods ?? Array.Empty<string>();
            _getDisplayName = getDisplayName ?? (i => string.Empty);

            Width.Set(300f, 0f);
            Height.Set(-50f, 1f);
            Top.Set(20f, 0f);
            Left.Set(-300f, 1f);
            SetPadding(6f);
            BackgroundColor = Color.DarkRed;

            BuildContent(selectedIndex);
        }

        public void SelectIndex(int index)
        {
            if (_rows.Count != 0)
            {
                int clamped = Math.Max(0, Math.Min(index, _rows.Count - 1));
                OnRowSelected(clamped);
            }
        }

        private void BuildContent(int selectedIndex)
        {
            UIPanel innerPanel = new UIPanel();
            innerPanel.Width.Set(0f, 1f);
            innerPanel.Height.Set(0f, 1f);
            innerPanel.Top.Set(0f, 0f);
            innerPanel.BackgroundColor = new Color(200, 50, 50, 255);
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

            if (_rows.Count > 1)
            {
                _rows[_rows.Count - 1].MarginBottom = -list.ListPadding;
            }

            UIImage topCover = new UIImage(TextureAssets.MagicPixel)
            {
                IgnoresMouseInteraction = true,
                Color = BackgroundColor,
                ScaleToFit = true
            };
            topCover.Top.Set(-6f, 0f);
            topCover.Left.Set(-69f, 1f);
            topCover.Width.Set(63f, 0f);
            topCover.Height.Set(2f, 0f);
            Append(topCover);

            OnRowSelected(selectedIndex);
        }

        private void OnRowSelected(int index)
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                _rows[i].SetSelected(i == index);
            }
            SelectedIndexChanged?.Invoke(this, index);
        }
    }
}
