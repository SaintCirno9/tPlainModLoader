using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

        /// <summary>客户端初始化（对齐原版 Tool.ClientInitialize，默认空实现）</summary>
        internal virtual void ClientInitialize()
        {
        }

        /// <summary>内容加载后回调（对齐原版 Tool.PostSetupContent，默认空实现）</summary>
        internal virtual void PostSetupContent()
        {
        }

        /// <summary>开关状态变化（对齐原版 Tool.Toggled，默认空实现）</summary>
        internal virtual void Toggled()
        {
        }

        /// <summary>绘制开关辅助（对齐原版 Tool.DrawUpdateToggle，默认空实现）</summary>
        internal virtual void DrawUpdateToggle(SpriteBatch spriteBatch, bool active)
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
