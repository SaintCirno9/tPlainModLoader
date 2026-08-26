using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using RecipeBrowser.Common;
using RecipeBrowser.UIElements;
using TPML.Core.Pinyin;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.UI;

namespace RecipeBrowser
{
    public class ItemCatalogueUIPanel : UIPanel
    {
    }

    public class ItemCatalogueUI
    {
        public static ItemCatalogueUI instance;
        internal static Color color = Color.DarkGreen * 0.5f;

        internal UIPanel mainPanel;
        internal UIPanel itemGridPanel;
        internal UIGrid itemGrid;
        internal UIPanel itemDropViewerPanel;
        internal UIGrid itemDropViewerGrid;
        internal bool updateNeeded;
        internal int slowUpdateNeeded;
        internal NewUITextBox itemNameFilter;
        internal NewUITextBox itemDescriptionFilter;
        internal List<UIItemCatalogueItemSlot> itemSlots;
        internal bool[] craftResults;
        internal bool[] isLoot;
        internal List<UIElement> additionalDragTargets;
        internal UICheckbox CraftedRadioButton;
        internal UICheckbox LootRadioButton;
        internal UIJourneyDuplicateButton duplicationButton;

        internal static string RBText(string key, string category = "ItemCatalogueUI", params object[] args)
        {
            return RBLanguage.GetText(category, key);
        }

        public ItemCatalogueUI()
        {
            instance = this;
            itemSlots = new List<UIItemCatalogueItemSlot>();
            additionalDragTargets = new List<UIElement>();
        }

        internal UIElement CreateItemCataloguePanel()
        {
            mainPanel = new ItemCatalogueUIPanel();
            mainPanel.SetPadding(6f);
            mainPanel.BackgroundColor = color;
            mainPanel.Top.Set(20f, 0f);
            mainPanel.Height.Set(-20f, 1f);
            mainPanel.Width.Set(0f, 1f);

            itemNameFilter = new NewUITextBox(RBLanguage.GetText("Common", "FilterByName"));
            itemNameFilter.OnTextChanged += () => { ValidateItemFilter(); updateNeeded = true; };
            itemNameFilter.OnTabPressed += () => itemDescriptionFilter.Focus();
            itemNameFilter.Top.Pixels = 0f;
            itemNameFilter.Left.Set(-150f, 1f);
            itemNameFilter.Width.Set(150f, 0f);
            itemNameFilter.Height.Set(25f, 0f);
            mainPanel.Append(itemNameFilter);

            itemDescriptionFilter = new NewUITextBox(RBLanguage.GetText("Common", "FilterByTooltip"));
            itemDescriptionFilter.OnTextChanged += () => { updateNeeded = true; };
            itemDescriptionFilter.OnTabPressed += () => itemNameFilter.Focus();
            itemDescriptionFilter.Top.Pixels = 30f;
            itemDescriptionFilter.Left.Set(-150f, 1f);
            itemDescriptionFilter.Width.Set(150f, 0f);
            itemDescriptionFilter.Height.Set(25f, 0f);
            mainPanel.Append(itemDescriptionFilter);

            CraftedRadioButton = new UICheckbox(RBText("Crafted"), RBText("OnlyShowCraftedItems"));
            CraftedRadioButton.Top.Set(0f, 0f);
            CraftedRadioButton.Left.Set(-270f, 1f);
            CraftedRadioButton.OnSelectedChanged += (s, e) => { updateNeeded = true; };
            mainPanel.Append(CraftedRadioButton);

            LootRadioButton = new UICheckbox(RBText("Loot"), RBText("ShowOnlyLootItems"));
            LootRadioButton.Top.Set(20f, 0f);
            LootRadioButton.Left.Set(-270f, 1f);
            LootRadioButton.OnSelectedChanged += (s, e) => { updateNeeded = true; };
            mainPanel.Append(LootRadioButton);

            itemGridPanel = new UIPanel();
            itemGridPanel.SetPadding(6f);
            itemGridPanel.Top.Pixels = 60f;
            itemGridPanel.Width.Set(0f, 1f);
            itemGridPanel.Left.Set(0f, 0f);
            itemGridPanel.Height.Set(-76f, 1f);
            itemGridPanel.BackgroundColor = Color.CornflowerBlue * 0.3f;
            mainPanel.Append(itemGridPanel);

            itemGrid = new UIGrid();
            itemGrid.alternateSort = ItemGridSort;
            itemGrid.Width.Set(-20f, 1f);
            itemGrid.Height.Set(0f, 1f);
            itemGrid.ListPadding = 2f;
            itemGridPanel.Append(itemGrid);

            FixedUIScrollbar scrollbar = new FixedUIScrollbar(RecipeBrowserUI.instance.userInterface);
            scrollbar.SetView(100f, 1000f);
            scrollbar.Height.Set(0f, 1f);
            scrollbar.Left.Set(-20f, 1f);
            itemGridPanel.Append(scrollbar);
            itemGrid.SetScrollbar(scrollbar);

            itemDropViewerPanel = new UIPanel();
            itemDropViewerPanel.SetPadding(6f);
            itemDropViewerPanel.Top.Pixels = 60f;
            itemDropViewerPanel.Width.Set(230f, 0f);
            itemDropViewerPanel.Height.Set(-76f, 1f);
            itemDropViewerPanel.Left.Set(-230f, 1f);
            itemDropViewerPanel.BackgroundColor = Color.CornflowerBlue * 0.3f;

            itemDropViewerGrid = new UIGrid();
            itemDropViewerGrid.Width.Set(0f, 1f);
            itemDropViewerGrid.Height.Set(0f, 1f);
            itemDropViewerGrid.ListPadding = 2f;
            itemDropViewerGrid.drawArrows = true;
            itemDropViewerPanel.Append(itemDropViewerGrid);

            InvisibleFixedUIScrollbar dropScroll = new InvisibleFixedUIScrollbar(RecipeBrowserUI.instance.userInterface);
            dropScroll.SetView(100f, 1000f);
            dropScroll.Height.Set(0f, 1f);
            dropScroll.Left.Set(-20f, 1f);
            itemDropViewerGrid.SetScrollbar(dropScroll);

            UIText bottomText = new UIText(RBText("BottomInstructions"), 0.85f, false);
            bottomText.Top.Set(-14f, 1f);
            bottomText.HAlign = 0.5f;
            mainPanel.Append(bottomText);

            additionalDragTargets.Add(bottomText);
            if (SharedUI.instance?.sortsAndFiltersPanel != null)
            {
                additionalDragTargets.Add(SharedUI.instance.sortsAndFiltersPanel);
            }
            return mainPanel;
        }

