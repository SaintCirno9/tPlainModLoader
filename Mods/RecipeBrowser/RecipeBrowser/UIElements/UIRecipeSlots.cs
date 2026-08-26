using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using RecipeBrowser.TagHandlers;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Chat;
using TPML.Content.Fusion;

namespace RecipeBrowser.UIElements
{
    public class UIRecipeSlot : UIItemSlot
    {
        public static Texture2D selectedBackgroundTexture => TextureAssets.InventoryBack15?.Value ?? TextureAssets.InventoryBack.Value;
        public static Texture2D favoritedBackgroundTexture => TextureAssets.InventoryBack10?.Value ?? TextureAssets.InventoryBack.Value;
        public static Texture2D ableToCraftBackgroundTexture => TextureAssets.InventoryBack3?.Value ?? TextureAssets.InventoryBack.Value;
        public static Texture2D ableToCraftExtendedBackgroundTexture => TextureAssets.InventoryBack8?.Value ?? TextureAssets.InventoryBack.Value;

        public static bool[] availableRecipesCache;

        public static void RefreshAvailableRecipesCache()
        {
            if (availableRecipesCache == null || availableRecipesCache.Length != Recipe.numRecipes)
            {
                availableRecipesCache = new bool[Math.Max(Recipe.numRecipes, 1)];
            }
            else
            {
                Array.Clear(availableRecipesCache, 0, availableRecipesCache.Length);
            }

            for (int i = 0; i < Main.numAvailableRecipes; i++)
            {
                int rIdx = Main.availableRecipe[i];
                if (rIdx >= 0 && rIdx < availableRecipesCache.Length)
                {
                    availableRecipesCache[rIdx] = true;
                }
            }
        }

        public int index;
        public bool selected;
        public bool favorited;
        public bool recentlyDiscovered;

        public bool craftPathNeeded;
        public bool craftPathCalculated;
        public bool craftPathsCalculated;
        public bool craftPathCalculationBegun;
        public CancellationTokenSource craftPathCancellationTokenSource;
        public List<CraftPath> craftPaths;

        public UIRecipeSlot(int index, float scale = 0.75f) : base(Main.recipe[index].createItem, scale)
        {
            this.index = index;
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            using (RBProfiler.Step($"UIRecipeSlot.LeftClick [Recipe #{index} ({item?.Name})]"))
            {
                if (Main.keyState.IsKeyDown(Main.FavoriteKey))
                {
                    favorited = !favorited;
                    RecipeBrowserUI.instance.FavoriteChange(index, favorited);
                }
                else
                {
                    if (Main.drawingPlayerChat)
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (Item it in Main.recipe[index].requiredItem)
                        {
                            if (it != null && !it.IsAir) sb.Append(ItemHoverFixTagHandler.GenerateTag(it.type, it.stack));
                        }
                        sb.Append("-->");
                        sb.Append(ItemHoverFixTagHandler.GenerateTag(Main.recipe[index].createItem.type, Main.recipe[index].createItem.stack));
                        if (ChatManager.AddChatText(FontAssets.MouseText.Value, sb.ToString(), Vector2.One))
                        {
                            SoundEngine.PlaySound(SoundID.MenuTick);
                        }
                    }
                    else
                    {
                        using (RBProfiler.Step("RecipeCatalogueUI.SetRecipe"))
                        {
                            RecipeCatalogueUI.instance.SetRecipe(index);
                        }
                    }
                }

                if (Main.playerInventory)
                {
                    using (RBProfiler.Step("Sync focusRecipe"))
                    {
                        for (int i = 0; i < Main.numAvailableRecipes; i++)
                        {
                            if (index == Main.availableRecipe[i])
                            {
                                Main.focusRecipe = i;
                                break;
                            }
                        }
                    }
                }
            }
        }

        public override void LeftDoubleClick(UIMouseEvent evt)
        {
            using (RBProfiler.Step($"UIRecipeSlot.LeftDoubleClick [Recipe #{index} ({item?.Name})]"))
            {
                if (!Main.keyState.IsKeyDown(Main.FavoriteKey))
                {
                    RecipeBrowserUI.instance.tabController.SetPanel(1);
                    CraftUI.instance.SetRecipe(index);
                }
            }
        }

        public override void RightClick(UIMouseEvent evt)
        {
            favorited = !favorited;
            RecipeBrowserUI.instance.FavoriteChange(index, favorited);
        }

