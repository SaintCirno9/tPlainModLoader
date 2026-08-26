using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Chat;

namespace RecipeBrowser.UIElements
{
    public class UIBottomlessPanel : UIPanel
    {
        private static int CORNER_SIZE = 12;
        private static int BAR_SIZE = 4;
        private static ReLogic.Content.Asset<Texture2D> _bottomlessBorderTexture;
        private static ReLogic.Content.Asset<Texture2D> _bottomlessBgTexture;

        public UIBottomlessPanel()
        {
            if (_bottomlessBorderTexture == null)
            {
                _bottomlessBorderTexture = Main.Assets.Request<Texture2D>("Images/UI/PanelBorder", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }
            if (_bottomlessBgTexture == null)
            {
                _bottomlessBgTexture = Main.Assets.Request<Texture2D>("Images/UI/PanelBackground", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }
            SetPadding(CORNER_SIZE);
        }

        public new void DrawPanel(SpriteBatch spriteBatch, Texture2D texture, Color color)
        {
            if (texture == null) return;
            CalculatedStyle dimensions = GetDimensions();
            Point val = new Point((int)dimensions.X, (int)dimensions.Y);
            Point val2 = new Point(val.X + (int)dimensions.Width - CORNER_SIZE, val.Y + (int)dimensions.Height);
            int width = val2.X - val.X - CORNER_SIZE;
            int height = val2.Y - val.Y - CORNER_SIZE;

            spriteBatch.Draw(texture, new Rectangle(val.X, val.Y, CORNER_SIZE, CORNER_SIZE), new Rectangle(0, 0, CORNER_SIZE, CORNER_SIZE), color);
            spriteBatch.Draw(texture, new Rectangle(val2.X, val.Y, CORNER_SIZE, CORNER_SIZE), new Rectangle(CORNER_SIZE + BAR_SIZE, 0, CORNER_SIZE, CORNER_SIZE), color);
            spriteBatch.Draw(texture, new Rectangle(val.X + CORNER_SIZE, val.Y, width, CORNER_SIZE), new Rectangle(CORNER_SIZE, 0, BAR_SIZE, CORNER_SIZE), color);
            spriteBatch.Draw(texture, new Rectangle(val.X, val.Y + CORNER_SIZE, CORNER_SIZE, height), new Rectangle(0, CORNER_SIZE, CORNER_SIZE, BAR_SIZE), color);
            spriteBatch.Draw(texture, new Rectangle(val2.X, val.Y + CORNER_SIZE, CORNER_SIZE, height), new Rectangle(CORNER_SIZE + BAR_SIZE, CORNER_SIZE, CORNER_SIZE, BAR_SIZE), color);
            spriteBatch.Draw(texture, new Rectangle(val.X + CORNER_SIZE, val.Y + CORNER_SIZE, width, height), new Rectangle(CORNER_SIZE, CORNER_SIZE, BAR_SIZE, BAR_SIZE), color);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (_bottomlessBgTexture != null)
            {
                DrawPanel(spriteBatch, _bottomlessBgTexture.Value, BackgroundColor);
            }
            if (_bottomlessBorderTexture != null)
            {
                DrawPanel(spriteBatch, _bottomlessBorderTexture.Value, BorderColor);
            }
        }
    }

    public class UIJourneyDuplicateButton : UIElement
    {
        private CraftPath.JourneyDuplicateItemNode duplicationNode;

        public UIJourneyDuplicateButton(CraftPath.JourneyDuplicateItemNode duplicationNode)
        {
            this.duplicationNode = duplicationNode;
            var onTex = RBTextures.DuplicateOn;
            Width.Set(onTex != null ? onTex.Width : 20f, 0f);
            Height.Set(onTex != null ? onTex.Height : 20f, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            bool flag = AbleToDuplicate();
            CalculatedStyle dimensions = GetDimensions();
            var tex = (IsMouseHovering && flag) ? RBTextures.DuplicateOn : RBTextures.DuplicateOff;
            if (tex != null)
            {
                spriteBatch.Draw(tex, dimensions.Position(), null, Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            }
            if (IsMouseHovering && flag)
            {
                UICommon.DrawHoverStringInBounds(spriteBatch, RBLanguage.GetText("CraftUI", "Duplicate"));
            }
        }

        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            if (AbleToDuplicate())
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
            }
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            if (!AbleToDuplicate() || duplicationNode == null) return;

            int remaining = duplicationNode.stack;
            while (remaining > 0)
            {
                Item item = ContentSamples.ItemsByType[duplicationNode.itemid].Clone();
                int take = Math.Min(remaining, item.maxStack);
                Main.LocalPlayer.QuickSpawnItem(null, item.type, take);
                SoundEngine.PlaySound(SoundID.MenuTick);
                remaining -= take;
            }
        }

        private bool AbleToDuplicate()
        {
            return duplicationNode != null && Main.GameMode == 3 && RecipePath.ItemFullyResearched(duplicationNode.itemid);
        }
    }

    public class BlockInputElement : UIElement
    {
        private UIElement elementToBlock;
        private int top;

        public BlockInputElement(UIElement elementToBlock, int top)
        {
            Width.Set(0f, 1f);
            Height.Set(0f, 1f);
            this.top = top;
            this.elementToBlock = elementToBlock;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (elementToBlock == null) return;
            CalculatedStyle dimensions = elementToBlock.GetDimensions();
            Rectangle rect = dimensions.ToRectangle();
            rect.Y += top;
            rect.Height -= top;
            Utils.DrawInvBG(spriteBatch, rect, Color.Black * 0.5f);
        }
    }

    public static class UIElementHelpers
    {
        internal static void GatherElementsByType(UIElement parent, Type type, List<UIElement> outList)
        {
            if (parent == null || type == null || outList == null) return;
            foreach (UIElement child in parent.Children)
            {
                if (child.GetType() == type)
                {
                    outList.Add(child);
                }
                GatherElementsByType(child, type, outList);
            }
        }

        internal static T FindChildOfType<T>(UIElement parent, Func<T, bool> predicate = null) where T : UIElement
        {
            if (parent == null) return null;
            foreach (UIElement child in parent.Children)
            {
                if (child is T match && (predicate == null || predicate(match)))
                {
                    return match;
                }
                T nested = FindChildOfType(child, predicate);
                if (nested != null) return nested;
            }
            return null;
        }
    }

    public class UITextSnippet : UIElement
    {
        private object _text = "";
        private float _textScale = 1f;
        private Vector2 _textSize = Vector2.Zero;
        private bool _isLarge;
        private Color _color = Color.White;

        public string Text => _text?.ToString() ?? "";
        public Color TextColor { get => _color; set => _color = value; }
        public string HoverText { get; set; }

        public UITextSnippet(string text, float textScale = 1f, bool large = false)
        {
            InternalSetText(text, textScale, large);
        }

        public UITextSnippet(LocalizedText text, float textScale = 1f, bool large = false)
        {
            InternalSetText(text, textScale, large);
        }

        public override void Recalculate()
        {
            InternalSetText(_text, _textScale, _isLarge);
            base.Recalculate();
        }

        public void SetText(string text) => InternalSetText(text, _textScale, _isLarge);
        public void SetText(LocalizedText text) => InternalSetText(text, _textScale, _isLarge);

        private void InternalSetText(object text, float textScale, bool large)
        {
            _text = text ?? "";
            _textScale = textScale;
            _isLarge = large;
            var font = (large ? FontAssets.DeathText : FontAssets.MouseText).Value;
            _textSize = ChatManager.GetStringSize(font, Text, new Vector2(textScale), -1f);
            MinWidth.Set(_textSize.X + PaddingLeft + PaddingRight, 0f);
            MinHeight.Set(_textSize.Y + PaddingTop + PaddingBottom, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            CalculatedStyle innerDimensions = GetInnerDimensions();
            Vector2 pos = innerDimensions.Position();
            if (_isLarge) pos.Y -= 10f * _textScale;
            else pos.Y -= 2f * _textScale;
            pos.X += (innerDimensions.Width - _textSize.X) * 0.5f;

            if (IsMouseHovering && !string.IsNullOrEmpty(HoverText))
            {
                Main.hoverItemName = HoverText;
            }

            var font = (_isLarge ? FontAssets.DeathText : FontAssets.MouseText).Value;
            int hoveredSnippet = -1;
            List<TextSnippet> snippetList = ChatManager.ParseMessage(Text, Color.White);
            ChatManager.ConvertNormalSnippets(snippetList);
            TextSnippet[] array = snippetList.ToArray();
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, array, pos, 0f, Vector2.Zero, new Vector2(_textScale), out hoveredSnippet, -1f, 2f);

            if (hoveredSnippet > -1)
            {
                array[hoveredSnippet].OnHover();
                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    array[hoveredSnippet].OnClick();
                }
            }
        }
    }
}
