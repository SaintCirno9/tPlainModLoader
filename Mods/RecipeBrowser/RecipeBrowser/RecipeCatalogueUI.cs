using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using RecipeBrowser.TagHandlers;
using RecipeBrowser.UIElements;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using TPML.Content.Fusion;
using TPML.Core.Pinyin;

namespace RecipeBrowser
{
    public class RecipeCatalogueUI
    {
        public static RecipeCatalogueUI instance;
        internal static Color color = new Color(73, 94, 171);

        internal UIRecipeCatalogueQueryItemSlot queryItem;
        internal UICheckbox TileLookupRadioButton;
        internal Item queryLootItem;
        internal bool tileIsItemsThatPlaceThisTileInstead;
        internal int pendingQueryHowToCraftTile = -1;
        internal bool pendingQueryHowToCraftTileShouldGoto;
        private int tile = -1;
        internal NewUITextBox itemNameFilter;
        internal NewUITextBox itemDescriptionFilter;
        internal UIPanel mainPanel;
        internal UIPanel recipeGridPanel;
        internal UIGrid recipeGrid;
        internal UIGrid lootSourceGrid;
        internal UIPanel tileChooserPanel;
        internal UICycleImage uniqueCheckbox;
        internal UIGrid tileChooserGrid;
        internal UIRecipeInfo recipeInfo;
        internal UIRadioButton NearbyIngredientsRadioButton;
        internal UIRadioButtonGroup RadioButtonGroup;
        internal UIJourneyDuplicateButton duplicationButton;
        internal int selectedIndex = -1;
        internal int hoveredIndex = -1;
        internal int newestItem;
        internal List<UIRecipeSlot> recipeSlots;
        internal List<UITileSlot> tileSlots;
        internal List<int> craftingTiles;
        internal bool updateNeeded;
        internal int slowUpdateNeeded;
        internal int resultCount;

        private int _lastDownstreamQueryType = -1;
        private Dictionary<int, RecipeDerivationEngine.DerivationInfo> _cachedDownstreamRecipes;

        internal int Tile
        {
            get => tile;
            set
            {
                if (tile != value) updateNeeded = true;
                tile = value;
                if (tileSlots != null)
                {
                    foreach (var slot in tileSlots)
                    {
                        slot.selected = (slot.tile == value);
                    }
                }
                tileIsItemsThatPlaceThisTileInstead = false;
            }
        }

        internal static string RBText(string key, string category = "RecipeCatalogueUI")
        {
            return RBLanguage.GetText(category, key);
        }

        public RecipeCatalogueUI()
        {
            instance = this;
            recipeSlots = new List<UIRecipeSlot>();
            tileSlots = new List<UITileSlot>();
        }

