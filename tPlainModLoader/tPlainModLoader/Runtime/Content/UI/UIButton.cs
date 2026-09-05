using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace TPML.UI
{
    /// <summary>
    /// 标准文本面板按钮，提供原版风格边框、悬停高亮色彩与清脆点击音效
    /// </summary>
    public class UIButton : UITextPanel<string>
    {
        /// <summary>
        /// 为 <see langword="false"/> 时禁用按钮，阻止点击与交互
        /// </summary>
        public bool isEnable = true;

        /// <summary>
        /// 为 <see langword="false"/> 时隐藏按钮，阻止绘制与交互命中
        /// </summary>
        public bool isDraw = true;

        /// <summary>启用状态下的背景色</summary>
        public Color EnableColorBack = new Color(63, 82, 151) * 0.8f;

        /// <summary>启用状态下的边框色</summary>
        public Color EnableColorBorder = Color.Black;

        /// <summary>禁用状态下的背景色</summary>
        public Color NoEnableColorBack = Color.Gray * 0.8f;

        /// <summary>禁用状态下的边框色</summary>
        public Color NoEnableColorBorder = Color.Black;

        /// <summary>鼠标悬停时的背景色</summary>
        public Color MouseOverColorBack = new Color(73, 94, 171);

        /// <summary>鼠标悬停时的边框色</summary>
        public Color MouseOverColorBorder = Colors.FancyUIFatButtonMouseOver;

        public UIButton(string text, float textScale = 1f, bool large = false) : base(text, textScale, large)
        {
            OnMouseOver += FadedMouseOver;
        }

        public override bool ContainsPoint(Vector2 point)
        {
            if (!isDraw) return false;
            return base.ContainsPoint(point);
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            if (isEnable && isDraw) base.LeftClick(evt);
        }

        public override void LeftDoubleClick(UIMouseEvent evt)
        {
            if (isEnable && isDraw) base.LeftDoubleClick(evt);
        }

        public override void RightClick(UIMouseEvent evt)
        {
            if (isEnable && isDraw) base.RightClick(evt);
        }

        public override void RightDoubleClick(UIMouseEvent evt)
        {
            if (isEnable && isDraw) base.RightDoubleClick(evt);
        }

        public override void Update(GameTime gameTime)
        {
            if (!isDraw) return;
            base.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!isDraw) return;
            base.Draw(spriteBatch);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (!isDraw) return;

            if (isEnable)
            {
                BackgroundColor = IsMouseHovering ? MouseOverColorBack : EnableColorBack;
                BorderColor = IsMouseHovering ? MouseOverColorBorder : EnableColorBorder;
            }
            else
            {
                BackgroundColor = NoEnableColorBack;
                BorderColor = NoEnableColorBorder;
            }

            base.DrawSelf(spriteBatch);
        }

        private void FadedMouseOver(UIMouseEvent evt, UIElement listeningElement)
        {
            if (!isEnable || !isDraw) return;
            SoundEngine.PlaySound(12);
        }
    }
}
