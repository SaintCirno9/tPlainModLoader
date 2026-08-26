using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace RecipeBrowser.UIElements
{
    public class UIDragableElement : UIElement
    {
        private Vector2 offset;
        private bool dragable;
        private bool dragging;
        private bool resizeableX;
        private bool resizeableY;
        private bool resizeing;
        private List<UIElement> additionalDragTargets;

        private bool resizeable => resizeableX || resizeableY;

        public UIDragableElement(bool dragable = true, bool resizeableX = false, bool resizeableY = false)
        {
            this.dragable = dragable;
            this.resizeableX = resizeableX;
            this.resizeableY = resizeableY;
            additionalDragTargets = new List<UIElement>();
        }

        public void AddDragTarget(UIElement element)
        {
            if (element != null && !additionalDragTargets.Contains(element))
                additionalDragTargets.Add(element);
        }

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            DragStart(evt);
            base.LeftMouseDown(evt);
        }

        public override void LeftMouseUp(UIMouseEvent evt)
        {
            DragEnd(evt);
            base.LeftMouseUp(evt);
        }

        private void DragStart(UIMouseEvent evt)
        {
            CalculatedStyle innerDimensions = GetInnerDimensions();
            if (evt.Target != this && !additionalDragTargets.Contains(evt.Target))
            {
                return;
            }
            if (resizeable)
            {
                Rectangle resizeRect = new Rectangle((int)(innerDimensions.X + innerDimensions.Width - 18f), (int)(innerDimensions.Y + innerDimensions.Height - 18f), 18, 18);
                if (resizeRect.Contains(evt.MousePosition.ToPoint()))
                {
                    offset = new Vector2(evt.MousePosition.X - innerDimensions.X - innerDimensions.Width, evt.MousePosition.Y - innerDimensions.Y - innerDimensions.Height);
                    resizeing = true;
                    return;
                }
            }
            if (dragable)
            {
                offset = new Vector2(evt.MousePosition.X - Left.Pixels, evt.MousePosition.Y - Top.Pixels);
                dragging = true;
            }
        }

        private void DragEnd(UIMouseEvent evt)
        {
            if (evt.Target == this || additionalDragTargets.Contains(evt.Target))
            {
                dragging = false;
                resizeing = false;
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle outerDimensions = GetOuterDimensions();
            if (ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.LocalPlayer.cursorItemIconEnabled = false;
            }
            if (dragging)
            {
                Left.Set(Main.MouseScreen.X - offset.X, 0f);
                Top.Set(Main.MouseScreen.Y - offset.Y, 0f);
                Recalculate();
            }
            if (resizeing)
            {
                if (resizeableX)
                {
                    Width.Pixels = Main.MouseScreen.X - outerDimensions.X - offset.X;
                }
                if (resizeableY)
                {
                    Height.Pixels = Main.MouseScreen.Y - outerDimensions.Y - offset.Y;
                }
                Recalculate();
            }
            base.DrawSelf(spriteBatch);
        }

        protected override void DrawChildren(SpriteBatch spriteBatch)
        {
            base.DrawChildren(spriteBatch);
            if (resizeable)
            {
                DrawDragAnchor(spriteBatch, TextureAssets.MagicPixel.Value, Color.Black);
            }
        }

        private void DrawDragAnchor(SpriteBatch spriteBatch, Texture2D texture, Color color)
        {
            CalculatedStyle outerDimensions = GetOuterDimensions();
            Point pt = new Point((int)(outerDimensions.X + outerDimensions.Width - 12f), (int)(outerDimensions.Y + outerDimensions.Height - 12f));
            spriteBatch.Draw(texture, new Rectangle(pt.X - 2, pt.Y - 2, 10, 10), new Rectangle(0, 0, 1, 1), color);
            spriteBatch.Draw(texture, new Rectangle(pt.X - 4, pt.Y - 4, 8, 8), new Rectangle(0, 0, 1, 1), color);
            spriteBatch.Draw(texture, new Rectangle(pt.X - 6, pt.Y - 6, 6, 6), new Rectangle(0, 0, 1, 1), color);
        }
    }
}