        internal UIElement CreateRecipeCataloguePanel()
        {
            mainPanel = new UIPanel();
            mainPanel.SetPadding(6f);
            mainPanel.BackgroundColor = color;
            mainPanel.Top.Set(20f, 0f);
            mainPanel.Height.Set(-20f, 1f);
            mainPanel.Width.Set(0f, 1f);

            queryItem = new UIRecipeCatalogueQueryItemSlot(new Item());
            queryItem.Left.Set(10f, 0f);
            queryItem.Top.Set(4f, 0f);
            queryItem.emptyHintText = RBText("EmptyQuerySlotHint");
            mainPanel.Append(queryItem);

            // 查询历史前进/后退按钮（对齐原版）
            UISilentImageButton historyBackButton = new UISilentImageButton(RBTextures.HistoryBack, RBText("HistoryBackwardTooltip"));
            historyBackButton.Left.Set(10f, 0f);
            historyBackButton.Top.Set(46f, 0f);
            historyBackButton.OnLeftClick += (evt, el) => (queryItem as UIRecipeCatalogueQueryItemSlot)?.GoBackInHistory();
            mainPanel.Append(historyBackButton);

            UISilentImageButton historyForwardButton = new UISilentImageButton(RBTextures.HistoryForward, RBText("HistoryForwardTooltip"));
            historyForwardButton.Left.Set(34f, 0f);
            historyForwardButton.Top.Set(46f, 0f);
            historyForwardButton.OnLeftClick += (evt, el) => (queryItem as UIRecipeCatalogueQueryItemSlot)?.GoForwardInHistory();
            mainPanel.Append(historyForwardButton);

            RadioButtonGroup = new UIRadioButtonGroup();
            RadioButtonGroup.Left.Pixels = 60f;
            RadioButtonGroup.Top.Pixels = 4f;
            RadioButtonGroup.Width.Set(180f, 0f);

            UIRadioButton allRecipesRadio = new UIRadioButton(RBText("AllRecipes"), 1f);
            NearbyIngredientsRadioButton = new UIRadioButton(RBText("NearbyChests"), 1f);
            RadioButtonGroup.Add(allRecipesRadio);
            RadioButtonGroup.Add(NearbyIngredientsRadioButton);
            mainPanel.Append(RadioButtonGroup);
            allRecipesRadio.Selected = true;

            TileLookupRadioButton = new UICheckbox(RBText("Tile"), "");
            TileLookupRadioButton.Top.Set(44f, 0f);
            TileLookupRadioButton.Left.Set(60f, 0f);
            TileLookupRadioButton.SetText("  " + RBText("Tile"));
            TileLookupRadioButton.OnSelectedChanged += (s, e) =>
            {
                ToggleTileChooser(TileLookupRadioButton.Selected);
                updateNeeded = true;
            };
            mainPanel.Append(TileLookupRadioButton);

            NearbyIngredientsRadioButton.OnSelectedChanged += (s, e) => { updateNeeded = true; };

            itemNameFilter = new NewUITextBox(RBLanguage.GetText("Common", "FilterByName"));
            itemNameFilter.OnTextChanged += () => { ValidateItemFilter(); updateNeeded = true; };
            itemNameFilter.OnTabPressed += () => itemDescriptionFilter.Focus();
            itemNameFilter.Top.Pixels = 0f;
            itemNameFilter.Left.Set(-202f, 1f);
            itemNameFilter.Width.Set(150f, 0f);
            itemNameFilter.Height.Set(25f, 0f);
            mainPanel.Append(itemNameFilter);

            itemDescriptionFilter = new NewUITextBox(RBLanguage.GetText("Common", "FilterByTooltip"));
            itemDescriptionFilter.OnTextChanged += () => { updateNeeded = true; };
            itemDescriptionFilter.OnTabPressed += () => itemNameFilter.Focus();
            itemDescriptionFilter.Top.Pixels = 30f;
            itemDescriptionFilter.Left.Set(-202f, 1f);
            itemDescriptionFilter.Width.Set(150f, 0f);
            itemDescriptionFilter.Height.Set(25f, 0f);
            mainPanel.Append(itemDescriptionFilter);

            recipeGridPanel = new UIPanel();
            recipeGridPanel.SetPadding(6f);
            recipeGridPanel.Top.Pixels = 120f;
            recipeGridPanel.Width.Set(-52f, 1f);
            recipeGridPanel.Height.Set(-170f, 1f);
            recipeGridPanel.BackgroundColor = Color.CornflowerBlue * 0.3f;
            mainPanel.Append(recipeGridPanel);

            recipeGrid = new UIGrid();
            recipeGrid.alternateSort = ItemGridSort;
            recipeGrid.Width.Set(-20f, 1f);
            recipeGrid.Height.Set(0f, 1f);
            recipeGrid.ListPadding = 2f;
            recipeGridPanel.Append(recipeGrid);

            FixedUIScrollbar scrollbar = new FixedUIScrollbar(RecipeBrowserUI.instance.userInterface);
            scrollbar.SetView(100f, 1000f);
            scrollbar.Height.Set(0f, 1f);
            scrollbar.Left.Set(-20f, 1f);
            recipeGridPanel.Append(scrollbar);
            recipeGrid.SetScrollbar(scrollbar);

            recipeInfo = new UIRecipeInfo();
            recipeInfo.Top.Set(-48f, 1f);
            recipeInfo.Width.Set(-50f, 1f);
            recipeInfo.Height.Set(50f, 0f);
            mainPanel.Append(recipeInfo);

            UIPanel lootPanel = new UIPanel();
            lootPanel.SetPadding(6f);
            lootPanel.Top.Pixels = 0f;
            lootPanel.Width.Set(50f, 0f);
            lootPanel.Left.Set(-50f, 1f);
            lootPanel.Height.Set(-16f, 1f);
            lootPanel.BackgroundColor = Color.CornflowerBlue * 0.3f;
            mainPanel.Append(lootPanel);

            lootSourceGrid = new UIGrid();
            lootSourceGrid.Width.Set(0f, 1f);
            lootSourceGrid.Height.Set(0f, 1f);
            lootSourceGrid.ListPadding = 2f;
            lootSourceGrid.drawArrows = true;
            lootPanel.Append(lootSourceGrid);

            InvisibleFixedUIScrollbar lootScroll = new InvisibleFixedUIScrollbar(RecipeBrowserUI.instance.userInterface);
            lootScroll.SetView(100f, 1000f);
            lootScroll.Height.Set(0f, 1f);
            lootScroll.Left.Set(-20f, 1f);
            lootSourceGrid.SetScrollbar(lootScroll);

            tileChooserPanel = new UIPanel();
            tileChooserPanel.SetPadding(6f);
            tileChooserPanel.Top.Pixels = 120f;
            tileChooserPanel.Width.Set(50f, 0f);
            tileChooserPanel.Height.Set(-170f, 1f);
            tileChooserPanel.BackgroundColor = Color.CornflowerBlue * 0.3f;

            uniqueCheckbox = new UICycleImage(RBTextures.UniqueTile, 2, new string[] { RBText("ShowInheritedRecipes"), RBText("ShowUniqueRecipes") }, 36, 20);
            uniqueCheckbox.Top.Set(0f, 0f);
            uniqueCheckbox.Left.Set(1f, 0f);
            uniqueCheckbox.CurrentState = 1;
            uniqueCheckbox.OnStateChanged += (s, e) => { updateNeeded = true; };
            tileChooserPanel.Append(uniqueCheckbox);

            tileChooserGrid = new UIGrid();
            tileChooserGrid.Width.Set(0f, 1f);
            tileChooserGrid.Height.Set(-24f, 1f);
            tileChooserGrid.Top.Set(24f, 0f);
            tileChooserGrid.ListPadding = 2f;
            tileChooserGrid.drawArrows = true;
            tileChooserPanel.Append(tileChooserGrid);

            InvisibleFixedUIScrollbar tileScroll = new InvisibleFixedUIScrollbar(RecipeBrowserUI.instance.userInterface);
            tileScroll.SetView(100f, 1000f);
            tileScroll.Height.Set(0f, 1f);
            tileScroll.Left.Set(-20f, 1f);
            tileChooserGrid.SetScrollbar(tileScroll);

            updateNeeded = true;
            return mainPanel;
        }

