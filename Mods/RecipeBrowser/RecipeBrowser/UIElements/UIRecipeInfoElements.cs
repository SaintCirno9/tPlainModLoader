using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Chat;

namespace RecipeBrowser.UIElements
{
    public class UIRecipeInfo : UIElement
    {
        internal UIHorizontalGrid craftingIngredientsGrid;
        internal UIPanel ingredientsPanel;

        public UIRecipeInfo()
        {
            ingredientsPanel = new UIPanel();
            ingredientsPanel.SetPadding(6f);
            ingredientsPanel.Top.Set(0f, 0f);
            ingredientsPanel.Left.Set(180f, 0f);
            ingredientsPanel.Width.Set(-182f, 1f);
            ingredientsPanel.Height.Set(50f, 0f);
            ingredientsPanel.BackgroundColor = Color.CornflowerBlue * 0.3f;
            Append(ingredientsPanel);

            craftingIngredientsGrid = new UIHorizontalGrid();
            craftingIngredientsGrid.Width.Set(0f, 1f);
            craftingIngredientsGrid.Height.Set(0f, 1f);
            craftingIngredientsGrid.ListPadding = 2f;
            craftingIngredientsGrid.drawArrows = true;
            ingredientsPanel.Append(craftingIngredientsGrid);

            InvisibleFixedUIHorizontalScrollbar scrollbar = new InvisibleFixedUIHorizontalScrollbar(RecipeBrowserUI.instance.userInterface);
            scrollbar.SetView(100f, 1000f);
            scrollbar.Width.Set(0f, 1f);
            scrollbar.Top.Set(-20f, 1f);
            craftingIngredientsGrid.SetScrollbar(scrollbar);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (RecipeCatalogueUI.instance == null || RecipeCatalogueUI.instance.selectedIndex < 0 || RecipeCatalogueUI.instance.selectedIndex >= Recipe.numRecipes)
            {
                return;
            }

            Recipe recipe = Main.recipe[RecipeCatalogueUI.instance.selectedIndex];
            if (recipe == null) return;

            CalculatedStyle innerDimensions = GetInnerDimensions();
            float x = innerDimensions.X;
            float y = innerDimensions.Y;

            string headerText = Language.GetTextValue("LegacyInterface.22");
            if (string.IsNullOrEmpty(headerText) || headerText == "LegacyInterface.22") headerText = "制作站";
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, headerText, new Vector2(x, y), Utilities.textColor, 0f, Vector2.Zero, Vector2.One, -1f, 2f);

            StringBuilder sbTooltip = new StringBuilder();
            StringBuilder sbDisplay = new StringBuilder();

            bool hasPrevious = false;
            Dictionary<string, int> tileNameMap = new Dictionary<string, int>();

            if (recipe.requiredTile < 0 && !recipe.needWater && !recipe.needHoney && !recipe.needLava && !recipe.needSnowBiome && !recipe.needGraveyardBiome)
            {
                string byHand = Language.GetTextValue("LegacyInterface.23");
                if (string.IsNullOrEmpty(byHand) || byHand == "LegacyInterface.23") byHand = "徒手";
                DoChatTag(sbTooltip, hasPrevious, true, byHand);
                DoChatTag(sbDisplay, hasPrevious, true, byHand);
                hasPrevious = true;
            }
            else
            {
                if (recipe.requiredTile >= 0)
                {
                    string tName = Utilities.GetTileName(recipe.requiredTile);
                    bool state = (recipe.requiredTile < Main.LocalPlayer.adjTile.Length && Main.LocalPlayer.adjTile[recipe.requiredTile]);
                    DoChatTag(sbTooltip, hasPrevious, state, tName);
                    DoChatTag(sbDisplay, hasPrevious, state, tName);
                    tileNameMap[tName] = recipe.requiredTile;
                    hasPrevious = true;
                }

                if (recipe.needWater)
                {
                    string water = Language.GetTextValue("LegacyInterface.53");
                    if (string.IsNullOrEmpty(water) || water == "LegacyInterface.53") water = "水";
                    bool state = Main.LocalPlayer.adjWaterSource;
                    DoChatTag(sbTooltip, hasPrevious, state, water);
                    DoChatTag(sbDisplay, hasPrevious, state, water);
                    hasPrevious = true;
                }
                if (recipe.needHoney)
                {
                    string honey = Language.GetTextValue("LegacyInterface.58");
                    if (string.IsNullOrEmpty(honey) || honey == "LegacyInterface.58") honey = "蜂蜜";
                    bool state = Main.LocalPlayer.adjHoney;
                    DoChatTag(sbTooltip, hasPrevious, state, honey);
                    DoChatTag(sbDisplay, hasPrevious, state, honey);
                    hasPrevious = true;
                }
                if (recipe.needLava)
                {
                    string lava = Language.GetTextValue("LegacyInterface.56");
                    if (string.IsNullOrEmpty(lava) || lava == "LegacyInterface.56") lava = "熔岩";
                    bool state = Main.LocalPlayer.adjLava;
                    DoChatTag(sbTooltip, hasPrevious, state, lava);
                    DoChatTag(sbDisplay, hasPrevious, state, lava);
                    hasPrevious = true;
                }
                if (recipe.needSnowBiome)
                {
                    string snow = "雪原";
                    bool state = Main.LocalPlayer.ZoneSnow;
                    DoChatTag(sbTooltip, hasPrevious, state, snow);
                    DoChatTag(sbDisplay, hasPrevious, state, snow);
                    hasPrevious = true;
                }
                if (recipe.needGraveyardBiome)
                {
                    string grave = "墓地";
                    bool state = Main.LocalPlayer.ZoneGraveyard;
                    DoChatTag(sbTooltip, hasPrevious, state, grave);
                    DoChatTag(sbDisplay, hasPrevious, state, grave);
                    hasPrevious = true;
                }
            }

