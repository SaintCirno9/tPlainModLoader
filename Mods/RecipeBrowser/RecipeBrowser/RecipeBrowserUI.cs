using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using RecipeBrowser.UIElements;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;

namespace RecipeBrowser
{
    public class RecipeBrowserUI : UIModState
    {
        public static RecipeBrowserUI instance;

        internal const int RecipeCatalogue = 0;
        internal const int Craft = 1;
        internal const int ItemCatalogue = 2;
        internal const int Bestiary = 3;
        internal const int Help = 4;

        internal TabController tabController;
        internal UIDragableElement mainPanel;
        internal UIDragablePanel favoritePanel;
        internal UICycleImage HideUnlessInventoryToggle;
        internal UICycleImage ShowOtherPlayersFavoritesToggle;
        internal UIHoverImageButton closeFavoritePanelButton;
        internal UIHoverImageButton closeButton;
        internal UIHoverImageButtonMod modFilterButton;



        internal RecipeCatalogueUI recipeCatalogueUI;
        internal CraftUI craftUI;
        internal ItemCatalogueUI itemCatalogueUI;
        internal BestiaryUI bestiaryUI;
        internal HelpUI helpUI;

        internal bool[] foundItems;
        internal string[] mods;
        internal ModFilterDropdown ModFilterDropdown;

        public bool ForceShowFavoritePanel;
        public bool ForceHideFavoritePanel;
        private bool showFavoritePanel;
        private bool showRecipeBrowser;

        private static int modIndex;
        internal static int modIndexPrevious;
        internal static int modHoverIndex = -1;
        internal bool lastMainPlayerInventory;
        internal bool favoritePanelUpdateNeeded;
        internal int npcArrow = -1;

        internal List<int> localPlayerFavoritedRecipes => RecipeBrowserPlayer.GetLocalFavoritedRecipes();

        public bool ShowFavoritePanel
        {
            get => showFavoritePanel;
            set
            {
                if (value) Append(favoritePanel);
                else RemoveChild(favoritePanel);

                if (value) ForceHideFavoritePanel = false;
                if (!value) ForceShowFavoritePanel = false;
                showFavoritePanel = value;
            }
        }

        public bool ShowRecipeBrowser
        {
            get => showRecipeBrowser;
            set
            {
                if (value)
                {
                    RefreshModList();
                    Recipe.UpdateRecipeList();
                    if (mainPanel != null)
                    {
                        var config = RecipeBrowserClientConfig.Instance;
                        mainPanel.HAlign = 0.5f;
                        mainPanel.VAlign = 0.5f;

                        if (config == null || config.RecipeBrowserPosition == new Vector2(-1, -1) || config.RecipeBrowserPosition == new Vector2(400, 400))
                        {
                            mainPanel.Left.Set(0f, 0f);
                            mainPanel.Top.Set(0f, 0f);
                        }
                        else
                        {
                            mainPanel.Left.Set(config.RecipeBrowserPosition.X, 0f);
                            mainPanel.Top.Set(config.RecipeBrowserPosition.Y, 0f);
                        }
                        mainPanel.Recalculate();
                    }
                    Append(mainPanel);
                }
                else
                {
                    UnblockInput();
                    RemoveChild(mainPanel);
                }
                showRecipeBrowser = value;
            }
        }

        public int CurrentPanel => tabController?.currentPanel ?? 0;

        internal static int ModIndex
        {
            get => modIndex;
            set
            {
                modIndex = value;
                SharedUI.instance?.ModFilterByFilter?.FormatText(instance?.mods != null && modIndex < instance.mods.Length ? instance.mods[modIndex] : "");
                if (SharedUI.instance != null) SharedUI.instance.updateNeeded = true;
            }
        }

        internal static string RBText(string key, string category = "RecipeBrowserUI")
        {
            return RBLanguage.GetText(category, key);
        }

        public RecipeBrowserUI(UserInterface ui)
            : base(ui)
        {
            instance = this;
            mods = new string[] { "Terraria" };
        }