        internal void ToggleTileChooser(bool show = true)
        {
            if (show)
            {
                recipeGridPanel.Width.Set(-104f, 1f);
                recipeGridPanel.Left.Set(52f, 0f);
                mainPanel.Append(tileChooserPanel);
            }
            else
            {
                recipeGridPanel.Width.Set(-52f, 1f);
                recipeGridPanel.Left.Set(0f, 0f);
                mainPanel.RemoveChild(tileChooserPanel);
                Tile = -1;
            }
            recipeGridPanel.Recalculate();
        }

        internal void CloseButtonClicked()
        {
            RadioButtonGroup.ButtonClicked(0);
            if (queryItem.real && queryItem.item.stack > 0)
            {
                queryItem.ReplaceWithFake(0);
            }
            queryLootItem = null;
            _cachedDownstreamRecipes = null;
            _lastDownstreamQueryType = -1;
            updateNeeded = true;
        }

        internal void Update()
        {
            hoveredIndex = -1;
            RecipeBrowser.UIElements.UIItemSlot.hoveredItem = null;
            UpdateGrid();

            if (pendingQueryHowToCraftTile != -1)
            {
                queryItem?.ReplaceWithFake(0);
                Tile = pendingQueryHowToCraftTile;
                pendingQueryHowToCraftTile = -1;
                tileIsItemsThatPlaceThisTileInstead = true;
                if (TileLookupRadioButton != null) TileLookupRadioButton.Selected = true;
                if (pendingQueryHowToCraftTileShouldGoto)
                {
                    tileChooserGrid?.Goto((UIElement x) => x is UITileSlot uITileSlot && uITileSlot.tile == Tile, center: true);
                    pendingQueryHowToCraftTileShouldGoto = false;
                }
                updateNeeded = true;
            }
        }

