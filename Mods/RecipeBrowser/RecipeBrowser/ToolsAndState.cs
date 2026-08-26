using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;

namespace RecipeBrowser
{
    public class UIModState : UIState
    {
        internal UserInterface userInterface;

        public UIModState(UserInterface userInterface)
        {
            this.userInterface = userInterface;
        }

        public void ReverseChildren()
        {
            Elements.Reverse();
        }
    }

    public abstract class Tool
    {
        internal UserInterface userInterface;
        internal UIModState uistate;

        public Tool(Type uistateType)
        {
            userInterface = new UserInterface();
            uistate = (UIModState)Activator.CreateInstance(uistateType, userInterface);
            uistate.Activate();
            userInterface.SetState(uistate);
        }

        internal virtual void Initialize()
        {
        }

        internal virtual void ScreenResolutionChanged()
        {
            userInterface?.Recalculate();
        }

        internal virtual void UIUpdate(GameTime gameTime)
        {
            userInterface?.Update(gameTime);
        }

        internal virtual void UIDraw()
        {
            if (uistate != null)
            {
                uistate.ReverseChildren();
                uistate.Draw(Main.spriteBatch);
                uistate.ReverseChildren();
            }
        }
    }

    public class RecipeBrowserTool : Tool
    {
        public RecipeBrowserTool()
            : base(typeof(RecipeBrowserUI))
        {
        }
    }
}