        public void RefreshModList()
        {
            var activeMods = new List<string>();
            foreach (var mod in TPML.Content.ModContent.Mods)
            {
                if (mod == null || string.IsNullOrEmpty(mod.Name)) continue;
                bool hasItems = TPML.Content.ItemLoader.Items.Any(it => it.Mod?.Name == mod.Name);
                bool hasRecipes = false;
                for (int i = 0; i < Recipe.numRecipes; i++)
                {
                    var r = Main.recipe[i];
                    if (r?.createItem != null && r.createItem.type >= ItemID.Count)
                    {
                        var modItem = TPML.Content.ItemLoader.GetModItem(r.createItem.type);
                        if (modItem?.Mod?.Name == mod.Name)
                        {
                            hasRecipes = true;
                            break;
                        }
                    }
                }
                if (hasItems || hasRecipes)
                {
                    activeMods.Add(mod.Name);
                }
            }
            mods = new[] { "Terraria" }.Concat(activeMods.Distinct()).ToArray();
            if (modIndex >= mods.Length)
            {
                ModIndex = 0;
            }
            UpdateModFilterUI();
        }

        public override void OnInitialize()
        {
            mainPanel = new UIDragableElement(dragable: true, resizeableX: true, resizeableY: true);
            mainPanel.HAlign = 0.5f;
            mainPanel.VAlign = 0.5f;
            mainPanel.MinWidth.Set(450f, 0f);
            mainPanel.MaxWidth.Set(1400f, 0f);
            mainPanel.MinHeight.Set(320f, 0f);
            mainPanel.MaxHeight.Set(1200f, 0f);

            var config = RecipeBrowserClientConfig.Instance;
            float width = 640f;
            float height = 520f;

            if (config != null && config.RecipeBrowserSize.X >= 450f && config.RecipeBrowserSize.Y >= 300f)
            {
                width = config.RecipeBrowserSize.X;
                height = config.RecipeBrowserSize.Y;
            }

            mainPanel.Width.Set(width, 0f);
            mainPanel.Height.Set(height, 0f);

            float offsetX = 0f;
            float offsetY = 0f;

            if (config != null && config.RecipeBrowserPosition != new Vector2(-1, -1) &&
                config.RecipeBrowserPosition != new Vector2(400, 400))
            {
                offsetX = config.RecipeBrowserPosition.X;
                offsetY = config.RecipeBrowserPosition.Y;
            }

            mainPanel.Left.Set(offsetX, 0f);
            mainPanel.Top.Set(offsetY, 0f);

            new SharedUI();
            recipeCatalogueUI = new RecipeCatalogueUI();
            craftUI = new CraftUI();
            itemCatalogueUI = new ItemCatalogueUI();
            bestiaryUI = new BestiaryUI();
            helpUI = new HelpUI();

            SharedUI.instance.Initialize();

            UIElement p0 = recipeCatalogueUI.CreateRecipeCataloguePanel();
            mainPanel.Append(p0);
            UIElement p1 = craftUI.CreateCraftPanel();
            mainPanel.Append(p1);
            UIElement p2 = itemCatalogueUI.CreateItemCataloguePanel();
            mainPanel.Append(p2);
            UIElement p3 = bestiaryUI.CreateBestiaryPanel();
            mainPanel.Append(p3);
            UIElement p4 = helpUI.CreateHelpPanel();

            tabController = new TabController(mainPanel);
            tabController.AddPanel(p0);
            tabController.AddPanel(p1);
            tabController.AddPanel(p2);
            tabController.AddPanel(p3);
            tabController.AddPanel(p4);

            mainPanel.AddDragTarget(p0);
            mainPanel.AddDragTarget(recipeCatalogueUI.recipeInfo);
            mainPanel.AddDragTarget(recipeCatalogueUI.RadioButtonGroup);
            mainPanel.AddDragTarget(p1);
            craftUI.additionalDragTargets.ForEach(x => mainPanel.AddDragTarget(x));
            mainPanel.AddDragTarget(p2);
            itemCatalogueUI.additionalDragTargets.ForEach(x => mainPanel.AddDragTarget(x));
            mainPanel.AddDragTarget(p3);
            mainPanel.AddDragTarget(p4);
            mainPanel.AddDragTarget(helpUI.message);

            // 标签按钮添加
            AddTabButton(10f, RecipeCatalogueUI.color, RBText("Recipes"), 0);
            AddTabButton(85f, CraftUI.color, RBText("Craft"), 1);
            AddTabButton(160f, ItemCatalogueUI.color, RBText("Items"), 2, () => itemCatalogueUI.updateNeeded = true);
            AddTabButton(235f, BestiaryUI.color, RBText("Bestiary"), 3);
            AddTabButton(-155f, HelpUI.color, RBText("Help"), 4, fromRight: true);

            // 右侧关闭与辅助按钮条
            UIPanel rightTab = new UIBottomlessPanel();
            rightTab.SetPadding(0f);
            rightTab.Left.Set(-80f, 1f);
            rightTab.Width.Set(70f, 0f);
            rightTab.Height.Set(22f, 0f);
            rightTab.BackgroundColor = Color.DarkRed * 0.5f;

            Texture2D modTex = RBTextures.FilterMod ?? TextureAssets.MagicPixel.Value;
            Texture2D modColorableTex = RBTextures.FilterModColorable ?? modTex;
            modFilterButton = new UIHoverImageButtonMod(modTex, modColorableTex, RBText("ModFilter") + ": " + RBText("All"));
            modFilterButton.Left.Set(-56f, 1f);
            modFilterButton.VAlign = 0.5f;
            modFilterButton.OnLeftClick += (evt, el) => ModFilterButton_OnClick(evt, el);
            modFilterButton.OnRightClick += (evt, el) => ModFilterButton_OnRightClick(evt, el);
            modFilterButton.OnMiddleClick += (evt, el) => ModFilterButton_OnMiddleClick(evt, el);
            rightTab.Append(modFilterButton);

            Texture2D closeTex = RBTextures.CloseButton;
            closeButton = new UIHoverImageButton(closeTex, RBText("Close"));
            closeButton.OnLeftClick += (evt, el) => CloseButtonClicked(evt, el);
            closeButton.Left.Set(-26f, 1f);
            closeButton.VAlign = 0.5f;
            rightTab.Append(closeButton);
            mainPanel.Append(rightTab);

            RefreshModList();
            tabController.SetPanel(0);

            favoritePanel = new UIDragablePanel();
            favoritePanel.SetPadding(6f);
            favoritePanel.Left.Set(-310f, 0f);
            favoritePanel.HAlign = 1f;
            favoritePanel.Top.Set(90f, 0f);
            favoritePanel.Width.Set(415f, 0f);
            favoritePanel.MinWidth.Set(50f, 0f);
            favoritePanel.MaxWidth.Set(600f, 0f);
            favoritePanel.Height.Set(350f, 0f);
            favoritePanel.MinHeight.Set(50f, 0f);
            favoritePanel.MaxHeight.Set(300f, 0f);
            favoritePanel.BackgroundColor = Color.Transparent;

            if (config != null)
            {
                favoritePanel.Left.Set(config.FavoritedRecipePanelPosition.X, 0f);
                favoritePanel.Top.Set(config.FavoritedRecipePanelPosition.Y, 0f);
            }

            closeFavoritePanelButton = new UIHoverImageButton(closeTex, RBLanguage.GetText("FavoritedUI", "Close"));
            closeFavoritePanelButton.OnLeftClick += (evt, el) => CloseFavoritePanelButtonClicked(evt, el);
            closeFavoritePanelButton.Top.Set(0f, 0f);
            closeFavoritePanelButton.Left.Set(-15f, 1f);
            favoritePanel.Append(closeFavoritePanelButton);

            HideUnlessInventoryToggle = new UICycleImage(RBTextures.GetTexture("UIElements/TickOnOff"), 2, new string[]
            {
                RBLanguage.GetText("FavoritedUI", "AlwaysShow"),
                RBLanguage.GetText("FavoritedUI", "ShowWhenInventory")
            }, 16, 12);
            HideUnlessInventoryToggle.Top.Set(20f, 0f);
            HideUnlessInventoryToggle.Left.Set(-15f, 1f);
            HideUnlessInventoryToggle.CurrentState = (config != null && config.OnlyShowFavoritedWhileInInventory) ? 1 : 0;
            HideUnlessInventoryToggle.OnStateChanged += (s, e) =>
            {
                favoritePanelUpdateNeeded = true;
                if (config != null)
                {
                    config.OnlyShowFavoritedWhileInInventory = HideUnlessInventoryToggle.CurrentState == 1;
                    RecipeBrowserClientConfig.SaveConfig();
                }
            };
            favoritePanel.Append(HideUnlessInventoryToggle);
        }

