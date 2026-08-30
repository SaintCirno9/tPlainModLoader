using Microsoft.Xna.Framework;
using Terraria.UI;

namespace PotionSlots.Core.Loaders.UILoading
{
    public class SmartUIElement : UIElement
    {
        public virtual void SafeMouseUp(UIMouseEvent evt) { }
        public override void LeftMouseUp(UIMouseEvent evt)
        {
            base.LeftMouseUp(evt);
            SafeMouseUp(evt);
        }

        public virtual void SafeMouseDown(UIMouseEvent evt) { }
        public override void LeftMouseDown(UIMouseEvent evt)
        {
            base.LeftMouseDown(evt);
            SafeMouseDown(evt);
        }

        public virtual void SafeClick(UIMouseEvent evt) { }
        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            SafeClick(evt);
        }

        public virtual void SafeDoubleClick(UIMouseEvent evt) { }
        public override void LeftDoubleClick(UIMouseEvent evt)
        {
            base.LeftDoubleClick(evt);
            SafeDoubleClick(evt);
        }

        public virtual void SafeRightMouseUp(UIMouseEvent evt) { }
        public override void RightMouseUp(UIMouseEvent evt)
        {
            base.RightMouseUp(evt);
            SafeRightMouseUp(evt);
        }

        public virtual void SafeRightMouseDown(UIMouseEvent evt) { }
        public override void RightMouseDown(UIMouseEvent evt)
        {
            base.RightMouseDown(evt);
            SafeRightMouseDown(evt);
        }

        public virtual void SafeRightClick(UIMouseEvent evt) { }
        public override void RightClick(UIMouseEvent evt)
        {
            base.RightClick(evt);
            SafeRightClick(evt);
        }

        public virtual void SafeRightDoubleClick(UIMouseEvent evt) { }
        public override void RightDoubleClick(UIMouseEvent evt)
        {
            base.RightDoubleClick(evt);
            SafeRightDoubleClick(evt);
        }

        public virtual void SafeMouseOver(UIMouseEvent evt) { }
        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            SafeMouseOver(evt);
        }

        public virtual void SafeMouseOut(UIMouseEvent evt) { }
        public override void MouseOut(UIMouseEvent evt)
        {
            base.MouseOut(evt);
            SafeMouseOut(evt);
        }

        public virtual void SafeUpdate(GameTime gameTime) { }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            SafeUpdate(gameTime);
        }

        public virtual void SafeScrollWheel(UIScrollWheelEvent evt) { }
        public override void ScrollWheel(UIScrollWheelEvent evt)
        {
            base.ScrollWheel(evt);
            SafeScrollWheel(evt);
        }
    }
}
