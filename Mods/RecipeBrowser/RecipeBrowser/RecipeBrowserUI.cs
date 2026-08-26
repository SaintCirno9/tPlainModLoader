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

        private BlockInputElement blockInput;
        private UIElement activeDialog;

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
                    Recipe.UpdateRecipeList();
                    Append(mainPanel);
                }
                else
                {
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

        public override void OnInitialize()
        {
            mainPanel = new UIDragableElement(dragable: true, resizeableX: true, resizeableY: true);
            mainPanel.Left.Set(400f, 0f);
            mainPanel.Top.Set(400f, 0f);
            mainPanel.Width.Set(475f, 0f);
            mainPanel.MinWidth.Set(415f, 0f);
            mainPanel.MaxWidth.Set(884f, 0f);
            mainPanel.Height.Set(350f, 0f);
            mainPanel.MinHeight.Set(263f, 0f);
            mainPanel.MaxHeight.Set(1000f, 0f);

            var config = RecipeBrowserClientConfig.Instance;
            if (config != null)
            {
                mainPanel.Left.Set(config.RecipeBrowserPosition.X, 0f);
                mainPanel.Top.Set(config.RecipeBrowserPosition.Y, 0f);
                mainPanel.Width.Set(config.RecipeBrowserSize.X, 0f);
                mainPanel.Height.Set(config.RecipeBrowserSize.Y, 0f);
            }

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

            Texture2D closeTex = RBTextures.CloseButton;
            closeButton = new UIHoverImageButton(closeTex, RBText("Close"));
            closeButton.OnLeftClick += (evt, el) => CloseButtonClicked(evt, el);
            closeButton.Left.Set(-26f, 1f);
            closeButton.VAlign = 0.5f;
            rightTab.Append(closeButton);
            mainPanel.Append(rightTab);

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
            instance.ShowRecipeBrowser = !instance.ShowRecipeBrowser;
            recipeCatalogueUI.CloseButtonClicked();
            bestiaryUI.CloseButtonClicked();
        }

        internal void CloseFavoritePanelButtonClicked(UIMouseEvent evt, UIElement listeningElement)
        {
            instance.ForceHideFavoritePanel = true;
            instance.ShowFavoritePanel = false;
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
