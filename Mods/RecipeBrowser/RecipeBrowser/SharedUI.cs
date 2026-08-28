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
using Terraria.GameContent.ItemDropRules;
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

        /// <summary>
        /// 外部（Call API / 模组注入）注册新分类/过滤器后强制重建分类体系
        /// </summary>
        public void SetupAgain()
        {
            sorts = null;
            categories = null;
            filters = null;
            updateNeeded = true;
        }

        /// <summary>
        /// 掉落规则 UI 可见性判定（对齐原版）：任一条件不允许在 UI 展示则隐藏
        /// </summary>
        internal static bool ShouldShowItemDrop(DropRateInfo dropRateInfo)
        {
            bool result = true;
            if (dropRateInfo.conditions != null && dropRateInfo.conditions.Count > 0)
            {
                for (int i = 0; i < dropRateInfo.conditions.Count; i++)
                {
                    if (!dropRateInfo.conditions[i].CanShowItemDropInUI())
                    {
                        result = false;
                        break;
                    }
                }
            }
            return result;
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

            // SelectedSort 失效时回退到首个可用排序（对齐原版）
            if (SelectedSort == null || !sortList.Concat(activeSorts).Contains(SelectedSort))
            {
                if (sortList.Count > 0)
                {
                    sortList[0].button.selected = true;
                    SelectedSort = sortList[0];
                    updateNeeded = false;
                }
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
                // 预加载分类图标所需物品贴图
                int[] preload = { 3102, 531, 35, 525, 888, 3520, 1336, 95, 42, 1309, 4672, 3829, 91, 82, 78, 243, 92, 171, 417, 3097, 4929, 2420, 425, 557, 2430, 1236, 1008, 1983, 2458, 5139, 54, 1162, 2343, 188, 189, 2347, 306, 473, 530, 66, 3318, 997, 856, 511, 512, 349, 359, 25, 34, 2532, 344, 3045, 2334, 3979, 3319, 3328, 3093, 2890 };
                foreach (int id in preload)
                {
                    try { Main.instance.LoadItem(id); } catch { }
                }
            }
            catch { }

            // ---------- 局部图标辅助（CPU 缩放，对齐原版 Utilities.ResizeImage/StackResizeImage 语义） ----------
            Texture2D ItemTex(int id) => (id < TextureAssets.Item.Length && TextureAssets.Item[id]?.Value != null)
                ? (Utilities.ResizeImage(TextureAssets.Item[id].Value, 24, 24) ?? TextureAssets.MagicPixel.Value)
                : TextureAssets.MagicPixel.Value;
            Texture2D SortTex(string name) => RBTextures.GetTexture(name) ?? TextureAssets.MagicPixel.Value;
            Texture2D StackTex(params int[] ids) => Utilities.StackResizeImage(ids.Select(ItemTex).ToArray(), 24, 24) ?? TextureAssets.MagicPixel.Value;

            Texture2D tMelee = ItemTex(3520);
            Texture2D tMagic = ItemTex(1336);
            Texture2D tRanged = ItemTex(95);
            Texture2D tThrowing = ItemTex(42);
            Texture2D tSummon = ItemTex(1309);
            Texture2D tWhip = ItemTex(4672);
            Texture2D tSentry = ItemTex(3829);
            Texture2D tHead = ItemTex(91);
            Texture2D tBody = ItemTex(82);
            Texture2D tLegs = ItemTex(78);
            Texture2D tVanity = ItemTex(243);
            Texture2D tArmorOnly = ItemTex(92);
            Texture2D tTile = ItemTex(171);
            Texture2D tCraftStation = ItemTex(35);
            Texture2D tWall = ItemTex(417);
            Texture2D tExpert = ItemTex(3097);
            Texture2D tMaster = ItemTex(4929);
            Texture2D tPet = ItemTex(2420);
            Texture2D tLightPet = ItemTex(425);
            Texture2D tBossSummon = ItemTex(557);
            Texture2D tMount = ItemTex(2430);
            Texture2D tHook = ItemTex(1236);
            Texture2D tDye = ItemTex(1008);
            Texture2D tHairDye = ItemTex(1983);
            Texture2D tQuestFish = ItemTex(2458);
            Texture2D tBobber = ItemTex(5139);
            Texture2D tAccessory = ItemTex(54);
            Texture2D tWing = ItemTex(1162);
            Texture2D tCart = ItemTex(2343);
            Texture2D tHealPotion = ItemTex(188);
            Texture2D tManaPotion = ItemTex(189);
            Texture2D tBuffPotion = ItemTex(2347);
            Texture2D tContainer = ItemTex(306);
            Texture2D tStatue = ItemTex(473);
            Texture2D tWiring = ItemTex(530);
            Texture2D tConsumable = ItemTex(66);
            Texture2D tGrabBag = ItemTex(3318);
            Texture2D tExtractinator = ItemTex(997);
            Texture2D tOther = ItemTex(856);
            Texture2D tArmor = StackTex(91, 82, 78);
            Texture2D tPets = StackTex(2420, 425);
            Texture2D tWeapons = StackTex(3520, 1336, 95);
            Texture2D tTools = Utilities.StackResizeImage(new[] { SortTex("Images/sortPick"), SortTex("Images/sortAxe"), SortTex("Images/sortHammer") }, 24, 24) ?? TextureAssets.MagicPixel.Value;
            Texture2D tFishing = Utilities.StackResizeImage(new[] { SortTex("Images/sortFish"), SortTex("Images/sortBait"), ItemTex(2458) }, 24, 24) ?? TextureAssets.MagicPixel.Value;
            Texture2D tPotions = StackTex(188, 189, 2347);
            Texture2D tDyes = StackTex(1008, 1983);
            Texture2D tPlaceTile = StackTex(349, 359);

            // ---------- 全局排序（对齐原版 6 个；CreativeSort 用原版 ContentSamples.ItemCreativeSortingId） ----------
            Texture2D sortTex = SortTex("Images/sortAZ");
            Texture2D item3102 = ItemTex(3102);
            Texture2D sortRecipeOrderTex = (TextureAssets.CraftToggle.Length > 2 ? TextureAssets.CraftToggle[2]?.Value : null) ?? sortTex;
            Texture2D sortCreativeTex = (TextureAssets.InventorySort.Length > 0 ? TextureAssets.InventorySort[0]?.Value : null) ?? sortTex;

            sorts = new List<Sort>
            {
                new Sort(RBText("RecipeOrder"), sortRecipeOrderTex, (x, y) => 0)
                {
                    // 注：原版 tML 用 Recipe.RecipeIndex；TPML 原版 Recipe 无此属性，
                    // 排序由 ItemGridSort 的槽位索引比较（r1.index）兜底，语义等价
                    sortAvailable = () => RecipeBrowserUI.instance.CurrentPanel == 0
                },
                new Sort(RBText("CreativeSort"), sortCreativeTex, ByCreativeSortingId)
                {
                    sortAvailable = () => RecipeBrowserUI.instance.CurrentPanel == 2
                },
                new Sort(RBText("ItemID"), "Images/sortItemID", (x, y) => x.type.CompareTo(y.type)),
                new Sort(RBText("Value"), "Images/sortValue", (x, y) => x.value.CompareTo(y.value)),
                new Sort(RBText("Alphabetical"), "Images/sortAZ", (x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal)),
                new Sort(RBText("Rarity"), item3102, (x, y) => (x.rare != y.rare) ? Math.Abs(x.rare).CompareTo(Math.Abs(y.rare)) : x.value.CompareTo(y.value))
            };

            // ---------- 过滤（保留移植版 TPML 适配逻辑） ----------
            Texture2D matTex = ItemTex(531);
            Texture2D craftableTex = ItemTex(35);
            Texture2D extCraftableTex = ItemTex(525);
            Texture2D disabledTex = ItemTex(888);
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

            // ---------- 弹药类型循环过滤（按使用频率统计，对齐原版） ----------
            int itemUpper = Math.Max((int)ItemID.Count, TPML.Content.ItemLoader.NextItemID);
            Dictionary<int, int> useAmmoCount = new Dictionary<int, int>();
            Dictionary<int, int> ammoCount = new Dictionary<int, int>();
            Item probe = new Item();
            for (int i = 0; i < itemUpper; i++)
            {
                try
                {
                    probe.SetDefaults(i);
                    if (probe.useAmmo > 0 && probe.ammo > 0 && probe.useAmmo < itemUpper && probe.ammo < itemUpper)
                    {
                        useAmmoCount.TryGetValue(probe.useAmmo, out var ua);
                        useAmmoCount[probe.useAmmo] = ua + 1;
                        ammoCount.TryGetValue(probe.ammo, out var am);
                        ammoCount[probe.ammo] = am + 1;
                    }
                }
                catch { }
            }
            List<int> ammoTypesByUse = useAmmoCount.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToList();
            List<int> ammoTypesByType = ammoCount.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToList();
            foreach (int a in ammoTypesByUse) { try { Main.instance.LoadItem(a); } catch { } }
            foreach (int a in ammoTypesByType) { try { Main.instance.LoadItem(a); } catch { } }

            List<Filter> ammoFilters = ammoTypesByType.Select(a => new Filter(Lang.GetItemNameValue(a), x => x.ammo == a, ItemTex(a))).ToList();
            List<Filter> usedAmmoFilters = ammoTypesByUse.Select(a => new Filter(Lang.GetItemNameValue(a), x => x.useAmmo == a, ItemTex(a))).ToList();
            CycleFilter ammoCycle = new CycleFilter(RBText("Ammo.CycleAmmoTypes"), "Images/sortAmmo", ammoFilters);
            CycleFilter usedAmmoCycle = new CycleFilter(RBText("Weapons.CycleUsedAmmoTypes"), "Images/sortAmmo", usedAmmoFilters);

            // ---------- 互斥过滤（对齐原版 Armor Vanity/ArmorOnly、Tiles Solid/NonSolid） ----------
            MutuallyExclusiveFilter armorVanityFilter = new MutuallyExclusiveFilter(RBText("Armor.Vanity"), x => x.vanity, tVanity);
            MutuallyExclusiveFilter armorOnlyFilter = new MutuallyExclusiveFilter(RBText("Armor.ArmorOnly"), x => !x.vanity, tArmorOnly);
            armorVanityFilter.SetExclusions(new List<Filter> { armorVanityFilter, armorOnlyFilter });
            armorOnlyFilter.SetExclusions(new List<Filter> { armorVanityFilter, armorOnlyFilter });

            MutuallyExclusiveFilter tileSolidFilter = new MutuallyExclusiveFilter(RBText("Tiles.Solid"), x => x.createTile > 0 && Main.tileSolid[x.createTile], ItemTex(511));
            MutuallyExclusiveFilter tileNonSolidFilter = new MutuallyExclusiveFilter(RBText("Tiles.NonSolid"), x => x.createTile > 0 && !Main.tileSolid[x.createTile], ItemTex(512));
            tileSolidFilter.SetExclusions(new List<Filter> { tileSolidFilter, tileNonSolidFilter });
            tileNonSolidFilter.SetExclusions(new List<Filter> { tileSolidFilter, tileNonSolidFilter });

            // ---------- 完整分类树（对齐原版 SharedUI.SetupSortsAndCategories，TPML 适配：1.4.4 原版字段替代 DamageClass） ----------
            List<int> yoyoIds = new List<int>();
            for (int i = 0; i < ItemID.Sets.Yoyo.Length; i++)
            {
                if (ItemID.Sets.Yoyo[i]) { try { Main.instance.LoadItem(i); yoyoIds.Add(i); } catch { } }
            }
            Texture2D tYoyo = (yoyoIds.Count > 0) ? ItemTex(yoyoIds[0]) : tMelee;

            categories = new List<Category>
            {
                new Category("All", RBText("All"), x => true, "Images/sortAZ"),
                new Category("Weapons", RBText("Weapons.Name"), x => false, tWeapons)
                {
                    subCategories = new List<Category>
                    {
                        new Category("Melee", RBText("Weapons.Melee"), x => x.melee && x.pick <= 0 && x.axe <= 0 && x.hammer <= 0, tMelee),
                        new Category("Yoyo", RBText("Weapons.Yoyo"), x => x.type < ItemID.Sets.Yoyo.Length && ItemID.Sets.Yoyo[x.type], tYoyo),
                        new Category("Magic", RBText("Weapons.Magic"), x => x.magic, tMagic),
                        new Category("Ranged", RBText("Weapons.Ranged"), x => x.ranged && x.ammo == 0, tRanged)
                        {
                            sorts = new List<Sort> { new Sort(RBText("Weapons.UseAmmoType"), "Images/sortAmmo", (a, b) => a.useAmmo.CompareTo(b.useAmmo)) },
                            filters = new List<Filter> { usedAmmoCycle }
                        },
                        new Category("Throwing", RBText("Weapons.Throwing"), x => false, tThrowing),
                        new Category("Summon", RBText("Weapons.Summon"), x => x.summon && !x.sentry && !ProjectileID.Sets.IsAWhip[x.shoot], tSummon),
                        new Category("Whip", RBText("Weapons.Whip"), x => x.summon && !x.sentry && ProjectileID.Sets.IsAWhip[x.shoot], tWhip),
                        new Category("Sentry", RBText("Weapons.Sentry"), x => x.summon && x.sentry, tSentry)
                    },
                    sorts = new List<Sort> { new Sort(RBText("Damage"), "Images/sortDamage", (a, b) => a.damage.CompareTo(b.damage)) }
                },
                new Category("Tools", RBText("Tools.Name"), x => false, tTools)
                {
                    subCategories = new List<Category>
                    {
                        new Category("Pickaxes", RBText("Tools.Pickaxes"), x => x.pick > 0, "Images/sortPick")
                        {
                            sorts = new List<Sort> { new Sort(RBText("Tools.PickPower"), "Images/sortPick", (a, b) => a.pick.CompareTo(b.pick)) }
                        },
                        new Category("Axes", RBText("Tools.Axes"), x => x.axe > 0, "Images/sortAxe")
                        {
                            sorts = new List<Sort> { new Sort(RBText("Tools.AxePower"), "Images/sortAxe", (a, b) => a.axe.CompareTo(b.axe)) }
                        },
                        new Category("Hammers", RBText("Tools.Hammers"), x => x.hammer > 0, "Images/sortHammer")
                        {
                            sorts = new List<Sort> { new Sort(RBText("Tools.HammerPower"), "Images/sortHammer", (a, b) => a.hammer.CompareTo(b.hammer)) }
                        }
                    }
                },
                new Category(ArmorSetFeatureHelper.ArmorSetsInternalName, ArmorSetFeatureHelper.ArmorSetsHoverTest, x => true, "Images/categoryArmorSets")
                {
                    sorts = new List<Sort> { new Sort(RBText("Armor.TotalDefense"), "Images/categoryArmorSets", (a, b) => a.defense.CompareTo(b.defense)) }
                },
                new Category("Armor", RBText("Armor.Name"), x => false, tArmor)
                {
                    subCategories = new List<Category>
                    {
                        new Category("Head", RBText("Armor.Head"), x => x.headSlot != -1, tHead),
                        new Category("Body", RBText("Armor.Body"), x => x.bodySlot != -1, tBody),
                        new Category("Legs", RBText("Armor.Legs"), x => x.legSlot != -1, tLegs)
                    },
                    sorts = new List<Sort> { new Sort(RBText("Armor.Defense"), "Images/sortDefense", (a, b) => a.defense.CompareTo(b.defense)) },
                    filters = new List<Filter> { armorVanityFilter, armorOnlyFilter }
                },
                new Category("Tiles", RBText("Tiles.Name"), x => x.createTile != -1, tTile)
                {
                    subCategories = new List<Category>
                    {
                        new Category("Crafting Stations", RBText("Tiles.CraftingStations"), x => RecipeCatalogueUI.instance?.craftingTiles != null && RecipeCatalogueUI.instance.craftingTiles.Contains(x.createTile), tCraftStation),
                        new Category("Furniture", RBText("Tiles.Furniture"), x => x.createTile > 0 && Main.tileFrameImportant[x.createTile], ItemTex(354)),
                        new Category("Blocks", RBText("Tiles.Blocks"), x => x.createTile >= 0 && !Main.tileFrameImportant[x.createTile], ItemTex(2))
                        {
                            filters = new List<Filter> { tileSolidFilter, tileNonSolidFilter }
                        },
                        new Category("Containers", RBText("Tiles.Containers"), x => x.createTile != -1 && Main.tileContainer[x.createTile], tContainer),
                        new Category("Wiring", RBText("Tiles.Wiring"), x => x.type < ItemID.Sets.SortingPriorityWiring.Length && ItemID.Sets.SortingPriorityWiring[x.type] > -1, tWiring),
                        new Category("Statues", RBText("Tiles.Statues"), x => StatueBelongs(x), tStatue),
                        new Category("Doors", RBText("Tiles.Doors"), x => x.createTile > 0 && x.createTile < TileID.Sets.RoomNeeds.CountsAsDoor.Length && TileID.Sets.RoomNeeds.CountsAsDoor[x.createTile], ItemTex(25)),
                        new Category("Chairs", RBText("Tiles.Chairs"), x => x.createTile > 0 && x.createTile < TileID.Sets.RoomNeeds.CountsAsChair.Length && TileID.Sets.RoomNeeds.CountsAsChair[x.createTile], ItemTex(34)),
                        new Category("Tables", RBText("Tiles.Tables"), x => x.createTile > 0 && x.createTile < TileID.Sets.RoomNeeds.CountsAsTable.Length && TileID.Sets.RoomNeeds.CountsAsTable[x.createTile], ItemTex(2532)),
                        new Category("Light Sources", RBText("Tiles.LightSources"), x => x.createTile > 0 && x.createTile < TileID.Sets.RoomNeeds.CountsAsTorch.Length && TileID.Sets.RoomNeeds.CountsAsTorch[x.createTile], ItemTex(344)),
                        new Category("Torches", RBText("Tiles.Torches"), x => x.createTile > 0 && x.type < TileID.Sets.Torches.Length && TileID.Sets.Torches[x.createTile], ItemTex(3045))
                    },
                    sorts = new List<Sort> { new Sort(RBText("Tiles.PlaceTile"), tPlaceTile, (a, b) => (a.createTile != b.createTile) ? a.createTile.CompareTo(b.createTile) : a.placeStyle.CompareTo(b.placeStyle)) }
                },
                new Category("Walls", RBText("Walls"), x => x.createWall != -1, tWall),
                new Category("Accessories", RBText("Accessories.Name"), x => x.accessory, tAccessory)
                {
                    subCategories = new List<Category>
                    {
                        new Category("Wings", RBText("Accessories.Wings.Name"), x => x.wingSlot > 0, tWing)
                        {
                            sorts = new List<Sort>
                            {
                                new Sort(RBText("Accessories.Wings.FlightTime"), "Images/sortWingsFlightTime", (a, b) => ArmorIDs.Wing.Sets.Stats[a.wingSlot].FlyTime.CompareTo(ArmorIDs.Wing.Sets.Stats[b.wingSlot].FlyTime)),
                                new Sort(RBText("Accessories.Wings.HorizontalSpeed"), "Images/sortWingsHorizontalSpeed", (a, b) => ArmorIDs.Wing.Sets.Stats[a.wingSlot].AccRunSpeedOverride.CompareTo(ArmorIDs.Wing.Sets.Stats[b.wingSlot].AccRunSpeedOverride)),
                                new Sort(RBText("Accessories.Wings.AccelerationMultiplier"), "Images/sortWingsAccelerationMultiplier", (a, b) => ArmorIDs.Wing.Sets.Stats[a.wingSlot].AccRunAccelerationMult.CompareTo(ArmorIDs.Wing.Sets.Stats[b.wingSlot].AccRunAccelerationMult))
                            }
                        }
                    }
                },
                new Category("Ammo", RBText("Ammo.Name"), x => x.ammo != 0, "Images/sortAmmo")
                {
                    sorts = new List<Sort>
                    {
                        new Sort(RBText("Ammo.AmmoType"), "Images/sortAmmo", (a, b) => a.ammo.CompareTo(b.ammo)),
                        new Sort(RBText("Damage"), "Images/sortDamage", (a, b) => a.damage.CompareTo(b.damage))
                    },
                    filters = new List<Filter> { ammoCycle }
                },
                new Category("Potions", RBText("Potions.Name"), x => x.potion, tPotions)
                {
                    subCategories = new List<Category>
                    {
                        new Category("Health Potions", RBText("Potions.HealthPotions"), x => x.healLife > 0, tHealPotion)
                        {
                            sorts = new List<Sort> { new Sort(RBText("Potions.HealLife"), tHealPotion, (a, b) => a.healLife.CompareTo(b.healLife)) }
                        },
                        new Category("Mana Potions", RBText("Potions.ManaPotions"), x => x.healMana > 0, tManaPotion)
                        {
                            sorts = new List<Sort> { new Sort(RBText("Potions.HealMana"), tManaPotion, (a, b) => a.healMana.CompareTo(b.healMana)) }
                        },
                        new Category("Buff Potions", RBText("Potions.BuffPotions"), x => x.potion && x.buffType > 0 && x.buffType != 26 && x.buffType != 206 && x.buffType != 207, tBuffPotion),
                        new Category("Food", RBText("Potions.Food"), x => x.buffType == 26 || x.buffType == 206 || x.buffType == 207, "Images/sortFood")
                    }
                },
                new Category("Expert", RBText("Expert"), x => x.expert, tExpert),
                // 注：原版"Master"分类依赖 tML 的 Item.master 字段，TPML 原版 Item 无此字段，故省略（记录于 WALKTHROUGH）
                new Category("Pets", RBText("Pets.Name"), x => false, tPets)
                {
                    subCategories = new List<Category>
                    {
                        new Category("Pets", RBText("Pets.CommonPets"), x => Main.vanityPet[x.buffType], tPet),
                        new Category("Light Pets", RBText("Pets.LightPets"), x => Main.lightPet[x.buffType], tLightPet)
                    }
                },
                new Category("Mounts", RBText("Mounts"), x => x.mountType != -1, tMount)
                {
                    subCategories = new List<Category>
                    {
                        new Category("Carts", RBText("Carts"), x => x.mountType != -1 && MountID.Sets.Cart[x.mountType], tCart)
                    }
                },
                new Category("Hooks", RBText("Hooks"), x => Main.projHook[x.shoot], tHook)
                {
                    sorts = new List<Sort> { new Sort(RBText("GrappleRange"), tHook, (a, b) => GrappleRange(a.shoot).CompareTo(GrappleRange(b.shoot))) }
                },
                new Category("Dyes", RBText("Dyes.Name"), x => false, tDyes)
                {
                    subCategories = new List<Category>
                    {
                        new Category("Dyes", RBText("Dyes.CommonDyes"), x => x.dye != 0, tDye),
                        new Category("Hair Dyes", RBText("Dyes.HairDyes"), x => x.hairDye != -1, tHairDye)
                    }
                },
                // 注：原版"Boss Summons"分类依赖 tML 的 Sets.SortingPriorityBossSpawns，TPML 原版 API 无此数据源，故省略（记录于 WALKTHROUGH）。
                new Category("Consumables", RBText("Consumables.Name"), x => x.createWall <= 0 && x.createTile <= -1 && (x.ammo <= 0 || x.notAmmo) && x.consumable, tConsumable)
                {
                    subCategories = new List<Category>
                    {
                        new Category("Captured NPC", RBText("Consumables.CapturedNPC"), x => x.makeNPC != 0, ItemTex(2890))
                    }
                },
                // 注：原版 Grab Bags 谓词依赖 tML 的 ItemDropDatabase.GetRulesForItemID（"作为容器开出的规则"）；
                // TPML 无此 API，改用 BossBag/钓鱼箱 近似覆盖（记录于 WALKTHROUGH）
                new Category("Grab Bags", RBText("GrabBags.Name"), x => (x.type < ItemID.Sets.BossBag.Length && ItemID.Sets.BossBag[x.type]) || (x.type < ItemID.Sets.IsFishingCrate.Length && ItemID.Sets.IsFishingCrate[x.type]), tGrabBag)
                {
                    subCategories = new List<Category>
                    {
                        new Category("Fishing Crate (Pre-Hardmode)", RBText("GrabBags.FishingCrate"), x => x.type < ItemID.Sets.IsFishingCrate.Length && ItemID.Sets.IsFishingCrate[x.type] && !ItemID.Sets.IsFishingCrateHardmode[x.type], ItemTex(2334)),
                        new Category("Fishing Crate (Hardmode)", RBText("GrabBags.FishingCrateHardmode"), x => x.type < ItemID.Sets.IsFishingCrateHardmode.Length && ItemID.Sets.IsFishingCrateHardmode[x.type], ItemTex(3979)),
                        // 注：原版 Pre/Hardmode Boss Bag 区分依赖 tML 的 PreHardmodeLikeBossBag，此处用固定前期 ID 集合近似
                        new Category("Boss Bag (Pre-Hardmode)", RBText("GrabBags.BossBag"), x => IsPreHardmodeBossBag(x.type), ItemTex(3319)),
                        new Category("Boss Bag (Hardmode)", RBText("GrabBags.BossBagHardmode"), x => x.type < ItemID.Sets.BossBag.Length && ItemID.Sets.BossBag[x.type] && !IsPreHardmodeBossBag(x.type), ItemTex(3328)),
                        new Category("Other", RBText("Other"), x => (x.type < ItemID.Sets.BossBag.Length && ItemID.Sets.BossBag[x.type]) || (x.type < ItemID.Sets.IsFishingCrate.Length && ItemID.Sets.IsFishingCrate[x.type]), ItemTex(3093))
                    }
                    // 注：原版 Grab Bags 的 ExpectedValue 排序依赖 GetRulesForItemID，TPML 无此 API，故省略
                },
                new Category("Fishing", RBText("Fishing.Name"), x => false, tFishing)
                {
                    subCategories = new List<Category>
                    {
                        new Category("Poles", RBText("Fishing.Poles"), x => x.fishingPole > 0, "Images/sortFish")
                        {
                            sorts = new List<Sort> { new Sort(RBText("Fishing.PolePower"), "Images/sortFish", (a, b) => a.fishingPole.CompareTo(b.fishingPole)) }
                        },
                        new Category("Bait", RBText("Fishing.Bait"), x => x.bait > 0, "Images/sortBait")
                        {
                            sorts = new List<Sort> { new Sort(RBText("Fishing.BaitPower"), "Images/sortBait", (a, b) => a.bait.CompareTo(b.bait)) }
                        },
                        new Category("Bobbers", RBText("Fishing.Bobbers"), x => x.type >= 5139 && x.type <= 5146, tBobber),
                        new Category("Quest Fish", RBText("Fishing.QuestFish"), x => x.questItem, tQuestFish)
                    }
                },
                new Category("Extractinator", RBText("Extractinator"), x => x.type < ItemID.Sets.ExtractinatorMode.Length && ItemID.Sets.ExtractinatorMode[x.type] > -1, tExtractinator),
                new Category("Other", RBText("Other"), BelongsInOther, tOther)
            };

            // ---------- modCategories / modFilters 消费（原版 Call API 的注入点） ----------
            if (RecipeBrowserMod.Instance != null)
            {
                foreach (var modCategory in RecipeBrowserMod.Instance.modCategories)
                {
                    if (string.IsNullOrEmpty(modCategory.parent))
                    {
                        categories.Insert(categories.Count - 2, new Category(modCategory.name, modCategory.name, modCategory.belongs, modCategory.icon));
                        continue;
                    }
                    bool found = false;
                    foreach (var cat in categories)
                    {
                        if (cat.name == modCategory.parent)
                        {
                            cat.subCategories.Add(new Category(modCategory.name, modCategory.name, modCategory.belongs, modCategory.icon));
                            found = true;
                        }
                    }
                    if (!found) Console.WriteLine($"[RecipeBrowser] Parent '{modCategory.parent}' for '{modCategory.name}' category not found.");
                }

                foreach (var modFilter in RecipeBrowserMod.Instance.modFilters)
                {
                    if (string.IsNullOrEmpty(modFilter.parent))
                    {
                        filters.Add(new Filter(modFilter.name, modFilter.belongs, modFilter.icon));
                        continue;
                    }
                    bool found = false;
                    foreach (var cat in categories)
                    {
                        if (cat.name == modFilter.parent)
                        {
                            cat.filters.Add(new Filter(modFilter.name, modFilter.belongs, modFilter.icon));
                            found = true;
                        }
                        foreach (var sub in cat.subCategories)
                        {
                            if (sub.name == modFilter.parent)
                            {
                                sub.filters.Add(new Filter(modFilter.name, modFilter.belongs, modFilter.icon));
                                found = true;
                            }
                        }
                    }
                    if (!found) Console.WriteLine($"[RecipeBrowser] Parent '{modFilter.parent}' for '{modFilter.name}' filter not found.");
                }
            }

            // 子分类 parent 归属
            foreach (var cat in categories)
            {
                foreach (var sub in cat.subCategories)
                {
                    sub.parent = cat;
                }
            }

            SelectedSort = sorts[0];
            SelectedCategory = categories[0];
        }

        /// <summary>
        /// 创意排序（对齐原版 ByCreativeSortingId）：按 1.4.4 原版 ContentSamples.ItemCreativeSortingId
        /// </summary>
        private int ByCreativeSortingId(Item x, Item y)
        {
            ContentSamples.CreativeHelper.ItemGroupAndOrderInGroup v1 = default;
            ContentSamples.CreativeHelper.ItemGroupAndOrderInGroup v2 = default;
            bool f1 = ContentSamples.ItemCreativeSortingId.TryGetValue(x.type, out v1);
            bool f2 = ContentSamples.ItemCreativeSortingId.TryGetValue(y.type, out v2);
            if (f1 && f2)
            {
                int g = v1.Group.CompareTo(v2.Group);
                if (g != 0) return g;
                int o = v1.OrderInGroup.CompareTo(v2.OrderInGroup);
                if (o != 0) return o;
            }
            else if (f1 != f2)
            {
                return f1 ? -1 : 1;
            }
            return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
        }

        /// <summary>
        /// 钩爪射程（原版 GrappleRange）：优先查内置表，未知类型返回 0
        /// </summary>
        private float GrappleRange(int type)
        {
            if (vanillaGrappleRanges != null && vanillaGrappleRanges.ContainsKey(type))
            {
                return vanillaGrappleRanges[type];
            }
            return 0f;
        }

        /// <summary>
        /// 雕像分类（对齐原版）：GenVars.statueList 中 (tile, placeStyle) 匹配
        /// </summary>
        private bool StatueBelongs(Item x)
        {
            if (GenVars.statueList == null) return false;
            return GenVars.statueList.Any(p => p.X == x.createTile && p.Y == x.placeStyle);
        }

        /// <summary>
        /// 前期 Boss 袋固定 ID 集合（史莱姆王/克眼/世界吞噬者/克脑/蜂王/骷髅王）——
        /// 替代 tML 的 ItemID.Sets.PreHardmodeLikeBossBag（TPML 无此字段）
        /// </summary>
        private static bool IsPreHardmodeBossBag(int type)
        {
            return type == 3318 || type == 3319 || type == 3320 || type == 3321 || type == 3322 || type == 3323;
        }

        /// <summary>
        /// "其他"兜底分类（对齐原版 BelongsInOther）
        /// </summary>
        private bool BelongsInOther(Item item)
        {
            if (categories == null) return true;
            foreach (var cat in categories.Skip(1).Take(Math.Max(0, categories.Count - 2)))
            {
                if (cat.name != ArmorSetFeatureHelper.ArmorSetsInternalName && cat.BelongsRecursive(item))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