        private void AddTabButton(float left, Color color, string text, int panelIdx, Action extraAction = null, bool fromRight = false)
        {
            UITabButton btn = new UITabButton(color, text, panelIdx);
            if (fromRight) btn.Left.Set(left, 1f);
            else btn.Left.Set(left, 0f);

            btn.OnLeftClick += (evt, el) =>
            {
                tabController.SetPanel(panelIdx);
                extraAction?.Invoke();
            };

            mainPanel.Append(btn);
            tabController.AddButton(btn);
        }

        internal void CloseButtonClicked(UIMouseEvent evt, UIElement listeningElement)
        {
            UnblockInput();
            instance.ShowRecipeBrowser = !instance.ShowRecipeBrowser;
            recipeCatalogueUI.CloseButtonClicked();
            bestiaryUI.CloseButtonClicked();
        }

        internal void CloseFavoritePanelButtonClicked(UIMouseEvent evt, UIElement listeningElement)
        {
            instance.ForceHideFavoritePanel = true;
            instance.ShowFavoritePanel = false;
        }

        private void ModFilterButton_OnClick(UIMouseEvent evt, UIElement listeningElement)
        {
            if (mods == null || mods.Length == 0) return;

            if (IsInputBlocked)
            {
                UnblockInput();
                return;
            }

            CalculatedStyle btnDims = listeningElement.GetDimensions();
            float dropdownWidth = 220f;
            float dropdownHeight = Math.Min(320f, mods.Length * 28f + 16f);

            ModFilterDropdown = new ModFilterDropdown(mods, (i) => i == 0 ? "全部 (All / Terraria)" : mods[i], ModIndex, (selIdx) =>
            {
                ModIndex = selIdx;
                UpdateModFilterUI();
                UnblockInput();
            });

            ModFilterDropdown.Width.Set(dropdownWidth, 0f);
            ModFilterDropdown.Height.Set(dropdownHeight, 0f);

            float leftPos = Math.Max(10f, btnDims.X + btnDims.Width - dropdownWidth);
            float topPos = btnDims.Y + btnDims.Height + 4f;
            if (topPos + dropdownHeight > Main.screenHeight)
            {
                topPos = Math.Max(10f, btnDims.Y - dropdownHeight - 4f);
            }

            ModFilterDropdown.Left.Set(leftPos, 0f);
            ModFilterDropdown.Top.Set(topPos, 0f);

            BlockInput(ModFilterDropdown);
        }

