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
    /// 通用单行文本输入框控件（具备完整的 Windows IME 输入法支持、光标闪烁、超长文本平滑视口滚动、防误失焦与回车/Esc快捷按键）
    /// 作者: SaintCirno9
    /// </summary>
    public class UITextBox : UIPanel
    {
        public Action<string> OnTextChanged = null;
        public Action OnLostFocus = null;
        public Action OnFocusGain = null;
        public Action<string> OnSubmit = null;
        public Action OnCancel = null;

        public int Text_MaxLength = 50;
        public float TextScale { get; set; } = 0.8f;
        public Color TextColor { get; set; } = Color.White;
        public Color HintColor { get; set; } = Color.Gray;
        public Color CursorColor { get; set; } = Color.White;

        public Color? FocusedBorderColor { get; set; } = null;
        public Color? UnfocusedBorderColor { get; set; } = null;

        public bool UnfocusOnEnter { get; set; } = true;
        public bool UnfocusOnEscape { get; set; } = true;

        private bool focus = false;
        private bool _justFocused = false;
        private bool _mouseLeftOld = false;
        private int _frameCount = 0;

        public bool Focus
        {
            get => focus;
            set
            {
                if (focus == value) return;
                focus = value;
                if (!focus)
                {
                    _justFocused = false;
                    PlayerInput.WritingText = false;
                    Main.blockInput = false;
                    Main.instance?.HandleIME();
                    if (Main.CurrentInputTextTakerOverride == this)
                    {
                        Main.CurrentInputTextTakerOverride = null;
                    }
                    if (UnfocusedBorderColor.HasValue)
                    {
                        BorderColor = UnfocusedBorderColor.Value;
                    }
                    OnLostFocus?.Invoke();
                }
                else
                {
                    _justFocused = true;
                    PlayerInput.WritingText = true;
                    Main.blockInput = true;
                    Main.CurrentInputTextTakerOverride = this;
                    Main.instance?.HandleIME();
                    Main.clrInput();
                    if (FocusedBorderColor.HasValue)
                    {
                        BorderColor = FocusedBorderColor.Value;
                    }
                    OnFocusGain?.Invoke();
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

        public UITextBox(string text_default = "")
        {
            text = string.Empty;
            text_old = string.Empty;
            TextDefault = text_default;

            OverflowHidden = true;
            SetPadding(4);
            BackgroundColor = new Color(20, 25, 45, 240);
            BorderColor = new Color(70, 100, 160);
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

            Vector2 mousePos = new Vector2(Main.mouseX, Main.mouseY);
            bool isMouseHovering = ContainsPoint(mousePos);

            // 当刚刚获得焦点时，等待当前鼠标左键抬起后才开始外部点击失焦判定
            if (_justFocused)
            {
                if (!Main.mouseLeft)
                {
                    _justFocused = false;
                }
            }
            else if (Focus)
            {
                // 玩家在外部区域点击左键时失焦
                if (Main.mouseLeft && !_mouseLeftOld && !isMouseHovering)
                {
                    Focus = false;
                }
            }

            _mouseLeftOld = Main.mouseLeft;

            if (Focus)
            {
                string composition = Platform.Get<IImeService>()?.CompositionString;
                bool isComposing = !string.IsNullOrEmpty(composition);

                if (Main.drawingPlayerChat || Main.ingameOptionsWindow || (!isComposing && Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape)))
                {
                    Focus = false;
                    return;
                }

                PlayerInput.WritingText = true;
                Main.blockInput = true;
                Main.CurrentInputTextTakerOverride = this;
                Main.instance?.HandleIME();

                string newText = Main.GetInputText(text);

                if (Main.inputTextEnter)
                {
                    Main.inputTextEnter = false;
                    if (!isComposing)
                    {
                        OnSubmit?.Invoke(text);
                        if (UnfocusOnEnter)
                        {
                            Focus = false;
                        }
                    }
                }
                else if (Main.inputTextEscape)
                {
                    Main.inputTextEscape = false;
                    if (!isComposing)
                    {
                        OnCancel?.Invoke();
                        if (UnfocusOnEscape)
                        {
                            Focus = false;
                        }
                    }
                }

                SetText(newText);
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            CalculatedStyle dims = GetDimensions();
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            float scale = TextScale;

            float paddingX = 8f;
            float paddingY = (dims.Height - font.LineSpacing * scale) / 2f;
            if (paddingY < 2f) paddingY = 2f;

            float visibleWidth = dims.Width - paddingX * 2f;

            if (Focus)
            {
                PlayerInput.WritingText = true;
                Main.blockInput = true;

                Vector2 imeAnchor = new Vector2(dims.X, dims.Y + dims.Height + 4);
                Main.instance?.SetIMEPanelAnchor(imeAnchor, 0f);

                string composition = Platform.Get<IImeService>()?.CompositionString;
                bool isComposing = !string.IsNullOrEmpty(composition);

                string displayContent = text ?? string.Empty;
                float textWidth = font.MeasureString(displayContent).X * scale;
                float compWidth = 0f;
                string compText = null;

                if (isComposing)
                {
                    compText = $"[{composition}]";
                    compWidth = font.MeasureString(compText).X * scale;
                }

                // 计算光标总横坐标及超长文本的水平视口平滑偏移
                float totalWidth = textWidth + compWidth;
                float scrollOffset = 0f;
                if (totalWidth > visibleWidth)
                {
                    scrollOffset = visibleWidth - totalWidth;
                }

                Vector2 textPos = new Vector2(dims.X + paddingX + scrollOffset, dims.Y + paddingY);

                // 绘制输入文本
                spriteBatch.DrawString(font, displayContent, textPos, TextColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                // 绘制拼音合成串
                if (isComposing)
                {
                    spriteBatch.DrawString(font, compText, new Vector2(textPos.X + textWidth, textPos.Y), Main.imeCompositionStringColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }

                // 绘制闪烁光标
                _frameCount++;
                if ((_frameCount %= 40) <= 20)
                {
                    spriteBatch.DrawString(font, "|", new Vector2(textPos.X + totalWidth, textPos.Y), CursorColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
            }
            else
            {
                Vector2 textPos = new Vector2(dims.X + paddingX, dims.Y + paddingY);
                if (string.IsNullOrEmpty(text))
                {
                    spriteBatch.DrawString(font, TextDefault ?? string.Empty, textPos, HintColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
                else
                {
                    float textWidth = font.MeasureString(text).X * scale;
                    float scrollOffset = 0f;
                    if (textWidth > visibleWidth)
                    {
                        scrollOffset = visibleWidth - textWidth;
                    }
                    spriteBatch.DrawString(font, text, new Vector2(textPos.X + scrollOffset, textPos.Y), TextColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
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
