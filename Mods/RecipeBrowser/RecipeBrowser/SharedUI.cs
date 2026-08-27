using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using RecipeBrowser.UIElements;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using Terraria.WorldBuilding;

namespace RecipeBrowser
{
    public class SharedUI
    {
        internal static SharedUI instance;
        internal bool updateNeeded;
        internal UIPanel sortsAndFiltersPanel;
        internal UIHorizontalGrid categoriesGrid;
        internal InvisibleFixedUIHorizontalScrollbar categoriesGridScrollbar;
        internal UIHorizontalGrid subCategorySortsFiltersGrid;
        internal InvisibleFixedUIHorizontalScrollbar lootGridScrollbar2;

        private Sort selectedSort;
        private Category selectedCategory;

        internal List<Filter> availableFilters;
        internal List<Category> categories;
        internal List<Filter> filters;
        internal Filter CraftableFilter;
        internal Filter ObtainableFilter;
        internal Filter DisabledFilter;
        internal Filter UnresearchedFilter;
        internal CycleFilter ModFilterByFilter;
        internal List<Sort> sorts;

        private Dictionary<int, float> vanillaGrappleRanges;

        internal Sort SelectedSort
        {
            get => selectedSort;
            set
            {
                if (selectedSort != value)
                {
                    updateNeeded = true;
                    if (RecipeCatalogueUI.instance != null) RecipeCatalogueUI.instance.updateNeeded = true;
                    if (ItemCatalogueUI.instance != null) ItemCatalogueUI.instance.updateNeeded = true;
                }
                selectedSort = value;
            }
        }

        internal Category SelectedCategory
        {
            get => selectedCategory;
            set
            {
                if (selectedCategory != value)
                {
                    updateNeeded = true;
                    if (RecipeCatalogueUI.instance != null) RecipeCatalogueUI.instance.updateNeeded = true;
                    if (ItemCatalogueUI.instance != null) ItemCatalogueUI.instance.updateNeeded = true;
                }
                selectedCategory = value;
                if (selectedCategory != null && selectedCategory.sorts.Count > 0)
                {
                    SelectedSort = selectedCategory.sorts[0];
                }
                else if (selectedCategory != null && selectedCategory.parent != null && selectedCategory.parent.sorts.Count > 0)
                {
                    SelectedSort = selectedCategory.parent.sorts[0];
                }
            }
        }

        internal static string RBText(string key, string category = "RecipeCatalogueFilters")
        {
            return RBLanguage.GetText(category, key);
        }

        public SharedUI()
        {
            instance = this;
            vanillaGrappleRanges = new Dictionary<int, float>
            {
                [13] = 300f, [32] = 400f, [73] = 440f, [74] = 440f, [165] = 250f, [256] = 350f,
                [315] = 500f, [322] = 550f, [331] = 400f, [332] = 550f, [372] = 400f, [396] = 300f,
                [446] = 500f, [652] = 600f, [646] = 550f, [647] = 550f, [648] = 550f, [649] = 550f,
                [486] = 480f, [487] = 480f, [488] = 480f, [489] = 480f, [230] = 300f, [231] = 330f,
                [232] = 360f, [233] = 390f, [234] = 420f, [235] = 450f
            };
        }

        internal void Initialize()
        {
            sortsAndFiltersPanel = new UIPanel();
            sortsAndFiltersPanel.SetPadding(6f);
            sortsAndFiltersPanel.Top.Set(0f, 0f);
            sortsAndFiltersPanel.Width.Set(-275f, 1f);
            sortsAndFiltersPanel.Height.Set(60f, 0f);
            sortsAndFiltersPanel.BackgroundColor = Color.CornflowerBlue * 0.3f;
            updateNeeded = true;
        }

        internal void Update()
        {
            if (updateNeeded)
            {
                updateNeeded = false;
                if (sorts == null)
                {
                    SetupSortsAndCategories();
                }
                PopulateSortsAndFiltersPanel();
            }
        }