        private void ModFilterButton_OnRightClick(UIMouseEvent evt, UIElement listeningElement)
        {
            if (IsInputBlocked)
            {
                UnblockInput();
            }
            ChangeModIndex(false);
        }

        private void ModFilterButton_OnMiddleClick(UIMouseEvent evt, UIElement listeningElement)
        {
            if (IsInputBlocked)
            {
                UnblockInput();
            }
            ModIndex = 0;
            UpdateModFilterUI();
        }

        private void ChangeModIndex(bool increment)
        {
            if (mods == null || mods.Length == 0) return;
            ModIndex = increment ? (ModIndex + 1) % mods.Length : (ModIndex + mods.Length - 1) % mods.Length;
            UpdateModFilterUI();
        }

        public void UpdateModFilterUI()
        {
            if (modFilterButton != null)
            {
                string modName = (ModIndex == 0 || mods == null || ModIndex >= mods.Length) ? RBText("All") : mods[ModIndex];
                modFilterButton.hoverText = RBText("ModFilter") + ": " + modName;
            }
            if (SharedUI.instance != null) SharedUI.instance.updateNeeded = true;
            if (RecipeCatalogueUI.instance != null) RecipeCatalogueUI.instance.updateNeeded = true;
            if (ItemCatalogueUI.instance != null) ItemCatalogueUI.instance.updateNeeded = true;
        }

        internal void FavoriteChange(int index, bool favorite)
        {
            if (RecipeCatalogueUI.instance?.recipeSlots != null && index < RecipeCatalogueUI.instance.recipeSlots.Count)
            {
                RecipeCatalogueUI.instance.recipeSlots[index].favorited = favorite;
            }
            localPlayerFavoritedRecipes.RemoveAll(x => x == index);
            if (favorite)
            {
                localPlayerFavoritedRecipes.Add(index);
            }
            favoritePanelUpdateNeeded = true;
            if (favorite)
            {
                ShowFavoritePanel = true;
            }
            if (RecipeCatalogueUI.instance != null) RecipeCatalogueUI.instance.updateNeeded = true;
        }

