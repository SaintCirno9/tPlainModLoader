using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace tContentPatch.Content.UI
{
    /// <summary/>
    public class UITextBox : UIPanel
    {
        /// <summary/>
        public Action<string> OnTextChanged = null;
        /// <summary/>
        public Action OnLostFocus = null;
        /// <summary/>
        public int Text_MaxLength = -1;

        private bool focus = false;
        /// <summary/>
        public bool Focus
        {
            get => focus;
            set
            {
                if (focus == value) return;
                focus = value;
                if (focus == false) OnLostFocus?.Invoke();
            }
        }
        private string text = null;
        /// <summary/>
        public string Text
        {
            get => text;
            set => SetText(value);
        }
        private string textDefault = null;
        /// <summary/>
        public string TextDefault
        {
            get => textDefault;
            set => textDefault = value ?? string.Empty;
        }

        private string text_old = null;
        private UIText ui_text = null;
        private int time1 = 0;
        private bool mouseLeftOld = false;

        /// <summary/>
        public UITextBox(string text_default = "")
        {
            Text = string.Empty;
            text_old = Text;
            TextDefault = text_default;

            OverflowHidden = true;
            ui_text = new UIText(TextDefault);

            SetPadding(2);
            BackgroundColor = Color.White;
            BorderColor = Color.White;
            ui_text.ShadowColor = BackgroundColor;
            ui_text.VAlign = 0.5f;

            Append(ui_text);
        }

        /// <inheritdoc/>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            ++time1;
            if (time1 * 16 > 2000) time1 = 0;

            UpdateFocus();

            string ui_text_text = null;
            if (Focus)
            {
                ui_text_text = Text;
                if (time1 * 16 > 1000) ui_text_text += "|";

                ui_text.TextColor = Color.Black;
            }
            else
            {
                if (Text == "")
                {
                    ui_text_text = TextDefault;
                    ui_text.TextColor = Color.Gray;
                }
                else
                {
                    ui_text_text = Text;
                    ui_text.TextColor = Color.Black;
                }
            }
            ui_text.SetText(ui_text_text);
        }

        /// <summary/>
        protected virtual void UpdateFocus()
        {
            bool mouseLeftOld = this.mouseLeftOld;
            this.mouseLeftOld = Main.mouseLeft;

            if (
                //Main.gamePaused ||
                Main.drawingPlayerChat ||
                Main.ingameOptionsWindow ||
                Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
            {
                Focus = false;
                return;
            }

            if (Main.mouseLeft && mouseLeftOld == false)
            {
                Focus = IsMouseHovering;
            }
        }

        /// <summary/>
        protected virtual void UpdateInput()
        {
            if (Focus == false) return;

            Terraria.GameInput.PlayerInput.WritingText = true;
            Main.instance.HandleIME();
            string s = Main.GetInputText(Text);

            Terraria.UI.CalculatedStyle size = GetDimensions();
            DrawIME.NeedIME = true;
            DrawIME.IME_P = new Vector2(size.X, size.Y + size.Height + 36);

            SetText(s);
        }

        /// <summary/>
        protected virtual void OnDrawUpdateInput()
        {
            UpdateInput();
        }

        /// <inheritdoc/>
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            OnDrawUpdateInput();
        }

        /// <summary/>
        public void SetText(string s)
        {
            if (s == null) s = string.Empty;
            if (text_old == s) return;

            if (Text_MaxLength > -1 && s.Length > Text_MaxLength)
            {
                s = s.Substring(0, Math.Min(s.Length, Text_MaxLength));
            }

            if (text_old == s) return;

            text_old = text = s;
            OnTextChanged?.Invoke(Text);
        }

        /// <summary/>
        public void SetTextScale(float textScale)
        {
            ui_text.SetText(text, textScale, false);
        }
    }
}
