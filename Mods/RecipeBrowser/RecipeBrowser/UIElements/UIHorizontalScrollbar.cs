using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace RecipeBrowser.UIElements
{
    public class UIHorizontalScrollbar : UIElement
    {
        private float _viewPosition;
        private float _viewSize = 1f;
        private float _maxViewSize = 20f;
        private bool _isDragging;
        private bool _isHoveringOverHandle;
        private float _dragXOffset;

        public float ViewPosition
        {
            get => _viewPosition;
            set => _viewPosition = MathHelper.Clamp(value, 0f, _maxViewSize - _viewSize);
        }

        public UIHorizontalScrollbar()
        {
            Height.Set(20f, 0f);
            MaxHeight.Set(20f, 0f);
            PaddingLeft = 5f;
            PaddingRight = 5f;
        }

        public void SetView(float viewSize, float maxViewSize)
        {
            viewSize = MathHelper.Clamp(viewSize, 0f, maxViewSize);
            _viewPosition = MathHelper.Clamp(_viewPosition, 0f, maxViewSize - viewSize);
            _viewSize = viewSize;
            _maxViewSize = maxViewSize;
        }

        public float GetValue()
        {
            return _viewPosition;
        }

        private Rectangle GetHandleRectangle()
        {
            CalculatedStyle innerDimensions = GetInnerDimensions();
            if (_maxViewSize == 0f && _viewSize == 0f)
            {
                _viewSize = 1f;
                _maxViewSize = 1f;
            }
            return new Rectangle((int)(innerDimensions.X + innerDimensions.Width * (_viewPosition / _maxViewSize)) - 3, (int)innerDimensions.Y, (int)(innerDimensions.Width * (_viewSize / _maxViewSize)) + 7, 20);
        }

        private void DrawBar(SpriteBatch spriteBatch, Texture2D texture, Rectangle dimensions, Color color)
        {
            if (texture == null) return;
            if (dimensions.Width <= 12)
            {
                spriteBatch.Draw(texture, dimensions, color);
                return;
            }
            spriteBatch.Draw(texture, new Rectangle(dimensions.X - 6, dimensions.Y, 6, dimensions.Height), new Rectangle(0, 0, 6, texture.Height), color);
            spriteBatch.Draw(texture, new Rectangle(dimensions.X, dimensions.Y, dimensions.Width, dimensions.Height), new Rectangle(6, 0, 4, texture.Height), color);
            spriteBatch.Draw(texture, new Rectangle(dimensions.X + dimensions.Width, dimensions.Y, 6, dimensions.Height), new Rectangle(texture.Width - 6, 0, 6, texture.Height), color);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();
            CalculatedStyle innerDimensions = GetInnerDimensions();
            if (_isDragging)
            {
                float num = UserInterface.ActiveInstance.MousePosition.X - innerDimensions.X - _dragXOffset;
                _viewPosition = MathHelper.Clamp(num / innerDimensions.Width * _maxViewSize, 0f, _maxViewSize - _viewSize);
            }
            Rectangle handleRectangle = GetHandleRectangle();
            Vector2 mousePosition = UserInterface.ActiveInstance.MousePosition;
            bool wasHovering = _isHoveringOverHandle;
            _isHoveringOverHandle = handleRectangle.Contains(new Point((int)mousePosition.X, (int)mousePosition.Y));
            if (!wasHovering && _isHoveringOverHandle && Main.instance.IsActive)
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
            }

            var barTex = RBTextures.GetTexture("UIElements/ScrollbarHorizontal") ?? TextureAssets.MagicPixel.Value;
            var handleTex = RBTextures.GetTexture("UIElements/ScrollbarInnerHorizontal") ?? TextureAssets.MagicPixel.Value;

            DrawBar(spriteBatch, barTex, dimensions.ToRectangle(), Color.White);
            DrawBar(spriteBatch, handleTex, handleRectangle, Color.White * ((_isDragging || _isHoveringOverHandle) ? 1f : 0.85f));
        }

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            base.LeftMouseDown(evt);
            if (evt.Target == this)
            {
                Rectangle handleRectangle = GetHandleRectangle();
                if (handleRectangle.Contains(new Point((int)evt.MousePosition.X, (int)evt.MousePosition.Y)))
                {
                    _isDragging = true;
                    _dragXOffset = evt.MousePosition.X - handleRectangle.X;
                }
                else
                {
                    CalculatedStyle innerDimensions = GetInnerDimensions();
                    float num = UserInterface.ActiveInstance.MousePosition.X - innerDimensions.X - (handleRectangle.Width >> 1);
                    _viewPosition = MathHelper.Clamp(num / innerDimensions.Width * _maxViewSize, 0f, _maxViewSize - _viewSize);
                }
            }
        }

        public override void LeftMouseUp(UIMouseEvent evt)
        {
            base.LeftMouseUp(evt);
            _isDragging = false;
        }
    }
}