        internal void UpdateFavoritedPanel()
        {
            if (RecipeBrowserClientConfig.Instance != null && HideUnlessInventoryToggle != null)
            {
                HideUnlessInventoryToggle.CurrentState = RecipeBrowserClientConfig.Instance.OnlyShowFavoritedWhileInInventory ? 1 : 0;
            }

            if (HideUnlessInventoryToggle != null && HideUnlessInventoryToggle.CurrentState == 1 && lastMainPlayerInventory != Main.playerInventory && !ForceHideFavoritePanel)
            {
                ShowFavoritePanel = Main.playerInventory;
            }
            lastMainPlayerInventory = Main.playerInventory;

            if (!favoritePanelUpdateNeeded) return;
            favoritePanelUpdateNeeded = false;

            using (RBProfiler.Step("RecipeBrowserUI.UpdateFavoritedPanel"))
            {
                if (RecipeCatalogueUI.instance?.recipeSlots != null)
                {
                    foreach (var slot in RecipeCatalogueUI.instance.recipeSlots) slot.favorited = false;
                    foreach (var fIndex in localPlayerFavoritedRecipes)
                    {
                        if (fIndex >= 0 && fIndex < RecipeCatalogueUI.instance.recipeSlots.Count)
                        {
                            RecipeCatalogueUI.instance.recipeSlots[fIndex].favorited = true;
                        }
                    }
                }

            if (localPlayerFavoritedRecipes.Count == 0 && !ForceShowFavoritePanel)
            {
                ShowFavoritePanel = false;
                ForceHideFavoritePanel = true;
            }

            favoritePanel.RemoveAllChildren();
            favoritePanel.Append(HideUnlessInventoryToggle);
            favoritePanel.Append(closeFavoritePanelButton);

            UIGrid favGrid = new UIGrid();
            favGrid.Width.Set(-18f, 1f);
            favGrid.Height.Set(0f, 1f);
            favGrid.ListPadding = 5f;
            favGrid.drawArrows = true;
            favoritePanel.Append(favGrid);
            favoritePanel.AddDragTarget(favGrid);
            favoritePanel.AddDragTarget(favGrid._innerList);

            int maxWidth = 1;
            int totalHeight = 0;
            int order = 1;

            foreach (var fIndex in localPlayerFavoritedRecipes)
            {
                if (fIndex >= 0 && fIndex < Recipe.numRecipes)
                {
                    Recipe r = Main.recipe[fIndex];
                    UIRecipeProgress prog = new UIRecipeProgress(fIndex, r, order++, Main.myPlayer);
                    prog.Recalculate();
                    var dims = prog.GetInnerDimensions();
                    prog.Width.Precent = 1f;
                    favGrid.Add(prog);
                    totalHeight += (int)(dims.Height + favGrid.ListPadding);
                    maxWidth = Math.Max(maxWidth, (int)dims.Width);
                    favoritePanel.AddDragTarget(prog);
                }
            }

            if (totalHeight == 0)
            {
                UIText noFav = new UIText(RBText("NoFavoritedRecipes"), 1f, false);
                favGrid.Add(noFav);
                noFav.Recalculate();
                var dims = noFav.GetInnerDimensions();
                totalHeight += (int)(dims.Height + favGrid.ListPadding);
                maxWidth = Math.Max(maxWidth, (int)dims.Width + 20);
                favoritePanel.AddDragTarget(noFav);
            }

            favoritePanel.Height.Pixels = totalHeight + favoritePanel.PaddingBottom + favoritePanel.PaddingTop - favGrid.ListPadding;
            favoritePanel.Width.Pixels = maxWidth + 18;
            favoritePanel.Recalculate();

            InvisibleFixedUIScrollbar favScroll = new InvisibleFixedUIScrollbar(userInterface);
            favScroll.SetView(100f, 1000f);
            favScroll.Height.Set(0f, 1f);
            favScroll.Left.Set(-20f, 1f);
            favGrid.SetScrollbar(favScroll);
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            SharedUI.instance?.Update();
            recipeCatalogueUI?.Update();
            craftUI?.Update();
            itemCatalogueUI?.Update();
            bestiaryUI?.Update();
            UpdateFavoritedPanel();
        }

        internal void ItemReceived(Item item)
        {
            if (item == null || item.IsAir) return;
            foreach (int fIndex in localPlayerFavoritedRecipes.Where(x => x < Recipe.numRecipes && Main.recipe[x]?.createItem?.type == item.type && Main.recipe[x]?.createItem?.maxStack == 1).ToList())
            {
                FavoriteChange(fIndex, false);
            }
            if (item.createTile > -1)
            {
                foreach (int adjT in Utilities.PopulateAdjTilesForTile(item.createTile))
                {
                    if (RecipeBrowserPlayer.seenTiles != null && adjT < RecipeBrowserPlayer.seenTiles.Length)
                    {
                        RecipeBrowserPlayer.seenTiles[adjT] = true;
                    }
                }
            }
        }
    }
}
