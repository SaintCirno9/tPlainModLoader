using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RecipeBrowser.UIElements;
using Terraria;
using Terraria.GameInput;
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
        private bool _middleWasDown;

        public RecipeBrowserTool()
            : base(typeof(RecipeBrowserUI))
        {
        }

        internal override void UIUpdate(GameTime gameTime)
        {
            base.UIUpdate(gameTime);
            TryDispatchMiddleClick();
        }

        /// <summary>
        /// 原版 UserInterface 只分发左右键；中键需自行落到按钮的 MiddleClick。
        /// </summary>
        private void TryDispatchMiddleClick()
        {
            if (userInterface?.CurrentState == null) return;

            bool down = Mouse.GetState().MiddleButton == ButtonState.Pressed;
            bool released = !down && _middleWasDown;
            _middleWasDown = down;
            if (!released) return;
            if (!Main.gameMenu && PlayerInput.IgnoreMouseInterface) return;

            Vector2 pos = Main.MouseScreen;
            UIElement target = userInterface.CurrentState.GetElementAt(pos);
            if (target == null) return;

            UIMouseEvent evt = new UIMouseEvent(target, pos);
            while (target != null)
            {
                if (target is UIHoverImageButton hover)
                {
                    hover.MiddleClick(evt);
                    break;
                }
                if (target is UISilentImageButton silent)
                {
                    silent.MiddleClick(evt);
                    break;
                }
                target = target.Parent;
            }
        }
    }
}