            TextSnippet[] snippets = ChatManager.ParseMessage(sbDisplay.ToString(), Color.White).ToArray();
            int hoveredSnippet = -1;
            float textWidth = ChatManager.GetStringSize(FontAssets.MouseText.Value, sbDisplay.ToString(), Vector2.One, -1f).X;
            Vector2 textScale = (textWidth > 170f) ? new Vector2(170f / textWidth) : Vector2.One;

            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, snippets, new Vector2(x, y + 24f), Color.White, 0f, Vector2.Zero, textScale, out hoveredSnippet, -1f, 2f);

            CalculatedStyle dims = GetDimensions();
            Rectangle area = new Rectangle((int)dims.X, (int)dims.Y, 180, (int)dims.Height);
            if (area.Contains(Main.mouseX, Main.mouseY) && textWidth > 170f)
            {
                UICommon.TooltipMouseText(sbTooltip.ToString());
            }

            if (hoveredSnippet != -1 && Main.mouseLeft && Main.mouseLeftRelease && hoveredSnippet < snippets.Length && tileNameMap.TryGetValue(snippets[hoveredSnippet].Text, out int reqTile))
            {
                RecipeCatalogueUI.instance.pendingQueryHowToCraftTileShouldGoto = true;
                RecipeCatalogueUI.instance.pendingQueryHowToCraftTile = reqTile;
            }
        }

        private static void DoChatTag(StringBuilder sb, bool comma, bool state, string text)
        {
            if (comma) sb.Append(", ");
            string hex = Utils.Hex3(state ? Utilities.yesColor : Utilities.noColor);
            sb.Append($"[c/{hex}:{text}]");
        }
    }

    public class UIRecipeInfoRightAligned : UIElement
    {
        private Recipe recipe;
        private List<int> tiles;
        private bool needWater;
        private bool needHoney;
        private bool needLava;

        public UIRecipeInfoRightAligned(Recipe recipe, List<int> tiles, bool needWater, bool needHoney, bool needLava)
        {
            this.recipe = recipe;
            this.tiles = tiles;
            this.needWater = needWater;
            this.needHoney = needHoney;
            this.needLava = needLava;
            Width.Set(24f, 0f);
            Height.Set(24f, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();
            Vector2 drawPos = new Vector2(dimensions.X, dimensions.Y);

            int count = (tiles != null ? tiles.Count : 0) + (needWater ? 1 : 0) + (needHoney ? 1 : 0) + (needLava ? 1 : 0);
            if (count == 0)
            {
                Texture2D byHand = RBTextures.TileByHand ?? TextureAssets.MagicPixel.Value;
                spriteBatch.Draw(byHand, drawPos, Color.White);
                if (IsMouseHovering)
                {
                    UICommon.TooltipMouseText(RBLanguage.GetText("RecipeCatalogueUI", "ByHand"));
                }
            }
            else
            {
                if (tiles != null && tiles.Count > 0)
                {
                    Texture2D tex = Utilities.GetTileImage(tiles[0]);
                    if (tex != null)
                    {
                        spriteBatch.Draw(tex, drawPos, Color.White);
                        if (IsMouseHovering)
                        {
                            UICommon.TooltipMouseText(Recipe.GetRequiredTileName(tiles[0]));
                        }
                    }
                }
            }
        }
    }
}
