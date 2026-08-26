using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using Terraria;
using Terraria.GameInput;
using Terraria.UI;

namespace RecipeBrowser.UIElements
{
    public class UIHorizontalGrid : UIElement
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
        protected UIHorizontalScrollbar _scrollbar;
        internal UIElement _innerList = new UIInnerList();
        private float _innerListWidth;
        public float ListPadding = 5f;
        public bool drawArrows;

        public int Count => _items.Count;

        public UIHorizontalGrid()
        {
            _innerList.OverflowHidden = false;
            _innerList.Width.Set(0f, 1f);
            _innerList.Height.Set(0f, 1f);
            OverflowHidden = true;
            Append(_innerList);
        }

        public float GetTotalWidth()
        {
            return _innerListWidth;
        }

        public void Goto(ElementSearchMethod searchMethod, bool center = false)
        {
            if (_scrollbar == null) return;
            for (int i = 0; i < _items.Count; i++)
            {
                if (searchMethod(_items[i]))
                {
                    _scrollbar.ViewPosition = _items[i].Left.Pixels;
                    if (center)
                    {
                        _scrollbar.ViewPosition = _items[i].Left.Pixels - GetInnerDimensions().Width / 2f + _items[i].GetOuterDimensions().Width / 2f;
                    }
                    break;
                }
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
            float height = GetInnerDimensions().Height;
            base.RecalculateChildren();
            float top = 0f;
            float left = 0f;
            float maxWidth = 0f;

            for (int i = 0; i < _items.Count; i++)
            {
                UIElement item = _items[i];
                CalculatedStyle outerDimensions = item.GetOuterDimensions();
                if (top + outerDimensions.Height > height && top > 0f)
                {
                    left += maxWidth + ListPadding;
                    top = 0f;
                    maxWidth = 0f;
                }
                maxWidth = Math.Max(maxWidth, outerDimensions.Width);
                item.Top.Set(top, 0f);
                top += outerDimensions.Height + ListPadding;
                item.Left.Set(left, 0f);
                item.Recalculate();
            }
            _innerListWidth = left + maxWidth;
        }

        private void UpdateScrollbar()
        {
            if (_scrollbar != null)
            {
                _scrollbar.SetView(GetInnerDimensions().Width, _innerListWidth);
            }
        }

        public void SetScrollbar(UIHorizontalScrollbar scrollbar)
        {
            _scrollbar = scrollbar;
            UpdateScrollbar();
        }

        public void UpdateOrder()
        {
            _items.Sort(SortMethod);
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
                _innerList.Left.Set(-_scrollbar.GetValue(), 0f);
            }
            Recalculate();
        }

        protected override void DrawChildren(SpriteBatch spriteBatch)
        {
            base.DrawChildren(spriteBatch);
            if (drawArrows && _scrollbar != null)
            {
                Rectangle rect = GetInnerDimensions().ToRectangle();
                var moreLeft = RBTextures.MoreLeft;
                var moreRight = RBTextures.MoreRight;

                if (moreLeft != null && _scrollbar.ViewPosition != 0f)
                {
                    int y = rect.Y + rect.Height / 2 - moreLeft.Height / 2;
                    spriteBatch.Draw(moreLeft, new Vector2(rect.X, y), Color.White * 0.5f);
                }
                if (moreRight != null && _scrollbar.ViewPosition < _innerListWidth - rect.Width)
                {
                    int y = rect.Y + rect.Height / 2 - moreRight.Height / 2;
                    spriteBatch.Draw(moreRight, new Vector2(rect.Right - moreRight.Width, y), Color.White * 0.5f);
                }
            }
        }
    }
}
