using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.UI;

namespace RecipeBrowser.UIElements
{
    public class UIGrid : UIElement
    {
        public delegate bool ElementSearchMethod(UIElement element);

        private class UIInnerList : UIElement
        {
            public override bool ContainsPoint(Vector2 point)
            {
                return true;
            }

            protected override void DrawChildren(SpriteBatch spriteBatch)
            {
                CalculatedStyle parentDimensions = Parent.GetDimensions();
                Vector2 parentPos = parentDimensions.Position();
                Vector2 parentSize = new Vector2(parentDimensions.Width, parentDimensions.Height);

                foreach (UIElement element in Elements)
                {
                    CalculatedStyle dims = element.GetDimensions();
                    Vector2 elPos = dims.Position();
                    Vector2 elSize = new Vector2(dims.Width, dims.Height);

                    if (Collision.CheckAABBvAABBCollision(parentPos, parentSize, elPos, elSize))
                    {
                        element.Draw(spriteBatch);
                    }
                }
            }
        }

        public List<UIElement> _items = new List<UIElement>();
        protected UIScrollbar _scrollbar;
        internal UIElement _innerList = new UIInnerList();
        private float _innerListHeight;
        public float ListPadding = 5f;
        internal Comparison<UIElement> alternateSort;
        public bool drawArrows;

        public int Count => _items.Count;

        public UIGrid()
        {
            _innerList.OverflowHidden = false;
            _innerList.Width.Set(0f, 1f);
            _innerList.Height.Set(0f, 1f);
            OverflowHidden = true;
            Append(_innerList);
        }

        public float GetTotalHeight()
        {
            return _innerListHeight;
        }

        public void Goto(ElementSearchMethod searchMethod, bool center = false, bool fuzzy = false)
        {
            if (_scrollbar == null) return;
            float height = GetInnerDimensions().Height;
            for (int i = 0; i < _items.Count; i++)
            {
                UIElement el = _items[i];
                if (!searchMethod(el)) continue;

                if (!fuzzy || !(el.Top.Pixels > _scrollbar.ViewPosition) || !(el.Top.Pixels + el.GetOuterDimensions().Height < _scrollbar.ViewPosition + height))
                {
                    _scrollbar.ViewPosition = el.Top.Pixels;
                    if (center)
                    {
                        _scrollbar.ViewPosition = el.Top.Pixels - height / 2f + el.GetOuterDimensions().Height / 2f;
                    }
                }
                break;
            }
        }

        public virtual void Add(UIElement item)
        {
            _items.Add(item);
            _innerList.Append(item);
            UpdateOrder();
            _innerList.Recalculate();
        }

        public virtual void AddRange(IEnumerable<UIElement> items)
        {
            _items.AddRange(items);
            foreach (UIElement item in items)
            {
                _innerList.Append(item);
            }
            UpdateOrder();
            _innerList.Recalculate();
        }

        public virtual bool Remove(UIElement item)
        {
            _innerList.RemoveChild(item);
            UpdateOrder();
            return _items.Remove(item);
        }

        public virtual void Clear()
        {
            _innerList.RemoveAllChildren();
            _items.Clear();
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

        public override void RecalculateChildren()
        {
            float width = GetInnerDimensions().Width;
            base.RecalculateChildren();
            float top = 0f;
            float left = 0f;
            float maxHeight = 0f;

            for (int i = 0; i < _items.Count; i++)
            {
                UIElement item = _items[i];
                CalculatedStyle outerDimensions = item.GetOuterDimensions();
                if (left + outerDimensions.Width > width && left > 0f)
                {
                    top += maxHeight + ListPadding;
                    left = 0f;
                    maxHeight = 0f;
                }
                maxHeight = Math.Max(maxHeight, outerDimensions.Height);
                item.Left.Set(left, 0f);
                left += outerDimensions.Width + ListPadding;
                item.Top.Set(top, 0f);
                item.Recalculate();
            }
            _innerListHeight = top + maxHeight;
        }

        private void UpdateScrollbar()
        {
            if (_scrollbar != null)
            {
                _scrollbar.SetView(GetInnerDimensions().Height, _innerListHeight);
            }
        }

        public void SetScrollbar(UIScrollbar scrollbar)
        {
            _scrollbar = scrollbar;
            UpdateScrollbar();
        }

        public void UpdateOrder()
        {
            if (alternateSort != null)
            {
                _items.Sort(alternateSort);
            }
            else
            {
                _items.Sort(SortMethod);
            }
            UpdateScrollbar();
        }

        public int SortMethod(UIElement item1, UIElement item2)
        {
            return item1.CompareTo(item2);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (_scrollbar != null)
            {
                _innerList.Top.Set(-_scrollbar.GetValue(), 0f);
            }
            Recalculate();
        }

        protected override void DrawChildren(SpriteBatch spriteBatch)
        {
            base.DrawChildren(spriteBatch);
            if (drawArrows && _scrollbar != null)
            {
                Rectangle rect = GetInnerDimensions().ToRectangle();
                var moreUp = RBTextures.MoreUp;
                var moreDown = RBTextures.MoreDown;

                if (moreUp != null && _scrollbar.ViewPosition != 0f)
                {
                    int x = rect.X + rect.Width / 2 - moreUp.Width / 2;
                    spriteBatch.Draw(moreUp, new Vector2(x, rect.Y), Color.White * 0.5f);
                }
                if (moreDown != null && _scrollbar.ViewPosition < _innerListHeight - rect.Height)
                {
                    int x = rect.X + rect.Width / 2 - moreDown.Width / 2;
                    spriteBatch.Draw(moreDown, new Vector2(x, rect.Bottom - moreDown.Height), Color.White * 0.5f);
                }
            }
        }
    }
}