        private void PopulateSortsAndFiltersPanel()
        {
            List<Sort> sortList = new List<Sort>(sorts);
            sortList.RemoveAll(x => x.sortAvailable != null && !x.sortAvailable());

            availableFilters = new List<Filter>(filters);
            if (Main.GameMode != 3)
            {
                availableFilters.Remove(UnresearchedFilter);
            }
            if (RecipeBrowserUI.instance.CurrentPanel != 0 || RecipeBrowserUI.ModIndex == 0)
            {
                availableFilters.Remove(ModFilterByFilter);
            }

            if (subCategorySortsFiltersGrid != null)
            {
                sortsAndFiltersPanel.RemoveChild(subCategorySortsFiltersGrid);
                sortsAndFiltersPanel.RemoveChild(lootGridScrollbar2);
            }

            if (categoriesGrid == null)
            {
                categoriesGrid = new UIHorizontalGrid();
                categoriesGrid.Width.Set(0f, 1f);
                categoriesGrid.Height.Set(26f, 0f);
                categoriesGrid.ListPadding = 2f;
                categoriesGrid.drawArrows = true;

                categoriesGridScrollbar = new InvisibleFixedUIHorizontalScrollbar(RecipeBrowserUI.instance.userInterface);
                categoriesGridScrollbar.SetView(100f, 1000f);
                categoriesGridScrollbar.Width.Set(0f, 1f);
                categoriesGridScrollbar.Top.Set(0f, 0f);
                sortsAndFiltersPanel.Append(categoriesGridScrollbar);
                categoriesGrid.SetScrollbar(categoriesGridScrollbar);
                sortsAndFiltersPanel.Append(categoriesGrid);
            }

            subCategorySortsFiltersGrid = new UIHorizontalGrid();
            subCategorySortsFiltersGrid.Width.Set(0f, 1f);
            subCategorySortsFiltersGrid.Top.Set(26f, 0f);
            subCategorySortsFiltersGrid.Height.Set(26f, 0f);
            subCategorySortsFiltersGrid.ListPadding = 2f;
            subCategorySortsFiltersGrid.drawArrows = true;

            float viewPos = lootGridScrollbar2?.ViewPosition ?? 0f;
            lootGridScrollbar2 = new InvisibleFixedUIHorizontalScrollbar(RecipeBrowserUI.instance.userInterface);
            lootGridScrollbar2.SetView(100f, 1000f);
            lootGridScrollbar2.Width.Set(0f, 1f);
            lootGridScrollbar2.Top.Set(28f, 0f);
            sortsAndFiltersPanel.Append(lootGridScrollbar2);
            subCategorySortsFiltersGrid.SetScrollbar(lootGridScrollbar2);
            sortsAndFiltersPanel.Append(subCategorySortsFiltersGrid);

            int order = 0;
            List<Category> catList = new List<Category>();
            List<Category> subCatList = new List<Category>();

            foreach (var cat in categories)
            {
                cat.button.selected = false;
                catList.Add(cat);
                bool selected = SelectedCategory == cat;
                foreach (var sub in cat.subCategories)
                {
                    sub.button.selected = false;
                    selected |= (sub == SelectedCategory);
                }
                if (selected)
                {
                    subCatList.AddRange(cat.subCategories);
                    cat.button.selected = true;
                }
                if (RecipeBrowserUI.instance.CurrentPanel == 0 && cat.name == ArmorSetFeatureHelper.ArmorSetsInternalName)
                {
                    catList.Remove(cat);
                }
            }

            categoriesGrid.Clear();
            foreach (var cat in catList)
            {
                UISortableElement el = new UISortableElement(++order);
                el.Width.Set(24f, 0f);
                el.Height.Set(24f, 0f);
                el.Append(cat.button);
                categoriesGrid.Add(el);
            }

            foreach (var sub in subCatList)
            {
                UISortableElement el = new UISortableElement(++order);
                el.Width.Set(24f, 0f);
                el.Height.Set(24f, 0f);
                sub.button.selected = (sub == SelectedCategory);
                el.Append(sub.button);
                subCategorySortsFiltersGrid.Add(el);
            }

            if (subCatList.Count > 0)
            {
                UISortableElement sep = new UISortableElement(++order);
                sep.Width.Set(6f, 0f);
                sep.Height.Set(24f, 0f);
                subCategorySortsFiltersGrid.Add(sep);
            }

            List<Sort> activeSorts = new List<Sort>();
            if (SelectedCategory != null) SelectedCategory.ParentAddToSorts(activeSorts);
            foreach (var sort in sortList.Concat(activeSorts))
            {
                UISortableElement el = new UISortableElement(++order);
                el.Width.Set(24f, 0f);
                el.Height.Set(24f, 0f);
                sort.button.selected = (SelectedSort == sort);
                el.Append(sort.button);
                subCategorySortsFiltersGrid.Add(el);
            }

            UISortableElement sep2 = new UISortableElement(++order);
            sep2.Width.Set(6f, 0f);
            sep2.Height.Set(24f, 0f);
            subCategorySortsFiltersGrid.Add(sep2);

            List<Filter> activeFilters = new List<Filter>();
            if (SelectedCategory != null) SelectedCategory.ParentAddToFilters(activeFilters);
            foreach (var f in activeFilters.Concat(availableFilters))
            {
                UISortableElement el = new UISortableElement(++order);
                el.Width.Set(24f, 0f);
                el.Height.Set(24f, 0f);
                el.Append(f.button);
                subCategorySortsFiltersGrid.Add(el);
            }

            subCategorySortsFiltersGrid.Recalculate();
            lootGridScrollbar2.ViewPosition = viewPos;
            categoriesGrid.Recalculate();
        }