        public override int CompareTo(object obj)
        {
            UIRecipeSlot other = obj as UIRecipeSlot;
            int num = CompareToIgnoreIndex(other);
            if (num != 0) return num;
            return index.CompareTo(other.index);
        }

        public int CompareToIgnoreIndex(UIRecipeSlot other)
        {
            if (other == null) return 1;
            int num = (other.favorited ? 1 : 0) - (favorited ? 1 : 0);
            if (num != 0) return num;
            int num2 = (other.recentlyDiscovered ? 1 : 0) - (recentlyDiscovered ? 1 : 0);
            if (num2 != 0) return num2;
            int num3 = (other.AbleToCraft() ? 1 : 0) - (AbleToCraft() ? 1 : 0);
            if (num3 != 0) return num3;
            if (RecipePath.extendedCraft)
            {
                int num4 = (other.AbleToCraftExtended() ? 1 : 0) - (AbleToCraftExtended() ? 1 : 0);
                if (num4 != 0) return num4;
            }
            return 0;
        }

        public bool AbleToCraft()
        {
            if (availableRecipesCache != null && index >= 0 && index < availableRecipesCache.Length)
            {
                return availableRecipesCache[index];
            }
            for (int i = 0; i < Main.numAvailableRecipes; i++)
            {
                if (index == Main.availableRecipe[i]) return true;
            }
            return false;
        }

        public bool AbleToCraftExtended()
        {
            if (craftPathsCalculated || craftPathCalculated)
            {
                return craftPaths != null && craftPaths.Count > 0;
            }
            return false;
        }

        public void CraftPathNeeded()
        {
            if (RecipePath.extendedCraft)
            {
                craftPathNeeded = true;
                if (!craftPathCalculated && !craftPathCalculationBegun)
                {
                    craftPathCalculationBegun = true;
                    craftPathCancellationTokenSource = new CancellationTokenSource();
                    RecipeBrowserMod.Instance.concurrentTasks.Enqueue(new Task(() =>
                    {
                        craftPaths = RecipePath.GetCraftPaths(Main.recipe[index], craftPathCancellationTokenSource.Token, single: true);
                        craftPathCalculated = true;
                        RecipeCatalogueUI.instance.slowUpdateNeeded = 2;
                    }, craftPathCancellationTokenSource.Token));
                }
            }
        }

