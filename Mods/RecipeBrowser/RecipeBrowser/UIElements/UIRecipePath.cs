using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using RecipeBrowser.Common;
using RecipeBrowser.TagHandlers;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria.UI.Chat;

namespace RecipeBrowser.UIElements
{
    public class UIRecipePath : UIPanel
    {
        private CraftPath path;
        private const int verticalSpace = 24;
        private const int HorizontalTab = 20;

        public UIRecipePath(CraftPath path)
        {
            this.path = path;
            SetPadding(6f);
            Width.Set(0f, 1f);

            int top = 0;
            Dictionary<int, int> totalItemCost = new Dictionary<int, int>();
            int count = Traverse(path.root, 0, ref top, totalItemCost);

            StringBuilder sb = new StringBuilder();
            sb.Append(RBLanguage.GetText("CraftUI", "Cost"));
            foreach (var kvp in totalItemCost)
            {
                if (kvp.Key == 71)
                {
                    sb.Append(CraftPath.BuyItemNode.GetTotalCostAsTags(kvp.Value));
                }
                else
                {
                    sb.Append(ItemHoverFixTagHandler.GenerateTag(kvp.Key, kvp.Value));
                }
            }

            foreach (var lineSnippets in UIMessageBox.WordwrapStringSmart(sb.ToString(), Color.White, FontAssets.MouseText.Value, 300, -1))
            {
                UITextSnippet snippet = new UITextSnippet(string.Concat(lineSnippets.Select(x => x.TextOriginal)));
                snippet.Top.Set(top, 0f);
                snippet.Left.Set(0f, 0f);
                Append(snippet);
                top += 24;
                count++;
            }

            Height.Set(count * 24 + PaddingBottom + PaddingTop, 0f);
        }

        private int Traverse(CraftPath.CraftPathNode node, int left, ref int top, Dictionary<int, int> totalItemCost)
        {
            int count = 1;
            StringBuilder sb = new StringBuilder();
            CraftPath.RecipeNode recipeNode = node as CraftPath.RecipeNode;

            if (recipeNode != null)
            {
                sb.Append(ItemHoverFixTagHandler.GenerateTag(recipeNode.recipe.createItem.type, recipeNode.recipe.createItem.stack * recipeNode.multiplier));
                sb.Append('<');
                for (int i = 0; i < recipeNode.recipe.requiredItem.Length; i++)
                {
                    Item item = recipeNode.recipe.requiredItem[i];
                    if (item == null || item.IsAir) continue;
                    bool check = (recipeNode.children != null && i < recipeNode.children.Length) && (recipeNode.children[i] is CraftPath.HaveItemNode);
                    string nameOverride = RecipeCatalogueUI.OverrideForGroups(recipeNode.recipe, item.type);
                    sb.Append(ItemHoverFixTagHandler.GenerateTag(item.type, item.stack * recipeNode.multiplier, nameOverride, check));
                }
            }
            else if (node is CraftPath.HaveItemNode)
            {
                count--;
            }
            else
            {
                sb.Append(node.ToUITextString());
            }

            if (node is CraftPath.HaveItemNode haveItemNode)
            {
                totalItemCost.Adjust(haveItemNode.itemid, haveItemNode.stack);
            }
            if (node is CraftPath.HaveItemsNode haveItemsNode)
            {
                foreach (var tuple in haveItemsNode.listOfItems)
                {
                    totalItemCost.Adjust(tuple.Item1, tuple.Item2);
                }
            }
            if (node is CraftPath.BuyItemNode buyItemNode)
            {
                totalItemCost.Adjust(71, buyItemNode.TotalPrice);
            }

            if (sb.Length > 0)
            {
                UITextSnippet snippet = new UITextSnippet(sb.ToString());
                snippet.Top.Set(top, 0f);
                snippet.Left.Set(left, 0f);
                Append(snippet);

                if (recipeNode != null)
                {
                    HashSet<int> reqTiles = new HashSet<int>();
                    if (recipeNode.recipe.requiredTile >= 0)
                    {
                        reqTiles.Add(recipeNode.recipe.requiredTile);
                    }

                    bool needWater = recipeNode.recipe.needWater;
                    bool needHoney = recipeNode.recipe.needHoney;
                    bool needLava = recipeNode.recipe.needLava;

                    UIRecipeInfoRightAligned infoRight = new UIRecipeInfoRightAligned(recipeNode.recipe, reqTiles.ToList(), needWater, needHoney, needLava);
                    infoRight.Top.Set(top, 0f);
                    infoRight.Left.Set(-30f, 1f);
                    Append(infoRight);

                    UICraftButton craftBtn = new UICraftButton(recipeNode, recipeNode.recipe);
                    craftBtn.Top.Set(top, 0f);
                    craftBtn.Left.Set(-26f, 1f);
                    Append(craftBtn);
                }

                if (node is CraftPath.JourneyDuplicateItemNode duplicationNode)
                {
                    UIJourneyDuplicateButton dupBtn = new UIJourneyDuplicateButton(duplicationNode);
                    dupBtn.Top.Set(top, 0f);
                    dupBtn.Left.Set(-26f, 1f);
                    Append(dupBtn);
                }

                top += 24;
            }

            if (node.children != null)
            {
                foreach (var child in node.children)
                {
                    if (child != null)
                    {
                        count += Traverse(child, left + 20, ref top, totalItemCost);
                    }
                }
            }

            return count;
        }
    }
}
