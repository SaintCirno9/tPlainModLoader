using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;

namespace RecipeBrowser
{
    public class UIModState : UIState
    {
        internal UserInterface userInterface;
        private UIElement blockInputElement;

        public UIModState(UserInterface userInterface)
        {
            this.userInterface = userInterface;
        }

        public bool IsInputBlocked => blockInputElement != null && blockInputElement.Parent != null;

        public void BlockInput(UIElement dialog)
        {
            if (blockInputElement != null)
            {
                RemoveChild(blockInputElement);
                blockInputElement = null;
            }

            blockInputElement = new UIElement();
            blockInputElement.Width.Set(0f, 1f);
            blockInputElement.Height.Set(0f, 1f);
            blockInputElement.OnLeftClick += (evt, el) =>
            {
                if (evt.Target == blockInputElement)
                {
                    UnblockInput();
                }
            };
            blockInputElement.OnRightClick += (evt, el) =>
            {
                if (evt.Target == blockInputElement)
                {
                    UnblockInput();
                }
            };

            blockInputElement.Append(dialog);
            Append(blockInputElement);
        }

        public void UnblockInput()
        {
            if (blockInputElement != null)
            {
                RemoveChild(blockInputElement);
                blockInputElement = null;
            }
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