        public void CraftPathsImmediatelyNeeded()
        {
            if (RecipePath.extendedCraft)
            {
                if (!craftPathsCalculated)
                {
                    if (craftPathCalculationBegun)
                    {
                        craftPathCancellationTokenSource?.Cancel();
                    }
                    craftPaths = RecipePath.GetCraftPaths(Main.recipe[index], CancellationToken.None, single: false);
                    craftPathsCalculated = true;
                }
            }
            else
            {
                craftPaths = new List<CraftPath> { new CraftPath(Main.recipe[index], new Dictionary<int, int>()) };
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Texture2D backTex = TextureAssets.InventoryBack.Value;
            if (selected) backTex = selectedBackgroundTexture ?? backTex;
            else if (favorited) backTex = favoritedBackgroundTexture ?? backTex;
            else if (AbleToCraft()) backTex = ableToCraftBackgroundTexture ?? backTex;
            else if (RecipePath.extendedCraft && AbleToCraftExtended()) backTex = ableToCraftExtendedBackgroundTexture ?? backTex;

            CalculatedStyle dimensions = GetDimensions();
            spriteBatch.Draw(backTex, dimensions.Position(), null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            DrawItem(spriteBatch, item, dimensions.Position(), scale);

            if (IsMouseHovering)
            {
                Main.hoverItemName = item.HoverName;
                Main.HoverItem = item.Clone();
            }
        }
    }

    public class UIMockRecipeSlot : UIItemSlot
    {
        public static Texture2D ableToCraftBackgroundTexture;
        private UIRecipeSlot slot;

        public UIMockRecipeSlot(UIRecipeSlot slot, float scale = 0.75f) : base(slot.item, scale)
        {
            this.slot = slot;
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            slot.LeftClick(evt);
        }

        public override void RightClick(UIMouseEvent evt)
        {
            slot.RightClick(evt);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Texture2D backTex = TextureAssets.InventoryBack.Value;
            if (slot.AbleToCraft()) backTex = ableToCraftBackgroundTexture ?? backTex;
            else if (RecipePath.extendedCraft && slot.AbleToCraftExtended()) backTex = UIRecipeSlot.ableToCraftExtendedBackgroundTexture ?? backTex;

            CalculatedStyle dimensions = GetDimensions();
            spriteBatch.Draw(backTex, dimensions.Position(), null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            DrawItem(spriteBatch, item, dimensions.Position(), scale);

            if (IsMouseHovering)
            {
                Main.hoverItemName = item.HoverName;
                Main.HoverItem = item.Clone();
            }
        }
    }

    public class UIIngredientSlot : UIItemSlot
    {
        private int ingredientIndex;

        public UIIngredientSlot(Item item, int ingredientIndex, float scale = 0.75f) : base(item, scale)
        {
            this.ingredientIndex = ingredientIndex;
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            if (Main.drawingPlayerChat)
            {
                if (ChatManager.AddChatText(FontAssets.MouseText.Value, ItemHoverFixTagHandler.GenerateTag(item.type, item.stack), Vector2.One))
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                }
            }
            else
            {
                RecipeCatalogueUI.instance.queryItem.ReplaceWithFake(item.type);
            }
        }

        public override void RightClick(UIMouseEvent evt)
        {
            RecipeCatalogueUI.instance.queryItem.ReplaceWithFake(item.type);
        }
    }

    public class UITrackIngredientSlot : UIItemSlot
    {
        private int ingredientIndex;
        private Recipe recipe;
        private int owner;

        public UITrackIngredientSlot(Recipe recipe, Item item, int ingredientIndex, int owner, float scale = 0.75f) : base(item, scale)
        {
            this.recipe = recipe;
            this.ingredientIndex = ingredientIndex;
            this.owner = owner;
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            RecipeCatalogueUI.instance.queryItem.ReplaceWithFake(item.type);
        }

        public override void RightClick(UIMouseEvent evt)
        {
            RecipeCatalogueUI.instance.queryItem.ReplaceWithFake(item.type);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();
            DrawItem(spriteBatch, item, dimensions.Position(), scale);

            Player targetPlayer = (owner >= 0 && owner < 255) ? Main.player[owner] : Main.LocalPlayer;
            int count = CountItemGroups(targetPlayer, recipe, item.type, item.stack);

            Color color = count >= item.stack ? Color.White : Color.LightSalmon;
            string text = $"{count}/{item.stack}";
            Vector2 textPos = dimensions.Position() + new Vector2(0f, dimensions.Height - 12f);
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, text, textPos, color, 0f, Vector2.Zero, new Vector2(0.6f));

            if (IsMouseHovering)
            {
                Main.hoverItemName = item.HoverName;
                Main.HoverItem = item.Clone();
            }
        }

        public int CountItemGroups(Player player, Recipe recipe, int type, int stopCountingAt = 1)
        {
            if (type == 0 || player == null) return 0;
            int num = 0;
            for (int i = 0; i <= 58; i++)
            {
                Item inv = player.inventory[i];
                if (inv != null && !inv.IsAir)
                {
                    bool match = (inv.type == type);
                    if (!match && recipe != null && recipe.acceptedGroups != null)
                    {
                        foreach (int grp in recipe.acceptedGroups)
                        {
                            if (grp >= 0 && RecipeGroup.recipeGroups.TryGetValue(grp, out var rg) && rg.ValidItems.Contains(inv.type) && rg.ValidItems.Contains(type))
                            {
                                match = true;
                                break;
                            }
                        }
                    }
                    if (match)
                    {
                        num += inv.stack;
                    }
                }
            }

            // 通用背包融合 (Fusion) 穿透检测
            try
            {
                var fusionItems = InventoryFusionManager.GetAllFusionItems(player);
                if (fusionItems != null)
                {
                    foreach (var fit in fusionItems)
                    {
                        if (fit != null && !fit.IsAir)
                        {
                            bool match = (fit.type == type);
                            if (!match && recipe != null && recipe.acceptedGroups != null)
                            {
                                foreach (int grp in recipe.acceptedGroups)
                                {
                                    if (grp >= 0 && RecipeGroup.recipeGroups.TryGetValue(grp, out var rg) && rg.ValidItems.Contains(fit.type) && rg.ValidItems.Contains(type))
                                    {
                                        match = true;
                                        break;
                                    }
                                }
                            }
                            if (match)
                            {
                                num += fit.stack;
                            }
                        }
                    }
                }
            }
            catch { }

            return num;
        }
    }

    public class UICraftButton : UIElement
    {
        private CraftPath.RecipeNode recipeNode;
        private Recipe recipe;
        private int index = -1;

