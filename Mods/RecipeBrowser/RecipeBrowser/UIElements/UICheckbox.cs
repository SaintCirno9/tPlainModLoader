using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace RecipeBrowser.UIElements
{
    public class UICheckbox : UIText
    {
        private bool selected;
        private bool disabled;
        internal string hoverText;

        public bool Selected
        {
            get => selected;
            set
            {
                if (value != selected)
                {
                    selected = value;
                    OnSelectedChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler OnSelectedChanged;

        public UICheckbox(string text, string hoverText, float textScale = 1f, bool large = false) : base(text, textScale, large)
        {
            Left.Pixels += 20f;
            text = "   " + text;
            this.hoverText = hoverText;
            SetText(text);
            OnLeftClick += (evt, el) =>
            {
                if (!disabled) Selected = !Selected;
            };
            Recalculate();
        }

        public void SetDisabled(bool disabled = true)
        {
            this.disabled = disabled;
            if (disabled) Selected = false;
            TextColor = disabled ? Color.Gray : Color.White;
        }

        public void SetHoverText(string hoverText)
        {
            this.hoverText = hoverText;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            CalculatedStyle innerDimensions = GetInnerDimensions();
            Vector2 pos = new Vector2(innerDimensions.X, innerDimensions.Y - 5f);

            var boxTex = RBTextures.Checkbox;
            var markTex = RBTextures.Checkmark;

            if (boxTex != null)
            {
                spriteBatch.Draw(boxTex, pos, null, disabled ? Color.Gray : Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            }
            if (Selected && markTex != null)
            {
                spriteBatch.Draw(markTex, pos, null, disabled ? Color.Gray : Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            }

            if (IsMouseHovering && !string.IsNullOrWhiteSpace(hoverText))
            {
                UICommon.DrawHoverStringInBounds(spriteBatch, hoverText);
            }
        }
    }

    public class UICycleImage : UIElement
    {
        private Texture2D texture;
        private int _drawWidth;
        private int _drawHeight;
        private int padding;
        private int textureOffsetX;
        private int textureOffsetY;
        private int states;
        internal string[] hoverTexts;
        private int currentState;

        public int CurrentState
        {
            get => currentState;
            set
            {
                if (value != currentState)
                {
                    currentState = value;
                    OnStateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler OnStateChanged;

        public UICycleImage(Texture2D texture, int states, string[] hoverTexts, int width, int height, int textureOffsetX = 0, int textureOffsetY = 0, int padding = 2)
        {
            this.texture = texture;
            _drawWidth = width;
            _drawHeight = height;
            this.textureOffsetX = textureOffsetX;
            this.textureOffsetY = textureOffsetY;
            Width.Set(width, 0f);
            Height.Set(height, 0f);
            this.states = states;
            this.padding = padding;
            this.hoverTexts = hoverTexts;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (texture == null) return;
            CalculatedStyle dimensions = GetDimensions();
            Point pt = new Point(textureOffsetX, textureOffsetY + (padding + _drawHeight) * currentState);
            Color color = IsMouseHovering ? Color.White : Color.Silver;
            spriteBatch.Draw(texture, new Rectangle((int)dimensions.X, (int)dimensions.Y, _drawWidth, _drawHeight), new Rectangle(pt.X, pt.Y, _drawWidth, _drawHeight), color);
            if (IsMouseHovering && hoverTexts != null && CurrentState < hoverTexts.Length)
            {
                UICommon.DrawHoverStringInBounds(spriteBatch, hoverTexts[CurrentState]);
            }
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            CurrentState = (currentState + 1) % states;
            base.LeftClick(evt);
        }

        public override void RightClick(UIMouseEvent evt)
        {
            CurrentState = (currentState + states - 1) % states;
            base.RightClick(evt);
        }
    }
}
