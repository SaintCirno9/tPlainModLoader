using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using ReLogic.Localization.IME;
using ReLogic.OS;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;

namespace tContentPatch.Content.UI
{
    /// <summary>
    /// 单行文本输入框控件（具备完整的 Windows IME 输入法支持、光标闪烁与防误失焦机制）
    /// 作者: SaintCirno9
    /// </summary>
    public class UITextBox : UIPanel
    {
        public Action<string> OnTextChanged = null;
        public Action OnLostFocus = null;
        public int Text_MaxLength = 50;
        public float TextScale { get; set; } = 0.8f;

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
                    Main.instance?.HandleIME();
                    if (Main.CurrentInputTextTakerOverride == this)
                    {
                        Main.CurrentInputTextTakerOverride = null;
                    }
                    OnLostFocus?.Invoke();
                }
                else
                {
                    PlayerInput.WritingText = true;
                    Main.CurrentInputTextTakerOverride = this;
                    Main.instance?.HandleIME();
                    Main.clrInput();
                }
            }
        }

        private string text = string.Empty;
        public string Text
        {
            get => text;
            set => SetText(value);
        }

        private string textDefault = string.Empty;
        public string TextDefault
        {
            get => textDefault;
            set => textDefault = value ?? string.Empty;
        }

        private string text_old = string.Empty;
        private int _frameCount = 0;
        private bool _mouseLeftOld = false;

        public UITextBox(string text_default = "")
        {
            text = string.Empty;
            text_old = string.Empty;
            TextDefault = text_default;

            OverflowHidden = true;
            SetPadding(4);
            BackgroundColor = new Color(255, 255, 255, 240);
            BorderColor = Color.White;
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            if (!Focus)
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                Focus = true;
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            bool mouseLeftJustPressed = Main.mouseLeft && !_mouseLeftOld;
            _mouseLeftOld = Main.mouseLeft;

            string composition = Focus ? Platform.Get<IImeService>()?.CompositionString : null;
            bool isComposing = !string.IsNullOrEmpty(composition);

            // 当玩家点击控件外部区域时失焦
            if (Focus && mouseLeftJustPressed)
            {
                CalculatedStyle dims = GetDimensions();
                Rectangle rect = new Rectangle((int)dims.X, (int)dims.Y, (int)dims.Width, (int)dims.Height);
                if (!rect.Contains(Main.MouseScreen.ToPoint()))
                {
                    Focus = false;
                }
            }

            if (Focus)
            {
                if (Main.drawingPlayerChat || Main.ingameOptionsWindow || (!isComposing && Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape)))
                {
                    Focus = false;
                    return;
                }

                PlayerInput.WritingText = true;
                Main.CurrentInputTextTakerOverride = this;
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            CalculatedStyle dims = GetDimensions();
            Vector2 drawPos = new Vector2(dims.X + 8, dims.Y + 4);
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            float scale = TextScale;

            if (Focus)
            {
                PlayerInput.WritingText = true;
                Main.instance?.HandleIME();

                Vector2 imeAnchor = new Vector2(dims.X, dims.Y + dims.Height + 4);
                Main.instance?.SetIMEPanelAnchor(imeAnchor, 0f);

                string composition = Platform.Get<IImeService>()?.CompositionString;
                bool isComposing = !string.IsNullOrEmpty(composition);

                string newText = Main.GetInputText(text);

                if (Main.inputTextEnter)
                {
                    Main.inputTextEnter = false;
                    if (!isComposing)
                    {
                        Focus = false;
                    }
                }
                else if (Main.inputTextEscape)
                {
                    Main.inputTextEscape = false;
                    if (!isComposing)
                    {
                        Focus = false;
                    }
                }

                SetText(newText);

                // 绘制当前输入的文本内容
                string displayContent = text ?? string.Empty;
                spriteBatch.DrawString(font, displayContent, drawPos, Color.Black, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                float currentWidth = font.MeasureString(displayContent).X * scale;

                // 若处于输入法拼音合成阶段，绘制内联拼音反馈
                if (isComposing)
                {
                    string compText = $"[{composition}]";
                    spriteBatch.DrawString(font, compText, new Vector2(drawPos.X + currentWidth, drawPos.Y), Main.imeCompositionStringColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                    currentWidth += font.MeasureString(compText).X * scale;
                }

                // 绘制闪烁光标
                _frameCount++;
                if ((_frameCount %= 40) <= 20)
                {
                    spriteBatch.DrawString(font, "|", new Vector2(drawPos.X + currentWidth, drawPos.Y), Color.Black, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(text))
                {
                    spriteBatch.DrawString(font, TextDefault ?? string.Empty, drawPos, Color.Gray, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
                else
                {
                    spriteBatch.DrawString(font, text, drawPos, Color.Black, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
            }
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            if (Focus)
            {
                Focus = false;
            }
        }

        public void SetTextScale(float textScale)
        {
            TextScale = textScale;
        }

        public void SetText(string s)
        {
            if (s == null) s = string.Empty;
            if (text_old == s) return;

            if (Text_MaxLength > 0 && s.Length > Text_MaxLength)
            {
                s = s.Substring(0, Math.Min(s.Length, Text_MaxLength));
            }

            if (text_old == s) return;

            text_old = text = s;
            OnTextChanged?.Invoke(text);
        }
    }
}