        public UICraftButton(CraftPath.RecipeNode recipeNode, Recipe recipe)
        {
            this.recipe = recipe;
            this.recipeNode = recipeNode;
            Width.Set(TextureAssets.Reforge[0].Width(), 0f);
            Height.Set(TextureAssets.Reforge[0].Height(), 0f);
            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                if (recipe == Main.recipe[i])
                {
                    index = i;
                    break;
                }
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            bool flag = AbleToCraft();
            CalculatedStyle dimensions = GetDimensions();
            Vector2 position = new Vector2(dimensions.X, dimensions.Y);
            spriteBatch.Draw(TextureAssets.Reforge[(!flag) ? 1 : 0].Value, position, Color.White);
            if (IsMouseHovering)
            {
                Main.hoverItemName = flag ? RBLanguage.GetText("CraftUI", "Craft") : RBLanguage.GetText("CraftUI", "CannotCraft");
            }
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            if (index == -1 || recipe == null) return;

            int mult = recipeNode != null ? recipeNode.multiplier : 1;
            for (int i = 0; i < mult; i++)
            {
                Recipe.UpdateRecipeList();
                if (AbleToCraft())
                {
                    Item crafted = recipe.createItem.Clone();
                    crafted.Prefix(-1);

                    for (int k = 0; k < recipe.requiredItem.Length; k++)
                    {
                        Item req = recipe.requiredItem[k];
                        if (req == null || req.IsAir) continue;
                        int needed = req.stack;
                        for (int s = 0; s < 58; s++)
                        {
                            Item it = Main.LocalPlayer.inventory[s];
                            if (it != null && !it.IsAir && (it.type == req.type || (recipe.acceptedGroups != null && recipe.acceptedGroups.Any(grp => grp >= 0 && RecipeGroup.recipeGroups.TryGetValue(grp, out var rg) && rg.ValidItems.Contains(it.type) && rg.ValidItems.Contains(req.type)))))
                            {
                                int take = Math.Min(needed, it.stack);
                                it.stack -= take;
                                needed -= take;
                                if (it.stack <= 0) it.TurnToAir();
                                if (needed <= 0) break;
                            }
                        }
                    }

                    Main.LocalPlayer.QuickSpawnItem(null, crafted.type, crafted.stack);
                }
            }
        }

        private bool AbleToCraft()
        {
            if (index == -1) return false;
            if (Main.guideItem.type > 0 && Main.guideItem.stack > 0 && !string.IsNullOrEmpty(Main.guideItem.Name))
            {
                return false;
            }
            for (int i = 0; i < Main.numAvailableRecipes; i++)
            {
                if (index == Main.availableRecipe[i]) return true;
            }
            return false;
        }
    }

    public class UIRecipeProgress : UIElement
    {
        private int order;
        private int owner;

        public UIRecipeProgress(int index, Recipe recipe, int order, int owner)
        {
            this.order = order;
            this.owner = owner;

            UIMockRecipeSlot mockSlot = new UIMockRecipeSlot(RecipeCatalogueUI.instance.recipeSlots[index], owner != Main.myPlayer ? 0.5f : 0.75f);
            mockSlot.Recalculate();
            mockSlot.Left.Set(-mockSlot.Width.Pixels - (owner != Main.myPlayer ? 23 : 0), 1f);
            CalculatedStyle outer = mockSlot.GetOuterDimensions();
            Append(mockSlot);

            int offset = (owner != Main.myPlayer ? 23 : 0) + (int)outer.Width + 2;
            int rowTop = 0;
            int maxOffset = offset;
            int wrapCols = owner != Main.myPlayer ? 8 : 6;

            for (int i = 0; i < recipe.requiredItem.Length; i++)
            {
                Item req = recipe.requiredItem[i];
                if (req == null || req.IsAir) continue;

                UITrackIngredientSlot trackSlot = new UITrackIngredientSlot(recipe, req, i, owner, owner != Main.myPlayer ? 0.5f : 0.75f);
                trackSlot.Recalculate();
                trackSlot.Left.Set(-offset - trackSlot.Width.Pixels, 1f);
                trackSlot.Top.Set(rowTop, 0f);
                Append(trackSlot);

                offset += (int)trackSlot.Width.Pixels + 2;
                maxOffset = Math.Max(maxOffset, offset);

                if ((i + 1) % wrapCols == 0)
                {
                    offset = (owner != Main.myPlayer ? 23 : 0) + (int)outer.Width + 2;
                    rowTop += (int)trackSlot.Height.Pixels + 2;
                }
            }

            Height.Set(Math.Max(outer.Height, rowTop + outer.Height), 0f);
            Width.Set(maxOffset, 0f);
        }
    }
}
