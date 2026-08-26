using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace RecipeBrowser.UIElements
{
    public class FixedUIScrollbar : UIScrollbar
    {
        internal UserInterface userInterface;

        public FixedUIScrollbar(UserInterface userInterface)
        {
            this.userInterface = userInterface;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            UserInterface activeInstance = UserInterface.ActiveInstance;
            UserInterface.ActiveInstance = userInterface;
            base.DrawSelf(spriteBatch);
            UserInterface.ActiveInstance = activeInstance;
        }

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            UserInterface activeInstance = UserInterface.ActiveInstance;
            UserInterface.ActiveInstance = userInterface;
            base.LeftMouseDown(evt);
            UserInterface.ActiveInstance = activeInstance;
        }
    }

    public class InvisibleFixedUIScrollbar : FixedUIScrollbar
    {
        public InvisibleFixedUIScrollbar(UserInterface userInterface) : base(userInterface)
        {
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
        }

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            UserInterface activeInstance = UserInterface.ActiveInstance;
            UserInterface.ActiveInstance = userInterface;
            base.LeftMouseDown(evt);
            UserInterface.ActiveInstance = activeInstance;
        }
    }

    public class FixedUIHorizontalScrollbar : UIHorizontalScrollbar
    {
        internal UserInterface userInterface;

        public FixedUIHorizontalScrollbar(UserInterface userInterface)
        {
            this.userInterface = userInterface;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            UserInterface activeInstance = UserInterface.ActiveInstance;
            UserInterface.ActiveInstance = userInterface;
            base.DrawSelf(spriteBatch);
            UserInterface.ActiveInstance = activeInstance;
        }

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            UserInterface activeInstance = UserInterface.ActiveInstance;
            UserInterface.ActiveInstance = userInterface;
            base.LeftMouseDown(evt);
            UserInterface.ActiveInstance = activeInstance;
        }
    }

    public class InvisibleFixedUIHorizontalScrollbar : FixedUIHorizontalScrollbar
    {
        public InvisibleFixedUIHorizontalScrollbar(UserInterface userInterface) : base(userInterface)
        {
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
        }

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            UserInterface activeInstance = UserInterface.ActiveInstance;
            UserInterface.ActiveInstance = userInterface;
            base.LeftMouseDown(evt);
            UserInterface.ActiveInstance = activeInstance;
        }
    }
}
