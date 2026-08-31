using System.Collections.Generic;
using Microsoft.Xna.Framework;
using RecipeBrowser.Common;
using RecipeBrowser.UIElements;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace RecipeBrowser
{
    public class CraftUI
    {
        internal static CraftUI instance;
        internal static Color color = new Color(90, 158, 57) * 0.5f;

        internal UIPanel mainPanel;
        internal UIPanel craftPanel;
        internal UIList craftPathList;
        internal UICraftQueryItemSlot recipeResultItemSlot;
        internal List<UIElement> additionalDragTargets;
        internal List<int> selectedIndexes;
        internal bool craftPathsUpToDate;

        internal static string RBText(string key, string category = "CraftUI")
        {
            return RBLanguage.GetText(category, key);
        }

        public CraftUI()
        {
            instance = this;
            additionalDragTargets = new List<UIElement>();
            selectedIndexes = new List<int>();
        }

        internal UIElement CreateCraftPanel()
        {
            mainPanel = new UIPanel();
            mainPanel.SetPadding(6f);
            mainPanel.BackgroundColor = color;
            mainPanel.Top.Set(20f, 0f);
            mainPanel.Height.Set(-20f, 1f);
            mainPanel.Width.Set(0f, 1f);

            recipeResultItemSlot = new UICraftQueryItemSlot(new Item());
            recipeResultItemSlot.emptyHintText = RBText("EmptyQuerySlotHint");
            mainPanel.Append(recipeResultItemSlot);

            UICheckbox extendedCraft = new UICheckbox(RBText("Calc"), RBText("CalcTooltip"));
            extendedCraft.Top.Set(42f, 0f);
            extendedCraft.Left.Set(0f, 0f);
            extendedCraft.SetText("  " + RBText("Calc"));
            extendedCraft.Selected = RecipePath.extendedCraft;
            extendedCraft.OnSelectedChanged += (s, e) =>
            {
                RecipeCatalogueUI.instance?.InvalidateExtendedCraft();
                RecipePath.extendedCraft = extendedCraft.Selected;
            };
            mainPanel.Append(extendedCraft);

            int top = 2;
            int left = 50;

            UICheckbox lootables = new UICheckbox(RBText("Loot"), RBText("LootTooltip"));
            lootables.Top.Set(top, 0f);
            lootables.Left.Set(left, 0f);
            lootables.Selected = RecipePath.allowLoots;
            lootables.OnSelectedChanged += (s, e) =>
            {
                RecipeCatalogueUI.instance?.InvalidateExtendedCraft();
                RecipePath.allowLoots = lootables.Selected;
            };
            mainPanel.Append(lootables);
            left += (int)lootables.MinWidth.Pixels + 10;

            UICheckbox mineables = new UICheckbox(RBText("Mineable"), RBText("MineableTooltip"));
            mineables.Top.Set(top, 0f);
            mineables.Left.Set(left, 0f);
            mineables.Selected = RecipePath.allowMineables;
            mineables.OnSelectedChanged += (s, e) =>
            {
                RecipeCatalogueUI.instance?.InvalidateExtendedCraft();
                RecipePath.allowMineables = mineables.Selected;
                RecipePath.PrepareGetCraftPaths();
            };
            mainPanel.Append(mineables);
            left += (int)mineables.MinWidth.Pixels + 10;

            UICheckbox bugnetables = new UICheckbox(RBText("Bugnet"), RBText("BugnetTooltip"));
            bugnetables.Top.Set(top, 0f);
            bugnetables.Left.Set(left, 0f);
            bugnetables.Selected = RecipePath.allowBugNetables;
            bugnetables.OnSelectedChanged += (s, e) =>
            {
                RecipeCatalogueUI.instance?.InvalidateExtendedCraft();
                RecipePath.allowBugNetables = bugnetables.Selected;
                RecipePath.PrepareGetCraftPaths();
            };
            mainPanel.Append(bugnetables);
            left += (int)bugnetables.MinWidth.Pixels + 10;

            UICheckbox npcShopsCheckbox = new UICheckbox(RBText("Shop"), RBText("ShopTooltip"));
            npcShopsCheckbox.Top.Set(top, 0f);
            npcShopsCheckbox.Left.Set(left, 0f);
            npcShopsCheckbox.Selected = RecipePath.allowPurchasable;
            npcShopsCheckbox.OnSelectedChanged += (s, e) =>
            {
                RecipeCatalogueUI.instance?.InvalidateExtendedCraft();
                RecipePath.allowPurchasable = npcShopsCheckbox.Selected;
                RecipePath.purchasable = null;
                RecipePath.PrepareGetCraftPaths();
            };
            mainPanel.Append(npcShopsCheckbox);

            // 第二行设置：缺失制作站、物品栏
            top += 25;
            left = 50;

            UICheckbox missingStationsCheckbox = new UICheckbox(RBText("MissingStations"), RBText("MissingStationsTooltip"));
            missingStationsCheckbox.Top.Set(top, 0f);
            missingStationsCheckbox.Left.Set(left, 0f);
            missingStationsCheckbox.Selected = RecipePath.allowMissingStations;
            missingStationsCheckbox.OnSelectedChanged += (s, e) =>
            {
                RecipeCatalogueUI.instance?.InvalidateExtendedCraft();
                RecipePath.allowMissingStations = missingStationsCheckbox.Selected;
            };
            mainPanel.Append(missingStationsCheckbox);
            left += (int)missingStationsCheckbox.MinWidth.Pixels + 10;

            UICheckbox sourceInventoryCheckbox = new UICheckbox(RBText("Inventory"), "");
            sourceInventoryCheckbox.SetDisabled();
            sourceInventoryCheckbox.Top.Set(top, 0f);
            sourceInventoryCheckbox.Left.Set(left, 0f);
            sourceInventoryCheckbox.Selected = RecipePath.sourceInventory;
            mainPanel.Append(sourceInventoryCheckbox);

            top += 37;
            craftPanel = new UIPanel();
            craftPanel.SetPadding(6f);
            craftPanel.Top.Pixels = top;
            craftPanel.Left.Set(0f, 0f);
            craftPanel.Width.Set(0f, 1f);
            craftPanel.Height.Set(-top - 16, 1f);
            craftPanel.BackgroundColor = Color.DarkCyan * 0.3f;

            craftPathList = new UIList();
            craftPathList.Width.Set(-24f, 1f);
            craftPathList.Height.Set(0f, 1f);
            craftPathList.ListPadding = 6f;
            craftPanel.Append(craftPathList);

            FixedUIScrollbar scrollbar = new FixedUIScrollbar(RecipeBrowserUI.instance.userInterface);
            scrollbar.SetView(100f, 1000f);
            scrollbar.Height.Set(0f, 1f);
            scrollbar.Left.Set(-20f, 1f);
            craftPanel.Append(scrollbar);
            craftPathList.SetScrollbar(scrollbar);
            mainPanel.Append(craftPanel);

            UIText bottomText = new UIText(RBText("BottomInstructions"), 0.85f, false);
            bottomText.Top.Set(-14f, 1f);
            bottomText.HAlign = 0.5f;
            mainPanel.Append(bottomText);
            additionalDragTargets.Add(bottomText);

            return mainPanel;
        }

        private void PopulateList(List<UIRecipeSlot> slots)
        {
            using (RBProfiler.Step($"CraftUI.PopulateList ({slots.Count} slots)"))
            {
                List<UIRecipePath> list = new List<UIRecipePath>();
                craftPathList.Clear();
                foreach (var slot in slots)
                {
                    if (slot.craftPaths != null)
                    {
                        foreach (var path in slot.craftPaths)
                        {
                            list.Add(new UIRecipePath(path));
                        }
                    }
                }
                foreach (var p in list)
                {
                    craftPathList.Add(p);
                }
                if (list.Count == 0)
                {
                    UIText noPaths = new UIText(RBText("NoPathsFound"), 1f, false);
                    craftPathList.Add(noPaths);
                }
            }
        }

        internal void SetRecipe(int index)
        {
            using (RBProfiler.Step($"CraftUI.SetRecipe #{index}"))
            {
                selectedIndexes.Clear();
                selectedIndexes.Add(index);
                Recipe r = Main.recipe[index];
                recipeResultItemSlot.ReplaceWithFake(r.createItem.type);
                craftPathsUpToDate = false;
            }
        }

        internal void SetItem(int itemID)
        {
            using (RBProfiler.Step($"CraftUI.SetItem #{itemID}"))
            {
                List<int> list = new List<int>();
                for (int i = 0; i < Recipe.numRecipes; i++)
                {
                    if (Main.recipe[i].createItem.type == itemID)
                    {
                        list.Add(i);
                    }
                }
                selectedIndexes.Clear();
                selectedIndexes.AddRange(list);
                recipeResultItemSlot.ReplaceWithFake(itemID);
                craftPathsUpToDate = false;
            }
        }

        internal void Update()
        {
            if (craftPathsUpToDate || RecipeBrowserUI.instance == null || !RecipeBrowserUI.instance.ShowRecipeBrowser || RecipeBrowserUI.instance.CurrentPanel != 1)
            {
                return;
            }

            using (RBProfiler.Step("CraftUI.Update (Calculate CraftPaths)"))
            {
                craftPathsUpToDate = true;
                List<UIRecipeSlot> list = new List<UIRecipeSlot>();
                foreach (int idx in selectedIndexes)
                {
                    if (RecipeCatalogueUI.instance?.recipeSlots != null && idx < RecipeCatalogueUI.instance.recipeSlots.Count)
                    {
                        UIRecipeSlot rSlot = RecipeCatalogueUI.instance.recipeSlots[idx];
                        using (RBProfiler.Step($"CraftPathsImmediatelyNeeded (Recipe #{idx})"))
                        {
                            rSlot.CraftPathsImmediatelyNeeded();
                        }
                        list.Add(rSlot);
                    }
                }
                PopulateList(list);
            }
        }
    }
}
