using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Chat;

namespace RecipeBrowser.UIElements
{
    public class UISortableElement : UIElement
    {
        public int Order;

        public UISortableElement(int order)
        {
            Order = order;
        }

        public override int CompareTo(object obj)
        {
            if (obj is UISortableElement other)
            {
                return Order.CompareTo(other.Order);
            }
            return base.CompareTo(obj);
        }
    }

    public class UIHoverImageButton : UIElement
    {
        internal Texture2D texture;
        internal string hoverText;
        private float _visibilityActive = 1f;
        private float _visibilityInactive = 0.4f;

        public UIHoverImageButton(Texture2D texture, string hoverText)
        {
            this.texture = texture;
            this.hoverText = hoverText;
            if (texture != null)
            {
                Width.Set(texture.Width, 0f);
                Height.Set(texture.Height, 0f);
            }
        }

        public void SetImage(Texture2D tex)
        {
            texture = tex;
            if (texture != null)
            {
                Width.Set(texture.Width, 0f);
                Height.Set(texture.Height, 0f);
            }
        }

        public void SetVisibility(float whenActive, float whenInactive)
        {
            _visibilityActive = MathHelper.Clamp(whenActive, 0f, 1f);
            _visibilityInactive = MathHelper.Clamp(whenInactive, 0f, 1f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (texture == null) return;
            CalculatedStyle dimensions = GetDimensions();
            spriteBatch.Draw(texture, dimensions.Position(), Color.White * (IsMouseHovering ? _visibilityActive : _visibilityInactive));
            if (IsMouseHovering && !string.IsNullOrWhiteSpace(hoverText))
            {
                UICommon.TooltipMouseText(hoverText);
            }
        }

        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
    }

    public class UIHoverImageButtonMod : UIHoverImageButton
    {
        private Texture2D textureColorable;

        public UIHoverImageButtonMod(Texture2D texture, Texture2D textureColorable, string hoverText)
            : base(texture, hoverText)
        {
            this.textureColorable = textureColorable;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (RecipeBrowserUI.ModIndex != 0 && textureColorable != null)
            {
                CalculatedStyle dimensions = GetDimensions();
                Color disco = new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB);
                spriteBatch.Draw(textureColorable, dimensions.Position(), disco);
                if (IsMouseHovering && !string.IsNullOrWhiteSpace(hoverText))
                {
                    UICommon.TooltipMouseText(hoverText);
                }
            }
            else
            {
                base.DrawSelf(spriteBatch);
            }

            if ((IsMouseHovering || RecipeBrowserUI.modHoverIndex != -1) && texture != null)
            {
                CalculatedStyle innerDims = GetInnerDimensions();
                spriteBatch.Draw(texture, new Vector2(innerDims.X + innerDims.Width / 2 - 40, innerDims.Y - 80), Color.White);
            }
            RecipeBrowserUI.modHoverIndex = -1;
        }
    }

    public class UISilentImageButton : UIElement
    {
        public Texture2D texture;
        public bool selected;
        public string hoverText;
        public Color Color = Color.White;
        public event MouseEvent OnMiddleClick;
        private float _visibilityActive = 1f;
        private float _visibilityHovered = 0.95f;
        private float _visibilityInactive = 0.8f;

        public UISilentImageButton(Texture2D texture, string hoverText = null)
        {
            this.texture = texture;
            this.hoverText = hoverText;
            if (texture != null)
            {
                Width.Set(texture.Width, 0f);
                Height.Set(texture.Height, 0f);
            }
        }

        public void SetImage(Texture2D tex)
        {
            texture = tex;
            if (texture != null)
            {
                Width.Set(texture.Width, 0f);
                Height.Set(texture.Height, 0f);
            }
        }

        public void SetVisibility(float whenActive, float whenInactive)
        {
            _visibilityActive = MathHelper.Clamp(whenActive, 0f, 1f);
            _visibilityInactive = MathHelper.Clamp(whenInactive, 0f, 1f);
        }

        public override void MouseOver(UIMouseEvent evt)
        {
        }

        public virtual void MiddleClick(UIMouseEvent evt)
        {
            OnMiddleClick?.Invoke(evt, this);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (texture == null) return;
            CalculatedStyle dimensions = GetDimensions();
            if (selected)
            {
                var backTex = TextureAssets.InventoryBack14?.Value;
                if (backTex != null)
                {
                    spriteBatch.Draw(backTex, dimensions.ToRectangle(), Color.White);
                }
                else
                {
                    spriteBatch.Draw(TextureAssets.MagicPixel.Value, dimensions.ToRectangle(), Color.Goldenrod * 0.4f);
                }
            }
            spriteBatch.Draw(texture, dimensions.Position(), Color * (selected ? _visibilityActive : (IsMouseHovering ? _visibilityHovered : _visibilityInactive)));
            if (IsMouseHovering && !string.IsNullOrEmpty(hoverText))
            {
                UICommon.TooltipMouseText(hoverText);
            }
        }
    }

    public class UIBadgedSilentImageButton : UISilentImageButton
    {
        internal string badgeText;
        internal Color badgeColor = Color.White;

        public UIBadgedSilentImageButton(Texture2D texture, string badgeText) : base(texture)
        {
            this.badgeText = badgeText;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            if (!string.IsNullOrEmpty(badgeText))
            {
                CalculatedStyle dimensions = GetDimensions();
                Vector2 pos = new Vector2(dimensions.X + dimensions.Width - 10f, dimensions.Y + dimensions.Height - 12f);
                ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, badgeText, pos, badgeColor, 0f, Vector2.Zero, new Vector2(0.7f), -1f, 1f);
            }
        }
    }

    public class UIRadioButton : UICheckbox
    {
        internal UIRadioButtonGroup group;

        public UIRadioButton(string text, float textScale = 1f) : base(text, "")
        {
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            if (group != null)
            {
                group.Select(this);
            }
            else
            {
                Selected = true;
            }
        }
    }

    public class UIRadioButtonGroup : UIElement
    {
        private System.Collections.Generic.List<UIRadioButton> buttons = new System.Collections.Generic.List<UIRadioButton>();

        public void Add(UIRadioButton button)
        {
            buttons.Add(button);
            button.group = this;
            button.Top.Set(buttons.Count * 20f - 20f, 0f);
            Append(button);
            Height.Set(buttons.Count * 20f, 0f);
        }

        public void Select(UIRadioButton button)
        {
            foreach (var b in buttons)
            {
                b.Selected = (b == button);
            }
        }

        public void ButtonClicked(int index)
        {
            if (index >= 0 && index < buttons.Count)
            {
                Select(buttons[index]);
            }
        }
    }
}