        private int ItemGridSort(UIElement x, UIElement y)
        {
            if (x is UIPanel) return -1;
            if (y is UIPanel) return 1;
            if (x is UIItemCatalogueItemSlot s1 && y is UIItemCatalogueItemSlot s2)
            {
                if (SharedUI.instance?.SelectedSort != null)
                {
                    return SharedUI.instance.SelectedSort.sort(s1.item, s2.item);
                }
                return s1.itemType.CompareTo(s2.itemType);
            }
            return 0;
        }

        private void ValidateItemFilter()
        {
            updateNeeded = true;
        }

        internal void Update()
        {
            if (RecipeBrowserUI.instance == null || !RecipeBrowserUI.instance.ShowRecipeBrowser || RecipeBrowserUI.instance.CurrentPanel != 2)
            {
                return;
            }

            if (slowUpdateNeeded > 0)
            {
                slowUpdateNeeded--;
                if (slowUpdateNeeded == 0) updateNeeded = true;
            }

            if (!updateNeeded) return;
            updateNeeded = false;
            slowUpdateNeeded = 0;

            if (itemSlots.Count == 0)
            {
                craftResults = new bool[ItemID.Count];
                isLoot = new bool[ItemID.Count];
                itemSlots.Clear();

                for (int i = 1; i < ItemID.Count; i++)
                {
                    Item item = new Item();
                    item.SetDefaults(i);
                    if (item.type != 0)
                    {
                        itemSlots.Add(new UIItemCatalogueItemSlot(item));
                    }
                }

                for (int j = 0; j < Recipe.numRecipes; j++)
                {
                    Recipe r = Main.recipe[j];
                    if (r?.createItem != null && r.createItem.type < craftResults.Length)
                    {
                        craftResults[r.createItem.type] = true;
                    }
                }

                if (LootCache.instance == null || LootCache.instance.lootInfos == null || LootCache.instance.lootInfos.Count == 0)
                {
                    LootCacheManager.Setup();
                }

                if (LootCache.instance?.lootInfos != null)
                {
                    foreach (var kvp in LootCache.instance.lootInfos)
                    {
                        if (kvp.Key > 0 && kvp.Key < isLoot.Length)
                        {
                            isLoot[kvp.Key] = true;
                        }
                    }
                }
            }

            itemGrid.Clear();
            List<UIItemCatalogueItemSlot> list = itemSlots;

            foreach (var slot in list)
            {
                if (PassItemFilters(slot))
                {
                    itemGrid._items.Add(slot);
                    itemGrid._innerList.Append(slot);
                }
            }
            itemGrid.UpdateOrder();
            itemGrid._innerList.Recalculate();
        }