        private void SetupSortsAndCategories()
        {
            try
            {
                Main.instance.LoadItem(531);
                Main.instance.LoadItem(35);
                Main.instance.LoadItem(525);
                Main.instance.LoadItem(888);
                Main.instance.LoadItem(3102);
            }
            catch { }

            Texture2D sortTex = RBTextures.GetTexture("Images/sortAZ") ?? TextureAssets.MagicPixel.Value;
            Texture2D item3102 = (3102 < TextureAssets.Item.Length ? TextureAssets.Item[3102]?.Value : null) ?? TextureAssets.MagicPixel.Value;
            Texture2D sortRecipeOrderTex = (TextureAssets.CraftToggle.Length > 2 ? TextureAssets.CraftToggle[2]?.Value : null) ?? RBTextures.GetTexture("Images/sortRecipeOrder") ?? sortTex;

            sorts = new List<Sort>
            {
                new Sort(RBText("RecipeOrder"), sortRecipeOrderTex, (x, y) => 0)
                {
                    sortAvailable = () => RecipeBrowserUI.instance.CurrentPanel == 0
                },
                new Sort(RBText("ItemID"), "Images/sortItemID", (x, y) => x.type.CompareTo(y.type)),
                new Sort(RBText("Value"), "Images/sortValue", (x, y) => x.value.CompareTo(y.value)),
                new Sort(RBText("Alphabetical"), "Images/sortAZ", (x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal)),
                new Sort(RBText("Rarity"), item3102, (x, y) => (x.rare != y.rare) ? Math.Abs(x.rare).CompareTo(Math.Abs(y.rare)) : x.value.CompareTo(y.value))
            };

            Texture2D matTex = (531 < TextureAssets.Item.Length ? TextureAssets.Item[531]?.Value : null) ?? RBTextures.GetTexture("Images/filterModColorable") ?? TextureAssets.MagicPixel.Value;
            Texture2D craftableTex = (35 < TextureAssets.Item.Length ? TextureAssets.Item[35]?.Value : null) ?? matTex;
            Texture2D extCraftableTex = (525 < TextureAssets.Item.Length ? TextureAssets.Item[525]?.Value : null) ?? craftableTex;
            Texture2D disabledTex = (888 < TextureAssets.Item.Length ? TextureAssets.Item[888]?.Value : null) ?? matTex;
            Texture2D modColorableTex = RBTextures.GetTexture("Images/filterModColorable") ?? matTex;

            Texture2D unresearchedTex = null;
            try
            {
                unresearchedTex = Main.Assets.Request<Texture2D>("Images/UI/WorldCreation/IconDifficultyCreative", ReLogic.Content.AssetRequestMode.ImmediateLoad)?.Value;
            }
            catch { }
            unresearchedTex = unresearchedTex ?? matTex;

            Filter modFilter1 = new Filter(RBText("ModFilterByRecipeSourceTooltip"), x => true, modColorableTex)
            {
                recipeBelongs = (recipe) =>
                {
                    if (RecipeBrowserUI.ModIndex == 0 || RecipeBrowserUI.instance?.mods == null || RecipeBrowserUI.ModIndex >= RecipeBrowserUI.instance.mods.Length) return true;
                    string targetMod = RecipeBrowserUI.instance.mods[RecipeBrowserUI.ModIndex];
                    if (targetMod == "Terraria")
                    {
                        return recipe?.createItem != null && recipe.createItem.type < ItemID.Count;
                    }
                    var modItem = TPML.Content.ItemLoader.GetModItem(recipe?.createItem?.type ?? 0);
                    return modItem?.Mod?.Name == targetMod;
                }
            };
            Filter modFilter2 = new Filter(RBText("ModFilterByIngredientTooltip"), x => true, modColorableTex)
            {
                recipeBelongs = (recipe) =>
                {
                    if (RecipeBrowserUI.ModIndex == 0 || RecipeBrowserUI.instance?.mods == null || RecipeBrowserUI.ModIndex >= RecipeBrowserUI.instance.mods.Length) return true;
                    string targetMod = RecipeBrowserUI.instance.mods[RecipeBrowserUI.ModIndex];
                    if (targetMod == "Terraria")
                    {
                        return recipe?.requiredItem != null && recipe.requiredItem.Any(x => x != null && !x.IsAir && x.type < ItemID.Count);
                    }
                    return recipe?.requiredItem != null && recipe.requiredItem.Any(x =>
                    {
                        if (x == null || x.IsAir || x.type < ItemID.Count) return false;
                        var modItem = TPML.Content.ItemLoader.GetModItem(x.type);
                        return modItem?.Mod?.Name == targetMod;
                    });
                }
            };
            modFilter1.button.Color = Color.LightSeaGreen;
            modFilter2.button.Color = Color.Salmon;

            ModFilterByFilter = new CycleFilter(RBText("ModFilterByResultItemTooltip"), modColorableTex, new List<Filter> { modFilter1, modFilter2 });
            ModFilterByFilter.button.Color = Color.White;

            filters = new List<Filter>
            {
                new Filter(RBText("Materials"), x => x.material, matTex),
                (CraftableFilter = new Filter(RBText("Craftable"), x => true, craftableTex)),
                (ObtainableFilter = new Filter(RBText("ExtendedCraftable"), x => true, extCraftableTex)),
                (DisabledFilter = new Filter(RBText("DisabledRecipes"), x => true, disabledTex)),
                (UnresearchedFilter = new Filter(RBText("Unresearched"), x => CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId.ContainsKey(x.type) && !RecipePath.ItemFullyResearched(x.type), unresearchedTex)),
                ModFilterByFilter
            };

            // 根分类体系构建
            categories = new List<Category>
            {
                new Category("All", RBText("All"), x => true, "Images/sortAZ"),
                new Category("Weapons", RBText("Weapons.Name"), x => x.damage > 0 && x.ammo == 0 && !x.accessory, "Images/sortDamage"),
                new Category("Tools", RBText("Tools.Name"), x => x.pick > 0 || x.axe > 0 || x.hammer > 0 || x.fishingPole > 0, "Images/sortPick"),
                new Category("Armor", RBText("Armor.Name"), x => (x.headSlot >= 0 || x.bodySlot >= 0 || x.legSlot >= 0) && !x.vanity, "Images/sortDefense"),
                new Category("Accessories", RBText("Accessories.Name"), x => x.accessory, "Images/sortAccessory"),
                new Category("Ammo", RBText("Ammo.Name"), x => x.ammo > 0, "Images/sortAmmo"),
                new Category("Potions", RBText("Potions.Name"), x => (x.healLife > 0 || x.healMana > 0 || x.buffType > 0) && x.consumable, "Images/sortPotion"),
                new Category("Tiles", RBText("Tiles.Name"), x => x.createTile >= 0 || x.createWall >= 0, "Images/sortTile"),
                new Category("Misc", RBText("Other"), x => true, "Images/sortMisc"),
                new Category(ArmorSetFeatureHelper.ArmorSetsInternalName, ArmorSetFeatureHelper.ArmorSetsHoverTest, x => false, "Images/sortAZ")
            };

            // 子分类与过滤器分配
            var weapons = categories[1];
            weapons.subCategories.Add(new Category("Melee", RBText("Weapons.Melee"), x => x.melee, "Images/sortDamage"));
            weapons.subCategories.Add(new Category("Ranged", RBText("Weapons.Ranged"), x => x.ranged, "Images/sortAmmo"));
            weapons.subCategories.Add(new Category("Magic", RBText("Weapons.Magic"), x => x.magic, "Images/sortPotion"));
            weapons.subCategories.Add(new Category("Summon", RBText("Weapons.Summon"), x => x.summon, "Images/sortMisc"));

            foreach (var sub in weapons.subCategories) sub.parent = weapons;

            SelectedCategory = categories[0];
            SelectedSort = sorts[0];
        }
    }
}
