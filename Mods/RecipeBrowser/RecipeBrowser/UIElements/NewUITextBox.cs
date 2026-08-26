using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RecipeBrowser.Common;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.UI;

namespace RecipeBrowser.UIElements
{
    public class NewUITextBox : UIPanel
    {
        internal bool focused;
        private int _maxLength = 60;
        private string hintText;
        internal string currentString = "";
        private int textBlinkerCount;
        private int textBlinkerState;
        internal bool unfocusOnEnter = true;
        internal bool unfocusOnTab = true;

        public event Action OnFocus;
        public event Action OnUnfocus;
        public event Action OnTextChanged;
        public event Action OnTabPressed;
        public event Action OnEnterPressed;

        public NewUITextBox(string hintText, string text = "")
        {
            this.hintText = hintText;
            currentString = text ?? "";
            SetPadding(0f);
            BackgroundColor = Color.White;
            BorderColor = Color.White;

            var closeTex = RBTextures.CloseButton ?? TextureAssets.MagicPixel.Value;
            UIHoverImageButton closeBtn = new UIHoverImageButton(closeTex, "");
            closeBtn.OnLeftClick += (evt, el) => SetText("");
            closeBtn.Left.Set(-20f, 1f);
            closeBtn.VAlign = 0.5f;
            Append(closeBtn);
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            Focus();
            base.LeftClick(evt);
        }

        public override void RightClick(UIMouseEvent evt)
        {
            base.RightClick(evt);
            SetText("");
        }

        public void SetUnfocusKeys(bool unfocusOnEnter, bool unfocusOnTab)
        {
            this.unfocusOnEnter = unfocusOnEnter;
            this.unfocusOnTab = unfocusOnTab;
        }

        public void Unfocus()
        {
            if (focused)
            {
                focused = false;
                Main.blockInput = false;
                OnUnfocus?.Invoke();
            }
        }

        public void Focus()
        {
            if (!focused)
            {
                Main.clrInput();
                focused = true;
                Main.blockInput = true;
                OnFocus?.Invoke();
            }
        }

        public override void Update(GameTime gameTime)
        {
            Vector2 mousePos = new Vector2(Main.mouseX, Main.mouseY);
            if (!ContainsPoint(mousePos) && (Main.mouseLeft || Main.mouseRight))
            {
                Unfocus();
            }
            base.Update(gameTime);
        }

        public void SetText(string text)
        {
            if (text == null) text = "";
            if (text.Length > _maxLength)
            {
                text = text.Substring(0, _maxLength);
            }
            if (currentString != text)
            {
                currentString = text;
                OnTextChanged?.Invoke();
            }
        }

        public void SetTextMaxLength(int maxLength)
        {
            _maxLength = maxLength;
        }

        private static bool JustPressed(Keys key)
        {
            if (Main.inputText.IsKeyDown(key))
            {
                return !Main.oldInputText.IsKeyDown(key);
            }
            return false;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            if (focused)
            {
                PlayerInput.WritingText = true;
                Main.instance.HandleIME();
                string inputText = Main.GetInputText(currentString, false);
                if (!inputText.Equals(currentString))
                {
                    currentString = inputText;
                    OnTextChanged?.Invoke();
                }

                if (JustPressed(Keys.Tab))
                {
                    if (unfocusOnTab) Unfocus();
                    OnTabPressed?.Invoke();
                }
                if (JustPressed(Keys.Enter))
                {
                    Main.drawingPlayerChat = false;
                    if (unfocusOnEnter) Unfocus();
                    OnEnterPressed?.Invoke();
                }
                if (++textBlinkerCount >= 20)
                {
                    textBlinkerState = (textBlinkerState + 1) % 2;
                    textBlinkerCount = 0;
                }
            }

            string text = currentString;
            if (textBlinkerState == 1 && focused)
            {
                text += "|";
            }

            CalculatedStyle dimensions = GetDimensions();
            Color color = Color.Black;
            Vector2 pos = dimensions.Position() + new Vector2(4f, 2f);

            if (currentString.Length == 0 && !focused)
            {
                color *= 0.5f;
                DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, FontAssets.MouseText.Value, hintText, pos, color);
            }
            else
            {
                DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, FontAssets.MouseText.Value, text, pos, color);
            }
        }
    }

    public class PrettyUITextBox : UIElement
    {
    }
}