        internal void SetItem(UIItemCatalogueItemSlot slot)
        {
            foreach (var s in itemSlots) s.selected = false;
            slot.selected = true;

            if (duplicationButton != null)
            {
                mainPanel.RemoveChild(duplicationButton);
                duplicationButton = null;
            }

            if (Main.GameMode == 3 && slot.item != null && !slot.item.IsAir)
            {
                duplicationButton = new UIJourneyDuplicateButton(new CraftPath.JourneyDuplicateItemNode(slot.itemType, slot.item.maxStack, 0, null, null));
                duplicationButton.Top.Set(-18f, 1f);
                duplicationButton.Left.Set(2f, 0f);
                mainPanel.Append(duplicationButton);
            }
        }

        private bool PassItemFilters(UIItemCatalogueItemSlot slot)
        {
            if (CraftedRadioButton.Selected && (slot.item.type >= craftResults.Length || !craftResults[slot.item.type]))
            {
                return false;
            }
            if (LootRadioButton.Selected && (slot.item.type >= isLoot.Length || !isLoot[slot.item.type]))
            {
                return false;
            }

            Category selCat = SharedUI.instance?.SelectedCategory;
            if (selCat != null && !selCat.belongs(slot.item) && !selCat.subCategories.Any(x => x.belongs(slot.item)))
            {
                return false;
            }

            if (SharedUI.instance?.availableFilters != null)
            {
                foreach (var filter in SharedUI.instance.availableFilters)
                {
                    if (!filter.button.selected) continue;
                    if (!filter.belongs(slot.item)) return false;

                    if (filter == SharedUI.instance.CraftableFilter)
                    {
                        bool avail = false;
                        for (int i = 0; i < Main.numAvailableRecipes; i++)
                        {
                            if (Main.recipe[Main.availableRecipe[i]].createItem.type == slot.item.type)
                            {
                                avail = true;
                                break;
                            }
                        }
                        if (!avail) return false;
                    }
                }
            }

            string nameStr = itemNameFilter.currentString.Trim();
            if (nameStr.Length > 0)
            {
                string name = slot.item.Name;
                string internalName = (slot.item.type > 0 && slot.item.type < ItemID.Count) ? ItemID.Search.GetName(slot.item.type) : "";
                string localizedName = Lang.GetItemNameValue(slot.item.type);

                if (!PinyinHelper.Matches(name, nameStr) &&
                    !PinyinHelper.Matches(localizedName, nameStr) &&
                    !PinyinHelper.Matches(internalName, nameStr))
                {
                    return false;
                }
            }

            string descStr = itemDescriptionFilter.currentString.Trim();
            if (descStr.Length > 0)
            {
                if (slot is UIArmorSetCatalogueItemSlot armorSlot)
                {
                    return PinyinHelper.Matches(armorSlot.set.Item4, descStr);
                }
                if (slot.item.ToolTip != null && PinyinHelper.Matches(GetTooltipsAsString(slot.item.ToolTip), descStr))
                {
                    return true;
                }
                return false;
            }

            return true;
        }

        private string GetTooltipsAsString(ItemTooltip toolTip)
        {
            if (toolTip == null) return "";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < toolTip.Lines; i++)
            {
                sb.AppendLine(toolTip.GetLine(i));
            }
            return sb.ToString().ToLower();
        }

        internal void ToggleItemDropViewer(bool show)
        {
            if (show)
            {
                itemGridPanel.Width.Set(-232f, 1f);
                mainPanel.Append(itemDropViewerPanel);
            }
            else
            {
                itemGridPanel.Width.Set(0f, 1f);
                mainPanel.RemoveChild(itemDropViewerPanel);
            }
            itemGridPanel.Recalculate();
        }

        internal void PopulateItemDropViewerPanel(int type)
        {
            itemDropViewerGrid.Clear();
            ToggleItemDropViewer(false);
        }
    }
}