        private void UpdateGrid()
        {
            using (RBProfiler.Step("RecipeCatalogueUI.UpdateGrid"))
            {
                using (RBProfiler.Step("RefreshAvailableRecipesCache"))
                {
                    UIRecipeSlot.RefreshAvailableRecipesCache();
                }

                if (Recipe.numRecipes != recipeSlots.Count)
                {
                    using (RBProfiler.Step("InitAllRecipeSlotsAndTiles"))
                    {
                        recipeSlots.Clear();
                        for (int i = 0; i < Recipe.numRecipes; i++)
                        {
                            recipeSlots.Add(new UIRecipeSlot(i));
                        }
                        tileChooserGrid.Clear();
                        tileSlots.Clear();
                        Dictionary<int, int> tileCount = new Dictionary<int, int>();
                        for (int j = 0; j < Recipe.numRecipes; j++)
                        {
                            Recipe r = Main.recipe[j];
                            if (r != null && r.requiredTile >= 0)
                            {
                                tileCount.TryGetValue(r.requiredTile, out var cur);
                                tileCount[r.requiredTile] = cur + 1;
                            }
                        }
                        foreach (var kvp in tileCount.OrderBy(kv => kv.Value))
                        {
                            UITileSlot tSlot = new UITileSlot(kvp.Key, kvp.Value);
                            tileChooserGrid.Add(tSlot);
                            tileSlots.Add(tSlot);
                        }
                        craftingTiles = tileCount.Select(kv => kv.Key).ToList();
                        RecipeBrowserUI.instance?.UpdateFavoritedPanel();
                    }
                }

                if (RecipeBrowserUI.instance == null || !RecipeBrowserUI.instance.ShowRecipeBrowser || RecipeBrowserUI.instance.CurrentPanel != 0)
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

                List<int> groups = new List<int>();
                if (!queryItem.item.IsAir && queryItem.item.stack > 0)
                {
                    int qType = queryItem.CanonicalItemType;
                    foreach (var rg in RecipeGroup.recipeGroups)
                    {
                        if (rg.Value.ValidItems.Contains(qType))
                        {
                            groups.Add(rg.Key);
                        }
                    }

                    if (_cachedDownstreamRecipes == null || _lastDownstreamQueryType != qType)
                    {
                        _lastDownstreamQueryType = qType;
                        _cachedDownstreamRecipes = RecipeDerivationEngine.ComputeDownstreamRecipes(qType, maxDepth: 10);
                    }
                }
                else
                {
                    _lastDownstreamQueryType = -1;
                    _cachedDownstreamRecipes = null;
                }

                using (RBProfiler.Step("UpdateLootSourceGrid"))
                {
                    lootSourceGrid.Clear();
                    if (queryLootItem != null)
                    {
                        int qType = queryLootItem.type;
                        if (LootCache.instance?.lootInfos != null && LootCache.instance.lootInfos.TryGetValue(qType, out var list))
                        {
                            foreach (int nType in list)
                            {
                                if (nType > 0)
                                {
                                    NPC dummy = new NPC();
                                    dummy.SetDefaults(nType);
                                    UINPCSlot nSlot = new UINPCSlot(dummy);
                                    lootSourceGrid._items.Add(nSlot);
                                    lootSourceGrid._innerList.Append(nSlot);
                                }
                            }
                        }
                    }
                    lootSourceGrid.UpdateOrder();
                    lootSourceGrid._innerList.Recalculate();
                }

                RefreshNearbyChestCache();

                using (RBProfiler.Step("FilterAllRecipes (PassRecipeFilters)"))
                {
                    recipeGrid.Clear();
                    resultCount = 0;
                    for (int rIndex = 0; rIndex < Recipe.numRecipes; rIndex++)
                    {
                        Recipe r = Main.recipe[rIndex];
                        if (!PassRecipeFilters(recipeSlots[rIndex], r, groups)) continue;

                        UIRecipeSlot slot = recipeSlots[rIndex];
                        if (newestItem > 0)
                        {
                            slot.recentlyDiscovered = r.requiredItem != null && r.requiredItem.Any(x => x?.type == newestItem);
                        }
                        recipeGrid._items.Add(slot);
                        recipeGrid._innerList.Append(slot);
                        resultCount++;
                    }
                }

                using (RBProfiler.Step("recipeGrid.UpdateOrder & Recalculate"))
                {
                    using (RBProfiler.Step("recipeGrid.UpdateOrder"))
                    {
                        recipeGrid.UpdateOrder();
                    }
                    using (RBProfiler.Step("recipeGrid._innerList.Recalculate"))
                    {
                        recipeGrid._innerList.Recalculate();
                    }
                }
            }
        }

