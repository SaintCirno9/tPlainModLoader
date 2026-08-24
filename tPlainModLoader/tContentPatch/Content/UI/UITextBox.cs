using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;

namespace tContentPatch.Content.UI
{
    /// <summary>
    /// 单行文本输入框控件
    /// 作者: SaintCirno9
    /// </summary>
    public class UITextBox : UIPanel
    {
        public Action<string> OnTextChanged = null;
        public Action OnLostFocus = null;
        public int Text_MaxLength = -1;

        private bool focus = false;
        public bool Focus
        {
            get => focus;
            set
            {
                if (focus == value) return;
                focus = value;
                if (!focus)
                {
                    PlayerInput.WritingText = false;
                    Main.instance.HandleIME();
                    OnLostFocus?.Invoke();
                }
                else
                {
                    PlayerInput.WritingText = true;
                    Main.instance.HandleIME();
                }
            }
        }

        private string text = null;
        public string Text
        {
            get => text;
            set => SetText(value);
        }

        private string textDefault = null;
        public string TextDefault
        {
            get => textDefault;
            set => textDefault = value ?? string.Empty;
        }

        private string text_old = null;
        private UIText ui_text = null;
        private int time1 = 0;
        private bool mouseLeftOld = false;

        public UITextBox(string text_default = "")
        {
            Text = string.Empty;
            text_old = Text;
            TextDefault = text_default;

            OverflowHidden = true;
            ui_text = new UIText(TextDefault);

            SetPadding(4);
            BackgroundColor = new Color(255, 255, 255, 240);
            BorderColor = Color.White;
            ui_text.ShadowColor = Color.Transparent;
            ui_text.VAlign = 0.5f;

            Append(ui_text);

            OnLeftClick += (evt, elem) =>
            {
                if (!Focus)
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    Focus = true;
                }
            };
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            ++time1;
            if (time1 * 16 > 2000) time1 = 0;

            UpdateFocus();

            if (Focus)
            {
                PlayerInput.WritingText = true;
                Main.CurrentInputTextTakerOverride = this;
                Main.instance.HandleIME();
            }

            string ui_text_text = null;
            if (Focus)
            {
                ui_text_text = Text;
                if (time1 * 16 > 1000) ui_text_text += "|";
                ui_text.TextColor = Color.Black;
            }
            else
            {
                if (string.IsNullOrEmpty(Text))
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

        protected virtual void UpdateFocus()
        {
            bool mouseLeftOld = this.mouseLeftOld;
            this.mouseLeftOld = Main.mouseLeft;

            if (Main.drawingPlayerChat ||
                Main.ingameOptionsWindow ||
                Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
            {
                Focus = false;
                return;
            }

            if (Main.mouseLeft && !mouseLeftOld)
            {
                Focus = IsMouseHovering;
            }
        }

        protected virtual void UpdateInput()
        {
            if (!Focus) return;

            PlayerInput.WritingText = true;
            Main.CurrentInputTextTakerOverride = this;
            Main.instance.HandleIME();

            CalculatedStyle size = GetDimensions();
            Vector2 imePos = new Vector2(size.X, size.Y + size.Height + 4);
            Main.instance.SetIMEPanelAnchor(imePos, 0f);
            DrawIME.NeedIME = true;
            DrawIME.IME_P = imePos;

            string s = Main.GetInputText(Text);

            if (Main.inputTextEnter)
            {
                Main.inputTextEnter = false;
                Focus = false;
            }
            else if (Main.inputTextEscape)
            {
                Main.inputTextEscape = false;
                Focus = false;
            }

            SetText(s);
        }

        protected virtual void OnDrawUpdateInput()
        {
            UpdateInput();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            OnDrawUpdateInput();
        }

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

        public void SetTextScale(float textScale)
        {
            ui_text.SetText(text, textScale, false);
        }
    }
}