        private int ItemGridSort(UIElement x, UIElement y)
        {
            if (x is UIPanel) return -1;
            if (y is UIPanel) return 1;
            if (x is UIRecipeSlot r1 && y is UIRecipeSlot r2)
            {
                int ign = r1.CompareToIgnoreIndex(r2);
                if (ign != 0) return ign;

                var selSort = SharedUI.instance?.SelectedSort;
                if (selSort != null)
                {
                    if (selSort.recipeSort != null)
                    {
                        int rCmp = selSort.recipeSort(Main.recipe[r1.index], Main.recipe[r2.index]);
                        if (rCmp != 0) return rCmp;
                    }
                    else if (selSort.sort != null)
                    {
                        int iCmp = selSort.sort(r1.item, r2.item);
                        if (iCmp != 0) return iCmp;
                    }
                }
                return r1.index.CompareTo(r2.index);
            }
            return 0;
        }

        private bool PassRecipeFilters(UIRecipeSlot recipeSlot, Recipe recipe, List<int> groups)
        {
            if (recipe == null || recipe.createItem == null) return false;

            // Mod 过滤主开关（对齐原版）：选中某模组时，仅当循环过滤器开启时按 recipeBelongs 判定，否则结果物品必须属于该模组
            if (RecipeBrowserUI.ModIndex != 0 && RecipeBrowserUI.instance?.mods != null && RecipeBrowserUI.ModIndex < RecipeBrowserUI.instance.mods.Length)
            {
                if (SharedUI.instance?.ModFilterByFilter == null || !SharedUI.instance.ModFilterByFilter.button.selected)
                {
                    string targetMod = RecipeBrowserUI.instance.mods[RecipeBrowserUI.ModIndex];
                    if (targetMod == "Terraria")
                    {
                        if (recipe.createItem.type >= ItemID.Count) return false;
                    }
                    else
                    {
                        var modItem = TPML.Content.ItemLoader.GetModItem(recipe.createItem.type);
                        if (modItem?.Mod?.Name != targetMod) return false;
                    }
                }
                else if (SharedUI.instance.ModFilterByFilter.recipeBelongs != null && !SharedUI.instance.ModFilterByFilter.recipeBelongs(recipe))
                {
                    return false;
                }
            }

            if (NearbyIngredientsRadioButton.Selected && !PassNearbyChestFilter(recipe))
            {
                return false;
            }

            if (Tile > -1)
            {
                if (tileIsItemsThatPlaceThisTileInstead)
                {
                    if (recipe.createItem.createTile != Tile) return false;
                }
                else
                {
                    List<int> adjTiles = (uniqueCheckbox?.CurrentState == 0)
                        ? Utilities.PopulateAdjTilesForTile(Tile)
                        : new List<int> { Tile };

                    if (!adjTiles.Contains(recipe.requiredTile))
                    {
                        return false;
                    }
                }
            }

            if (!queryItem.item.IsAir)
            {
                int rIdx = recipeSlot.index;
                if (_cachedDownstreamRecipes != null)
                {
                    if (!_cachedDownstreamRecipes.TryGetValue(rIdx, out var derivInfo))
                    {
                        return false;
                    }
                    recipeSlot.derivationDepth = derivInfo.Depth;
                    recipeSlot.derivationPath = derivInfo.Path;
                }
                else
                {
                    int qType = queryItem.CanonicalItemType;
                    bool match = (recipe.createItem.type == qType) || (recipe.requiredItem != null && recipe.requiredItem.Any(ing => ing != null && ing.type == qType)) || (recipe.acceptedGroups != null && recipe.acceptedGroups.Intersect(groups).Any());
                    if (!match) return false;
                    recipeSlot.derivationDepth = (recipe.createItem.type == qType) ? 0 : 1;
                    recipeSlot.derivationPath = new List<int> { qType, recipe.createItem.type };
                }
            }
            else
            {
                recipeSlot.derivationDepth = 0;
                recipeSlot.derivationPath = null;
            }

            Category selectedCategory = SharedUI.instance?.SelectedCategory;
            if (selectedCategory != null && !selectedCategory.belongs(recipe.createItem) && !selectedCategory.subCategories.Any(x => x.belongs(recipe.createItem)))
            {
                return false;
            }

            if (SharedUI.instance?.availableFilters != null)
            {
                foreach (var filter in SharedUI.instance.availableFilters)
                {
                    // 注：原版有"未勾选 DisabledFilter 时隐藏禁用配方"逻辑，依赖 tML 的 Recipe.Disabled 扩展属性；
                    // TPML 原版 Recipe 无 Disabled 概念（无禁用配方数据源），该过滤在 TPML 生态下无意义，故省略。
                    if (!filter.button.selected) continue;
                    if (!filter.belongs(recipe.createItem)) return false;
                    if (filter.recipeBelongs != null && !filter.recipeBelongs(recipe)) return false;

                    if (filter == SharedUI.instance.ObtainableFilter)
                    {
                        recipeSlot.CraftPathNeeded();
                        if ((!recipeSlot.craftPathCalculated && !recipeSlot.craftPathsCalculated) || recipeSlot.craftPaths == null || recipeSlot.craftPaths.Count <= 0)
                        {
                            return false;
                        }
                    }
                    if (filter == SharedUI.instance.CraftableFilter)
                    {
                        int idx = recipeSlot.index;
                        bool avail = false;
                        for (int i = 0; i < Main.numAvailableRecipes; i++)
                        {
                            if (idx == Main.availableRecipe[i]) { avail = true; break; }
                        }
                        if (!avail) return false;
                    }
                }
            }

            string filterName = itemNameFilter.currentString.Trim();
            if (filterName.Length > 0)
            {
                string name = recipe.createItem.Name;
                string localizedName = Lang.GetItemNameValue(recipe.createItem.type);
                string internalName = (recipe.createItem.type > 0 && recipe.createItem.type < ItemID.Count) 
                    ? ItemID.Search.GetName(recipe.createItem.type) 
                    : (TPML.Content.ItemLoader.GetModItem(recipe.createItem.type)?.Name ?? "");
                string fullName = (recipe.createItem.type >= ItemID.Count) 
                    ? (TPML.Content.ItemLoader.GetModItem(recipe.createItem.type)?.FullName ?? "") 
                    : "";
                string displayName = (recipe.createItem.type >= ItemID.Count) 
                    ? TPML.Content.ItemLoader.GetDisplayName(recipe.createItem.type) 
                    : "";

                if (!PinyinHelper.Matches(name, filterName) &&
                    !PinyinHelper.Matches(localizedName, filterName) &&
                    !PinyinHelper.Matches(displayName, filterName) &&
                    !PinyinHelper.Matches(internalName, filterName) &&
                    !PinyinHelper.Matches(fullName, filterName))
                {
                    return false;
                }
            }

            string filterDesc = itemDescriptionFilter.currentString.Trim();
            if (filterDesc.Length > 0)
            {
                string tooltips = GetTooltipsAsString(recipe.createItem.ToolTip);
                if (recipe.createItem.type >= ItemID.Count)
                {
                    string modTip = TPML.Content.ItemLoader.GetTooltip(recipe.createItem.type);
                    if (!string.IsNullOrEmpty(modTip))
                    {
                        tooltips = tooltips + "\n" + modTip;
                    }
                }
                if (string.IsNullOrEmpty(tooltips) || !PinyinHelper.Matches(tooltips, filterDesc))
                {
                    return false;
                }
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

        private HashSet<int> nearbyChestCache;

        private void RefreshNearbyChestCache()
        {
            if (!NearbyIngredientsRadioButton.Selected)
            {
                nearbyChestCache = null;
                return;
            }

            using (RBProfiler.Step("RefreshNearbyChestCache"))
            {
                nearbyChestCache = new HashSet<int>();
                for (int i = 0; i < 1000; i++)
                {
                    Chest c = Main.chest[i];
                    if (c == null || Chest.IsLocked(c.x, c.y)) continue;
                    Vector2 diff = new Vector2(c.x * 16 + 16, c.y * 16 + 16) - Main.LocalPlayer.Center;
                    if (diff.Length() < 960f)
                    {
                        foreach (var it in c.item)
                        {
                            if (it != null && !it.IsAir) nearbyChestCache.Add(it.type);
                        }
                    }
                }

                foreach (var it in Main.LocalPlayer.bank.item) if (it != null && !it.IsAir) nearbyChestCache.Add(it.type);
                foreach (var it in Main.LocalPlayer.bank2.item) if (it != null && !it.IsAir) nearbyChestCache.Add(it.type);
                foreach (var it in Main.LocalPlayer.bank3.item) if (it != null && !it.IsAir) nearbyChestCache.Add(it.type);
                foreach (var it in Main.LocalPlayer.bank4.item) if (it != null && !it.IsAir) nearbyChestCache.Add(it.type);
                foreach (var it in Main.LocalPlayer.inventory) if (it != null && !it.IsAir) nearbyChestCache.Add(it.type);

                try
                {
                    var fusionItems = TPML.Content.Fusion.InventoryFusionManager.GetAllFusionItems(Main.LocalPlayer);
                    if (fusionItems != null)
                    {
                        foreach (var fit in fusionItems)
                        {
                            if (fit != null && !fit.IsAir) nearbyChestCache.Add(fit.type);
                        }
                    }
                }
                catch { }

                if (queryItem.item != null && !queryItem.item.IsAir) nearbyChestCache.Add(queryItem.item.type);
            }
        }

        private bool PassNearbyChestFilter(Recipe recipe)
        {
            if (recipe.requiredItem == null || nearbyChestCache == null) return true;
            foreach (var item in recipe.requiredItem)
            {
                if (item != null && !item.IsAir && !nearbyChestCache.Contains(item.type))
                {
                    return false;
                }
            }
            return true;
        }

        internal void SetRecipe(int index)
        {
            using (RBProfiler.Step($"SetRecipe #{index} ({Main.recipe[index]?.createItem?.Name})"))
            {
                selectedIndex = -1;
                recipeInfo.craftingIngredientsGrid.Clear();
                foreach (var slot in recipeSlots) slot.selected = false;

                UIRecipeSlot selectedSlot = recipeSlots[index];
                selectedSlot.selected = true;
                selectedIndex = index;

                List<UIIngredientSlot> ingList = new List<UIIngredientSlot>();
                Recipe r = Main.recipe[index];
                for (int i = 0; i < r.requiredItem.Length; i++)
                {
                    Item req = r.requiredItem[i];
                    if (req == null || req.IsAir) continue;
                    Item clone = req.Clone();
                    UIIngredientSlot slot = new UIIngredientSlot(clone, i);
                    ingList.Add(slot);
                    OverrideForGroups(r, slot.item);
                }
                recipeInfo.craftingIngredientsGrid.AddRange(ingList);

                using (RBProfiler.Step("CraftUI.SetRecipe"))
                {
                    CraftUI.instance?.SetRecipe(index);
                }

                using (RBProfiler.Step("UpdateLootGrid"))
                {
                    UpdateLootGrid(r.createItem);
                }

                if (duplicationButton != null)
                {
                    mainPanel.RemoveChild(duplicationButton);
                    duplicationButton = null;
                }

                if (Main.GameMode == 3 && RecipePath.ItemFullyResearched(r.createItem.type))
                {
                    duplicationButton = new UIJourneyDuplicateButton(new CraftPath.JourneyDuplicateItemNode(r.createItem.type, r.createItem.maxStack, 0, null, null));
                    duplicationButton.Top.Set(-18f, 1f);
                    duplicationButton.Left.Set(-36f, 1f);
                    mainPanel.Append(duplicationButton);
                }
            }
        }

        internal void UpdateLootGrid(Item item)
        {
            lootSourceGrid.Clear();
            if (item != null && !item.IsAir)
            {
                int qType = item.type;
                if (LootCache.instance?.lootInfos != null && LootCache.instance.lootInfos.TryGetValue(qType, out var list))
                {
                    foreach (int nType in list)
                    {
                        if (nType > 0)
                        {
                            NPC dummy = new NPC();
                            dummy.SetDefaults(nType);
                            UINPCSlot nSlot = new UINPCSlot(dummy);
                            lootSourceGrid._items.Add(nSlot);
                            lootSourceGrid._innerList.Append(nSlot);
                        }
                    }
                }
            }
            lootSourceGrid.UpdateOrder();
            lootSourceGrid._innerList.Recalculate();
        }

        public static void OverrideForGroups(Recipe recipe, Item item)
        {
            if (recipe == null || item == null) return;
            string text = null;
            recipe.ProcessGroupsForText(item.type, out text);
            if (!string.IsNullOrEmpty(text))
            {
                item.SetNameOverride(text);
            }
        }

        public static string OverrideForGroups(Recipe recipe, int itemType)
        {
            if (recipe == null) return null;
            string result = null;
            recipe.ProcessGroupsForText(itemType, out result);
            return result;
        }

        private void ValidateItemFilter()
        {
            // 搜索防呆（对齐原版）：结果为空时回退删除最后一个输入字符
            if (itemNameFilter == null || itemNameFilter.currentString.Length == 0 || resultCount != 0)
            {
                updateNeeded = true;
                return;
            }
            itemNameFilter.SetText(itemNameFilter.currentString.Substring(0, itemNameFilter.currentString.Length - 1));
            updateNeeded = true;
        }

        internal void InvalidateExtendedCraft()
        {
            if (!RecipePath.extendedCraft) return;
            if (recipeSlots != null)
            {
                foreach (var s in recipeSlots)
                {
                    s.craftPathNeeded = false;
                    s.craftPathCalculated = false;
                    s.craftPathsCalculated = false;
                    s.craftPaths = null;
                }
            }
            if (CraftUI.instance != null) CraftUI.instance.craftPathsUpToDate = false;
        }
    }
}
